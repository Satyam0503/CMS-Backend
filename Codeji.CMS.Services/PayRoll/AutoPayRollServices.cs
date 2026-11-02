using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Services.Employees;
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Codeji.CMS.Services.PayRoll
{
    public class AutoPayrollServices : IAutoPayRollServices
    {
        private readonly IPayRollServices _payRollServices;
        private readonly IMongoDbRepository<EmpUser> _employeeRepository;
        private readonly IMongoDbRepository<EmpPayRoll> _empPayRollRepository;
        private readonly IMongoDbRepository<AttendanceModel> _attendanceRepository;
        private readonly IMongoDbRepository<AttendanceDaySegment> _attendanceSegmentRepository;
        private readonly IMongoDbRepository<SalaryModel> _salaryRepository;
        private readonly IMongoDbRepository<AttendanceStatusSetting> _attendanceStatusRepository;
        private readonly IMongoDbRepository<MonthlyAttendanceSummary> _attendanceSummaryRepository;
        private readonly IMongoDbRepository<AttendancePayrollException> _attendanceExceptionRepository;
        private readonly IPayrollDivisorPolicyService _divisorPolicies;
        private readonly ICompanyWorkingCalendarService _workingCalendar;
        private readonly IMongoDbRepository<EmployeePayrollAssignment> _assignments;
        private readonly IMongoDbRepository<PayrollPolicyProfile> _policyProfiles;
        private readonly IMongoDbRepository<PayrollPayGroup>? _payGroups;
        private readonly IMongoDbRepository<SalaryTemplate>? _salaryTemplates;
        private readonly IMongoDbRepository<EmployeeSalaryStructure>? _employeeSalaryStructures;
        private readonly IMongoDbRepository<AttendancePenaltyPolicy>? _penaltyPolicies;
        private readonly IMongoDbRepository<PayrollAdjustment>? _adjustments;
        private readonly IProfessionalTaxResolver _ptResolver;
        private readonly IMongoDbRepository<Company>? _companyRepository;
        private readonly IMongoDbRepository<EmployeeTaxDeclaration>? _taxDeclarations;

        public AutoPayrollServices(
            IPayRollServices payRollServices,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<EmpPayRoll> empPayRollRepository,
            IMongoDbRepository<AttendanceModel> attendanceRepository,
            IMongoDbRepository<AttendanceDaySegment> attendanceSegmentRepository,
            IMongoDbRepository<SalaryModel> salaryRepository,
            IMongoDbRepository<AttendanceStatusSetting> attendanceStatusRepository,
            IMongoDbRepository<MonthlyAttendanceSummary> attendanceSummaryRepository,
            IMongoDbRepository<AttendancePayrollException> attendanceExceptionRepository,
            IPayrollDivisorPolicyService divisorPolicies,
            ICompanyWorkingCalendarService workingCalendar,
            IMongoDbRepository<EmployeePayrollAssignment> assignments,
            IMongoDbRepository<PayrollPolicyProfile> policyProfiles,
            IMongoDbRepository<PayrollPayGroup>? payGroups = null,
            IMongoDbRepository<SalaryTemplate>? salaryTemplates = null,
            IMongoDbRepository<EmployeeSalaryStructure>? employeeSalaryStructures = null,
            IMongoDbRepository<AttendancePenaltyPolicy>? penaltyPolicies = null,
            IMongoDbRepository<PayrollAdjustment>? adjustments = null,
            IProfessionalTaxResolver? ptResolver = null,
            IMongoDbRepository<Company>? companyRepository = null,
            IMongoDbRepository<EmployeeTaxDeclaration>? taxDeclarations = null)
        {
            _payRollServices = payRollServices;
            _employeeRepository = employeeRepository;
            _empPayRollRepository = empPayRollRepository;
            _attendanceRepository = attendanceRepository;
            _attendanceSegmentRepository = attendanceSegmentRepository;
            _salaryRepository = salaryRepository;
            _attendanceStatusRepository = attendanceStatusRepository;
            _attendanceSummaryRepository = attendanceSummaryRepository;
            _attendanceExceptionRepository = attendanceExceptionRepository;
            _divisorPolicies = divisorPolicies;
            _workingCalendar = workingCalendar;
            _assignments = assignments;
            _policyProfiles = policyProfiles;
            _payGroups = payGroups;
            _salaryTemplates = salaryTemplates;
            _employeeSalaryStructures = employeeSalaryStructures;
            _penaltyPolicies = penaltyPolicies;
            _adjustments = adjustments;
            _ptResolver = ptResolver ?? new ProfessionalTaxResolver();
            _companyRepository = companyRepository;
            _taxDeclarations = taxDeclarations;
        }

        public async Task<Result> GeneratePayrollForMonthAsync(
            string companyId,
            DateTime payMonth,
            IReadOnlyCollection<string>? targetEmployeeIds = null,
            DateTime? plannedPaymentDate = null)
        {
            var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
            // Payroll keys are calendar months. Use the canonical first-of-month value for
            // every read and write so a time component or client timezone cannot create a
            // second key for the same displayed month.
            payMonth = monthStart;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var tdsPaymentDate = (plannedPaymentDate ?? DateTime.UtcNow).Date;
            var tdsPeriod = PayrollTaxPeriod.ResolveIndiaSalaryPayment(tdsPaymentDate);
            var employees = (await _employeeRepository.GetAll(e => e.CompanyId == companyId && !e.IsDeleted))
                .Where(e => EmploymentPeriod.IsPayrollIncludedForMonth(e, monthStart))
                .ToList();

            var targetIdsSet = targetEmployeeIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (targetIdsSet != null && targetIdsSet.Count > 0)
            {
                employees = employees.Where(e => targetIdsSet.Contains(e.EmployeeId)).ToList();
            }

            // Check for existing processed payroll rows in this company and month.
            // A finalized/processed employee row must NEVER be overwritten or mutated.
            // However, if some employees were previously skipped (e.g. salary was not decided/set),
            // or new eligible employees were added, payroll must still calculate for the remaining
            // unprocessed/draft employees without re-generating or touching the locked ones.
            var existingMonthPayrolls = _empPayRollRepository
                .Get(p => p.CompanyId == companyId && p.PayMonth == monthStart)
                .ToList();

            static bool IsProcessedRow(EmpPayRoll p) =>
                p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" ||
                p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED";

            var processedEmployeeIds = existingMonthPayrolls
                .Where(IsProcessedRow)
                .Select(p => p.EmployeeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var excelUploadedEmployeeIds = existingMonthPayrolls
                .Where(p => string.Equals(p.CalculationSource, "EXCEL_UPLOAD", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.EmployeeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var hasAnyProcessedPayroll = processedEmployeeIds.Count > 0 ||
                await _empPayRollRepository.Exist(p => p.CompanyId == companyId &&
                    p.PayMonth == monthStart && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED"));

            if (hasAnyProcessedPayroll && (employees.Count == 0 || employees.All(e => processedEmployeeIds.Contains(e.EmployeeId))))
                return new Result { Success = false, Message = $"Payroll for {monthStart:MMMM yyyy} is already processed and cannot be regenerated." };

            var divisorSetting = await _divisorPolicies.GetEffective(companyId, monthStart);
            var divisorPolicy = divisorSetting.DivisorPolicy;
            var requireAttendanceLock = divisorSetting.RequireAttendanceLock;
            var policyProfiles = await _policyProfiles.GetAll(x => x.CompanyId == companyId && x.IsActive);
            var payGroups = _payGroups is null ? Array.Empty<PayrollPayGroup>() : (await _payGroups.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToArray();
            var salaryTemplates = _salaryTemplates is null ? Array.Empty<SalaryTemplate>() : (await _salaryTemplates.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToArray();
            var canonicalSalaryStructures = _employeeSalaryStructures is null ? Array.Empty<EmployeeSalaryStructure>() : (await _employeeSalaryStructures.GetAll(x => x.CompanyId == companyId && x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE")).ToArray();
            var assignmentRows = (await _assignments.GetAll(x => x.CompanyId == companyId)).ToArray();
            var company = _companyRepository is null ? null : await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId);
            var companyDefaultState = company?.State;

            // Validate every eligible employee before the first payroll write. This prevents a
            // later employee validation failure from leaving an unnoticed partial company run.
            var validationErrors = new List<string>();
            foreach (var employee in employees)
            {
                if (processedEmployeeIds.Contains(employee.EmployeeId)) continue;
                if ((targetIdsSet == null || targetIdsSet.Count == 0) && excelUploadedEmployeeIds.Contains(employee.EmployeeId)) continue;
                // Payroll cannot be calculated without an effective joining date. Skip this
                // employee instead of blocking a completed historical month for everyone else.
                if (!EmploymentPeriod.TryGetJoiningDate(employee, out var joining)) continue;
                if (joining.Date > monthEnd) continue;
                var exitDate = EmploymentPeriod.GetLastWorkingDate(employee);
                if (exitDate.HasValue && exitDate.Value < monthStart) continue;
                if (exitDate.HasValue && exitDate.Value < joining.Date) { validationErrors.Add($"{employee.EmployeeId}: exit date cannot be before joining date."); continue; }
                var policyPeriodStart = joining.Date > monthStart ? joining.Date : monthStart;
                var policyPeriodEnd = exitDate.HasValue && exitDate.Value < monthEnd ? exitDate.Value : monthEnd;
                var validationPolicy = PayrollPolicyResolver.ResolveConsistentPeriod(
                    companyId, employee.EmployeeId, policyPeriodStart, policyPeriodEnd,
                    assignmentRows, canonicalSalaryStructures, policyProfiles, payGroups,
                    joiningDate: joining.Date);
                // Mid-month policy transitions adopt the active policy as of period end instead of blocking the whole company
                var validationProfile = validationPolicy.Profile ?? PayrollPolicyResolver.ResolvePolicy(companyId, policyPeriodEnd, PayrollPolicyResolver.ResolveAssignment(companyId, employee.EmployeeId, policyPeriodEnd, assignmentRows), policyProfiles, payGroups);
                var taxAssignment = PayrollPolicyResolver.ResolveAssignment(companyId, employee.EmployeeId, tdsPaymentDate, assignmentRows);
                var taxProfile = PayrollPolicyResolver.ResolvePolicy(companyId, tdsPaymentDate, taxAssignment, policyProfiles, payGroups, validationPolicy.PayGroupId);
                var validationTdsMode = taxProfile?.TdsMode ?? validationProfile?.TdsMode ?? divisorSetting.TdsMode;
                if (!PayrollCalculationRules.IsSupportedTdsMode(validationTdsMode))
                    validationErrors.Add($"{employee.EmployeeId}: configured TDS mode '{validationTdsMode}' is unsupported; choose AUTO_NEW_REGIME, AUTO_OLD_REGIME, or MANUAL_ONLY before processing payroll.");
                if (!string.Equals(validationTdsMode, "MANUAL_ONLY", StringComparison.OrdinalIgnoreCase))
                {
                    if (validationProfile is not null && taxProfile is null)
                        validationErrors.Add($"{employee.EmployeeId}: no approved TDS policy is effective on planned salary payment date {tdsPaymentDate:yyyy-MM-dd}. Configure the company or pay-group tax policy for financial year {tdsPeriod.FinancialYear}.");
                    if (taxProfile is not null && !string.Equals(taxProfile.FinancialYear?.Trim(), tdsPeriod.FinancialYear, StringComparison.Ordinal))
                        validationErrors.Add($"{employee.EmployeeId}: TDS policy '{taxProfile.Name}' is configured for tax year '{taxProfile.FinancialYear}', but planned salary payment date {tdsPaymentDate:yyyy-MM-dd} belongs to {tdsPeriod.FinancialYear}. Configure an approved policy effective for the payment date and tax year.");
                    if (taxProfile is not null && (!taxProfile.StatutoryJurisdiction.StartsWith("IN", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(taxProfile.StatutoryRuleVersion)))
                        validationErrors.Add($"{employee.EmployeeId}: the TDS policy effective on {tdsPaymentDate:yyyy-MM-dd} needs an Indian jurisdiction and statutory rule version.");
                }
                var invalidSalaryIdentity = canonicalSalaryStructures.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId &&
                    x.EffectiveFrom.Date <= monthEnd.Date && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= monthStart.Date) &&
                    !Guid.TryParse(x.Id, out _));
                if (invalidSalaryIdentity is not null)
                    validationErrors.Add($"{employee.EmployeeId}: approved salary structure '{invalidSalaryIdentity.Id}' has an invalid identifier; correct the salary record before processing payroll.");
                var validationRequiresAttendanceLock = validationProfile?.RequireAttendanceLock ?? requireAttendanceLock;
                var summary = await _attendanceSummaryRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == monthStart && x.IsApproved && x.IsLocked);
                if (summary == null && validationRequiresAttendanceLock) validationErrors.Add($"{employee.EmployeeId}: monthly attendance for {monthStart:MMMM yyyy} must be approved and locked. Please go to Attendance -> Month Summary -> Click 'Validate & Lock Month' (or disable 'Require Attendance Lock' in Company Payroll Settings if daily attendance tracking is not required).");
                // Missing salary structure is not a company-wide blocker: the generation loop below
                // already skips that employee individually (tracked via skippedNoSalaryCount) and
                // reports the count in the result message, so other employees can still be paid.
                if (await _attendanceExceptionRepository.Exist(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == monthStart && x.Status == "PENDING_REVIEW")) validationErrors.Add($"{employee.EmployeeId}: unresolved attendance exception requires review.");
            }
            if (validationErrors.Count > 0) return new Result
            {
                Success = false,
                StatusCode = 422,
                ErrorCode = "PAYROLL_VALIDATION_BLOCKED",
                Message = $"Payroll validation failed for {validationErrors.Count} issue(s). No payroll records were changed.",
                Errors = validationErrors.Select(message =>
                {
                    var separator = message.IndexOf(": ", StringComparison.Ordinal);
                    return new ValidationError
                    {
                        Field = separator >= 0 ? message[..separator] : "Payroll",
                        Message = separator >= 0 ? message[(separator + 2)..] : message
                    };
                }).ToList()
            };

            int processedCount = 0;
            int skippedNoSalaryCount = 0;
            var skippedNoSalaryEmployeeIds = new List<string>();
            int consideredCount = 0;
            var payrollMutations = new List<PayrollMutation>();

            try
            {
                var monthAdjustments = _adjustments != null
                    ? (await _adjustments.GetAll(x => x.CompanyId == companyId && x.Status == "APPROVED" && x.TargetPeriod == monthStart)).ToList()
                    : [];
                var adjustmentsByEmployee = monthAdjustments
                    .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                foreach (var emp in employees)
                {
                    if (processedEmployeeIds.Contains(emp.EmployeeId))
                        continue;
                    if ((targetIdsSet == null || targetIdsSet.Count == 0) && excelUploadedEmployeeIds.Contains(emp.EmployeeId))
                        continue;

                    if (!EmploymentPeriod.TryGetJoiningDate(emp, out DateTime joiningDate))
                        continue;

                    if (joiningDate.Date > monthEnd)
                        continue;

                    DateTime? exitDate = EmploymentPeriod.GetLastWorkingDate(emp);
                    if (exitDate.HasValue && exitDate.Value < monthStart)
                        continue;

                    var eligibleFrom = joiningDate.Date > monthStart ? joiningDate.Date : monthStart;
                    var eligibleTo = exitDate.HasValue && exitDate.Value < monthEnd ? exitDate.Value : monthEnd;

                    var assignment = PayrollPolicyResolver.ResolveAssignment(companyId, emp.EmployeeId, monthStart, assignmentRows);
                    var policyPeriodStart = joiningDate.Date > monthStart ? joiningDate.Date : monthStart;
                    var policyPeriodEnd = exitDate.HasValue && exitDate.Value < monthEnd ? exitDate.Value : monthEnd;
                    var resolvedPolicyPeriod = PayrollPolicyResolver.ResolveConsistentPeriod(
                        companyId, emp.EmployeeId, policyPeriodStart, policyPeriodEnd,
                        assignmentRows, canonicalSalaryStructures, policyProfiles, payGroups,
                        joiningDate: joiningDate.Date);
                    var resolvedProfile = resolvedPolicyPeriod.Profile ?? PayrollPolicyResolver.ResolvePolicy(companyId, policyPeriodEnd, assignment, policyProfiles, payGroups);
                    var taxAssignment = PayrollPolicyResolver.ResolveAssignment(companyId, emp.EmployeeId, tdsPaymentDate, assignmentRows);
                    var tdsPolicyProfile = PayrollPolicyResolver.ResolvePolicy(companyId, tdsPaymentDate, taxAssignment, policyProfiles, payGroups, resolvedPolicyPeriod.PayGroupId);
                    var salaryResolution = PayrollSalarySourceResolver.Resolve(
                        companyId, emp.EmployeeId, monthStart, monthEnd, canonicalSalaryStructures,
                        () => _salaryRepository.Get(s => s.CompanyId == companyId && s.EmployeeId == emp.EmployeeId && s.EffectiveFrom <= monthEnd && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= monthStart)).ToList(),
                        joiningDate.Date);
                    var overlappingSalaryRecords = salaryResolution.Records;
                    var canonicalRecords = salaryResolution.UsesCanonicalSource
                        ? canonicalSalaryStructures.Where(x => x.EmployeeId == emp.EmployeeId &&
                            ((x.EffectiveFrom.Date <= monthEnd.Date && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= monthStart.Date)) ||
                             (salaryResolution.Records.Count > 0 && Guid.TryParse(x.Id, out var parsedId) && parsedId == salaryResolution.Records[0].SalaryId))).ToList()
                        : [];

                    var activeSalaryStructure = canonicalRecords.FirstOrDefault();
                    var employeeTaxRegime = activeSalaryStructure?.TaxRegime;
                    var employeeEpfMode = activeSalaryStructure?.EpfModeOverride;
                    var employeeEsicMode = activeSalaryStructure?.EsicModeOverride;
                    var employeeAttendanceMode = activeSalaryStructure?.AttendanceTrackingMode;

                    var effectiveDivisorPolicy = resolvedProfile?.DivisorPolicy ?? divisorPolicy;
                    var effectiveRequireAttendanceLock = resolvedProfile?.RequireAttendanceLock ?? requireAttendanceLock;
                    var effectiveEpfMode = !string.IsNullOrWhiteSpace(employeeEpfMode) ? employeeEpfMode : (resolvedProfile?.EpfMode ?? divisorSetting.EpfMode);
                    var effectiveEpfWageCeiling = resolvedProfile?.EpfWageCeiling ?? divisorSetting.EpfWageCeiling;
                    var effectiveEpfEmployeeRate = resolvedProfile?.EpfEmployeeRatePercent ?? divisorSetting.EpfEmployeeRatePercent;
                    var effectiveEpfEmployerRate = resolvedProfile?.EpfEmployerRatePercent ?? divisorSetting.EpfEmployerRatePercent;
                    var effectiveEsicMode = !string.IsNullOrWhiteSpace(employeeEsicMode) ? employeeEsicMode : (resolvedProfile?.EsicMode ?? divisorSetting.EsicMode);
                    var effectiveEsicWageCeiling = resolvedProfile?.EsicWageCeiling ?? divisorSetting.EsicWageCeiling;
                    var effectiveEsicEmployeeRate = resolvedProfile?.EsicEmployeeRatePercent ?? divisorSetting.EsicEmployeeRatePercent;
                    var effectiveEsicEmployerRate = resolvedProfile?.EsicEmployerRatePercent ?? divisorSetting.EsicEmployerRatePercent;
                    var effectiveTdsMode = !string.IsNullOrWhiteSpace(employeeTaxRegime) ? employeeTaxRegime : (tdsPolicyProfile?.TdsMode ?? resolvedProfile?.TdsMode ?? divisorSetting.TdsMode);
                    var isAttendanceExempt = string.Equals(employeeAttendanceMode, "EXEMPT", StringComparison.OrdinalIgnoreCase);

                    var attendanceSummary = await _attendanceSummaryRepository.FirstOrDefault(x =>
                        x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId &&
                        x.PayrollMonth == monthStart && x.IsApproved && x.IsLocked);
                    if (attendanceSummary == null && effectiveRequireAttendanceLock && !isAttendanceExempt)
                    {
                        await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                        return new Result { Success = false, Message = $"Attendance must be validated and locked before payroll. Missing lock for employee {emp.EmployeeId}." };
                    }

                    var attendanceException = await _attendanceExceptionRepository.FirstOrDefault(x =>
                        x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId &&
                        x.PayrollMonth == monthStart && x.ExceptionType == "LHD_ED_LIMIT_EXCEEDED" && x.Status != "CANCELLED");
                    if (attendanceException?.Status == "PENDING_REVIEW")
                    {
                        await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                        return new Result { Success = false, Message = $"Payroll is blocked: the LHD/ED exception for employee {emp.EmployeeId} requires HR/Admin review." };
                    }

                    consideredCount++;

                    if (canonicalRecords.Count > 0)
                    {
                        var taxTreatmentIssue = PayrollCalculationRules.FindUnsupportedAutomaticTdsTreatment(
                            canonicalRecords.SelectMany(record => record.Components).Select((component, index) => new PayrollComponentSnapshot
                            {
                                Code = component.ComponentCode, Name = component.ComponentName, Kind = component.Kind,
                                CalculationType = component.CalculationType, BaseComponentCode = component.BaseComponentCode,
                                Value = component.Percentage ?? component.Value, IsTaxable = component.IsTaxable,
                                IsRecurring = component.IsRecurring, IsStatutory = component.IsStatutory,
                                IsEmployerContribution = component.IsEmployerContribution, SortOrder = index
                            }), effectiveTdsMode);
                        if (taxTreatmentIssue is not null)
                        {
                            await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                            return new Result { Success = false, Message = $"Payroll is blocked for employee {emp.EmployeeId}: {taxTreatmentIssue} No payroll records were changed." };
                        }
                    }

                    var latestSalaryRecord = overlappingSalaryRecords.FirstOrDefault();
                    var assignedTemplate = assignment?.SalaryTemplateId is null ? null : salaryTemplates.FirstOrDefault(x => x.Id == assignment.SalaryTemplateId && (x.PayGroupId is null || x.PayGroupId == assignment.PayGroupId) && x.EffectiveFrom.Date <= monthStart.Date && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= monthStart.Date));
                    if (latestSalaryRecord == null)
                    {
                        skippedNoSalaryCount++;
                        skippedNoSalaryEmployeeIds.Add(emp.EmployeeId);
                        continue;
                    }

                    int daysInMonth = DateTime.DaysInMonth(payMonth.Year, payMonth.Month);
                    int eligibleDays = (eligibleTo - eligibleFrom).Days + 1;

                    // BambooHR-style day-weighted split-month salary proration:
                    // If an employee receives a mid-month increment/revision, weight each day in [eligibleFrom, eligibleTo]
                    // by the SalaryModel active on that date.
                    decimal weightedBasicSum = 0m;
                    decimal weightedHraSum = 0m;
                    decimal weightedLtaSum = 0m;
                    decimal weightedOtherSum = 0m;
                    decimal weightedBonusSum = 0m;
                    decimal weightedHealthInsuranceSum = 0m;
                    var weightedSalaryDeductions = new Dictionary<string, (string Name, decimal Amount)>(StringComparer.OrdinalIgnoreCase);
                    var weightedSalaryEarnings = new Dictionary<string, (string Name, decimal Amount)>(StringComparer.OrdinalIgnoreCase);

                    for (var cursor = eligibleFrom.Date; cursor <= eligibleTo.Date; cursor = cursor.AddDays(1))
                    {                        var activeSalaryForDay = overlappingSalaryRecords
                            .FirstOrDefault(s => s.EffectiveFrom.Date <= cursor && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value.Date >= cursor));
                        if (activeSalaryForDay is null)
                        {
                            await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                            return new Result { Success = false, Message = $"Payroll is blocked: no effective salary record covers {cursor:yyyy-MM-dd} for employee {emp.EmployeeId}." };
                        }

                        weightedBasicSum += activeSalaryForDay.BasicPay;
                        weightedHraSum += activeSalaryForDay.Hra ?? 0m;
                        weightedLtaSum += activeSalaryForDay.Lta ?? 0m;
                        weightedOtherSum += activeSalaryForDay.OtherAllowances ?? 0m;
                        weightedBonusSum += activeSalaryForDay.Bonus ?? 0m;
                        weightedHealthInsuranceSum += activeSalaryForDay.HealthInsurance ?? 0m;
                        if (activeSalaryForDay.OtherEarnings.Count > 0)
                        {
                            foreach (var item in activeSalaryForDay.OtherEarnings)
                            {
                                var prior = weightedSalaryEarnings.GetValueOrDefault(item.Code);
                                weightedSalaryEarnings[item.Code] = (item.Name, prior.Amount + item.Amount);
                            }
                        }
                        else if ((activeSalaryForDay.OtherAllowances ?? 0m) > 0m)
                        {
                            var prior = weightedSalaryEarnings.GetValueOrDefault("OTHER_ALLOWANCE");
                            weightedSalaryEarnings["OTHER_ALLOWANCE"] = ("Other Allowance", prior.Amount + (activeSalaryForDay.OtherAllowances ?? 0m));
                        }
                        foreach (var item in activeSalaryForDay.OtherDeductions)
                        {
                            var prior = weightedSalaryDeductions.GetValueOrDefault(item.Code);
                            weightedSalaryDeductions[item.Code] = (item.Name, prior.Amount + item.Amount);
                        }
                    }

                    // An approved employee salary is the payable offer/revision. A
                    // template is only a fallback when no canonical salary structure exists.
                    var hasApprovedEmployeeStructure = canonicalRecords.Count > 0;
                    decimal originalBasic = PayrollSalaryValueResolver.Resolve(hasApprovedEmployeeStructure, assignedTemplate?.BasicPay, eligibleDays > 0 ? weightedBasicSum / eligibleDays : latestSalaryRecord?.BasicPay ?? 0m, latestSalaryRecord?.BasicPay ?? 0m);
                    decimal originalHRA = PayrollSalaryValueResolver.Resolve(hasApprovedEmployeeStructure, assignedTemplate?.Hra, eligibleDays > 0 ? weightedHraSum / eligibleDays : latestSalaryRecord?.Hra ?? 0m, latestSalaryRecord?.Hra ?? 0m);
                    decimal originalLTA = PayrollSalaryValueResolver.Resolve(hasApprovedEmployeeStructure, assignedTemplate?.Lta, eligibleDays > 0 ? weightedLtaSum / eligibleDays : latestSalaryRecord?.Lta ?? 0m, latestSalaryRecord?.Lta ?? 0m);
                    decimal originalOtherAllowance = PayrollSalaryValueResolver.Resolve(hasApprovedEmployeeStructure, assignedTemplate?.OtherAllowance, eligibleDays > 0 ? weightedOtherSum / eligibleDays : latestSalaryRecord?.OtherAllowances ?? 0m, latestSalaryRecord?.OtherAllowances ?? 0m);
                    decimal originalBonus = PayrollSalaryValueResolver.Resolve(hasApprovedEmployeeStructure, assignedTemplate?.Bonus, eligibleDays > 0 ? weightedBonusSum / eligibleDays : latestSalaryRecord?.Bonus ?? 0m, latestSalaryRecord?.Bonus ?? 0m);
                    decimal contractualFixedMonthlyGross = originalBasic + originalHRA + originalLTA + originalOtherAllowance + originalBonus;

                    var eligibleToExclusive = eligibleTo.Date.AddDays(1);
                    var attendanceRecords = await _attendanceRepository.GetAll(a =>
                        a.CompanyId == companyId && a.UserId == emp.UserId && a.EmployeeId == emp.EmployeeId &&
                        a.Date >= eligibleFrom &&
                        a.Date < eligibleToExclusive);
                    var attendanceSegments = await _attendanceSegmentRepository.GetAll(a =>
                        a.CompanyId == companyId && a.UserId == emp.UserId && a.EmployeeId == emp.EmployeeId &&
                        a.Date >= eligibleFrom && a.Date < eligibleToExclusive);

                    var statusRules = (await _attendanceStatusRepository.GetAll(s => s.CompanyId == companyId)).ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);
                    var workingDays = await _workingCalendar.CountWorkingDaysAsync(companyId, DateOnly.FromDateTime(monthStart), DateOnly.FromDateTime(monthEnd));
                    decimal resolvedDivisor = PayrollCalculationRules.ResolveDivisor(effectiveDivisorPolicy, daysInMonth, workingDays);
                    if (resolvedDivisor <= 0)
                    {
                        await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                        return new Result { Success = false, Message = $"Invalid payroll divisor for employee {emp.EmployeeId}. No payroll was changed." };
                    }

                    // Joining / exit / split-month earned components aligned with divisor policy
                    decimal basicAfterJoining;
                    decimal hra;
                    decimal lta;
                    decimal otherAllowance;
                    decimal bonus;

                    if (eligibleDays < daysInMonth && effectiveDivisorPolicy == "WORKING_DAYS")
                    {
                        var eligibleWorkingDaysInPeriod = await _workingCalendar.CountWorkingDaysAsync(
                            companyId, DateOnly.FromDateTime(eligibleFrom), DateOnly.FromDateTime(eligibleTo));
                        decimal prorationFactor = workingDays > 0 ? (decimal)eligibleWorkingDaysInPeriod / workingDays : (decimal)eligibleDays / daysInMonth;
                        basicAfterJoining = PayrollCalculationRules.RoundMoney(originalBasic * prorationFactor);
                        hra = PayrollCalculationRules.RoundMoney(originalHRA * prorationFactor);
                        lta = PayrollCalculationRules.RoundMoney(originalLTA * prorationFactor);
                        otherAllowance = PayrollCalculationRules.RoundMoney(originalOtherAllowance * prorationFactor);
                        bonus = PayrollCalculationRules.RoundMoney(originalBonus * prorationFactor);
                    }
                    else
                    {
                        basicAfterJoining = PayrollCalculationRules.RoundMoney(weightedBasicSum / daysInMonth);
                        hra = PayrollCalculationRules.RoundMoney(weightedHraSum / daysInMonth);
                        lta = PayrollCalculationRules.RoundMoney(weightedLtaSum / daysInMonth);
                        otherAllowance = PayrollCalculationRules.RoundMoney(weightedOtherSum / daysInMonth);
                        bonus = PayrollCalculationRules.RoundMoney(weightedBonusSum / daysInMonth);
                    }

                    adjustmentsByEmployee.TryGetValue(emp.EmployeeId, out var employeeAdjustments);
                    employeeAdjustments ??= [];
                    decimal earningAdjustmentsSum = employeeAdjustments
                        .Where(x => string.Equals(x.ComponentKind, "EARNING", StringComparison.OrdinalIgnoreCase))
                        .Sum(x => x.Amount);
                    otherAllowance = PayrollCalculationRules.RoundMoney(otherAllowance + earningAdjustmentsSum);

                    // Transparent payslip model: keep earned/prorated basic before attendance LOP;
                    // LOP is stored once in the deductions section and subtracted once from net pay.
                    decimal finalBasic = basicAfterJoining;
                    decimal earnedFixedGross = finalBasic + hra + lta + otherAllowance + bonus;

                    decimal perDaySalary = PayrollCalculationRules.ResolvePerDayLopRate(
                        divisorSetting.LopCalculationBase,
                        originalBasic,
                        contractualFixedMonthlyGross,
                        resolvedDivisor);

                    var segmentDates = attendanceSegments.Select(x => x.Date.Date).ToHashSet();
                    var allowedLimit = 2;
                    if (_penaltyPolicies != null)
                    {
                        var pol = await _penaltyPolicies.FirstOrDefault(x =>
                            x.CompanyId == companyId && x.UserId == emp.UserId && x.IsEnabled && !x.IsDeleted &&
                            x.EffectiveFrom <= monthEnd && (!x.EffectiveTo.HasValue || x.EffectiveTo >= monthStart));
                        if (pol != null) allowedLimit = pol.CombinedLhdEdMonthlyLimit;
                    }
                    var effectiveMonthRecords = attendanceRecords.Where(a => !segmentDates.Contains(a.Date.Date));
                    var infractions = effectiveMonthRecords.Where(a => AttendancePayrollRules.IsPunctualityInfraction(a.Status))
                        .Select(a => (a.Date.Date, a.Status))
                        .Concat(attendanceSegments.Where(s => AttendancePayrollRules.IsPunctualityInfraction(s.Status)).Select(s => (s.Date.Date, s.Status)));
                    var graceDates = AttendancePayrollRules.ResolveGraceExemptDates(infractions, allowedLimit);

                    decimal configuredUnpaidDays = attendanceRecords.Where(a => !segmentDates.Contains(a.Date.Date)).Sum(a =>
                    {
                        var rawUnpaid = statusRules.TryGetValue(a.Status, out var rule)
                            ? rule.UnpaidDayFraction
                            : PayrollCalculationRules.ResolveFallbackUnpaidDayFraction(a.Status);
                        return AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction(a.Status, graceDates.Contains(a.Date.Date), rawUnpaid);
                    }) + attendanceSegments.Sum(a =>
                    {
                        var rawUnpaid = statusRules.TryGetValue(a.Status, out var rule) ? rule.UnpaidDayFraction : 0m;
                        return AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction(a.Status, graceDates.Contains(a.Date.Date), rawUnpaid);
                    });
                    decimal approvedPenaltyDays = attendanceException?.Status is "DEDUCTION_APPROVED" or "APPLIED_TO_PAYROLL"
                        ? attendanceException.DeductionDayFraction
                        : 0m;
                    decimal lossOfPayDays = isAttendanceExempt ? 0m : PayrollCalculationRules.TotalLossOfPayDays(configuredUnpaidDays, approvedPenaltyDays);
                    decimal lossOfPay = isAttendanceExempt ? 0m : PayrollCalculationRules.CalculateCappedLossOfPay(lossOfPayDays, perDaySalary, earnedFixedGross);

                    decimal paidDaysValue = isAttendanceExempt ? eligibleDays : Math.Max(0m, eligibleDays - lossOfPayDays);

                    decimal monthlyGross = earnedFixedGross;
                    decimal earnedGrossAfterLop = Math.Max(0m, monthlyGross - lossOfPay);

                    // Statutory EPF & ESIC auto-calculation according to company policy
                    decimal earnedBasicAfterLop = Math.Max(0m, finalBasic - Math.Min(finalBasic, lossOfPay));
                    decimal epf = PayrollCalculationRules.CalculateMonthlyEpf(
                        effectiveEpfMode,
                        earnedBasicAfterLop,
                        effectiveEpfWageCeiling,
                        effectiveEpfEmployeeRate);
                    decimal esic = PayrollCalculationRules.CalculateMonthlyEsic(
                        effectiveEsicMode,
                        contractualFixedMonthlyGross,
                        earnedGrossAfterLop,
                        effectiveEsicWageCeiling,
                        effectiveEsicEmployeeRate);

                    // Statutory Professional Tax (PT) auto-calculation according to state jurisdiction
                    var effectivePtMode = resolvedProfile?.PtMode ?? "STATUTORY";
                    var effectiveJurisdiction = _ptResolver.ResolveEffectiveState(
                        assignment?.WorkLocationState,
                        resolvedProfile?.StatutoryJurisdiction,
                        companyDefaultState);
                    decimal monthlyPt = _ptResolver.CalculateMonthlyPt(
                        effectiveJurisdiction,
                        earnedGrossAfterLop,
                        monthStart.Month,
                        emp.Gender,
                        effectivePtMode);

                    // Salary TDS follows the tax year in which salary is paid, not the month it was earned.
                    // Non-taxable earnings are excluded from taxable gross base, and one-time bonus is added once.
                    var priorFyPayrolls = _empPayRollRepository
                        .Get(p => p.CompanyId == companyId && p.EmployeeId == emp.EmployeeId &&
                            (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED"))
                        .ToList()
                        .Where(p =>
                        {
                            var priorPaymentDate = p.CalculationSnapshot?.TdsPaymentDate?.Date ?? p.PaidDate.Date;
                            var priorTaxYear = p.CalculationSnapshot?.TdsFinancialYear ?? PayrollTaxPeriod.ResolveIndiaSalaryPayment(priorPaymentDate).FinancialYear;
                            return string.Equals(priorTaxYear, tdsPeriod.FinancialYear, StringComparison.Ordinal) &&
                                   priorPaymentDate >= tdsPeriod.StartDate && priorPaymentDate < tdsPaymentDate;
                        })
                        .ToList();

                    var tdsTaxMonth = new DateTime(tdsPaymentDate.Year, tdsPaymentDate.Month, 1);
                    int remainingMonthsInFy = ((tdsPeriod.EndDateExclusive.Year - tdsTaxMonth.Year) * 12) +
                                              (tdsPeriod.EndDateExclusive.Month - tdsTaxMonth.Month);
                    remainingMonthsInFy = Math.Clamp(remainingMonthsInFy, 1, 12);

                    decimal ytdTaxAlreadyDeducted = priorFyPayrolls.Sum(p => p.Deduction?.IncomeTax ?? 0m);
                    decimal ytdActualGross = priorFyPayrolls.Sum(p =>
                        Math.Max(0m, p.BasicPay + (p.Allowance?.HRA ?? 0m) + (p.Allowance?.LTA ?? 0m) + (p.Allowance?.OtherAllowance ?? 0m) + p.Bonus - (p.Deduction?.LossOfPay ?? 0m)));
                    decimal nonTaxableMonthlyEarnings = canonicalRecords
                        .SelectMany(r => r.Components)
                        .Where(c => string.Equals(c.Kind, "EARNING", StringComparison.OrdinalIgnoreCase) && !c.IsTaxable)
                        .Sum(c => c.Value);
                    decimal taxableContractualFixedMonthlyGross = Math.Max(0m, contractualFixedMonthlyGross - nonTaxableMonthlyEarnings);
                    decimal taxableEarnedGrossAfterLop = Math.Max(0m, earnedGrossAfterLop - nonTaxableMonthlyEarnings);

                    decimal annualGrossIncome;
                    int futureMonthsAfterCurrent = Math.Max(0, remainingMonthsInFy - 1);
                    int divisorMonths = remainingMonthsInFy;

                    if (priorFyPayrolls.Count > 0)
                    {
                        annualGrossIncome = ytdActualGross + taxableEarnedGrossAfterLop + (taxableContractualFixedMonthlyGross * futureMonthsAfterCurrent);
                    }
                    else if (joiningDate.Date >= tdsPeriod.StartDate)
                    {
                        // Mid-year joiner with no prior FY payrolls: project only for remaining months employed in this FY
                        annualGrossIncome = Math.Max(0m, taxableEarnedGrossAfterLop + (taxableContractualFixedMonthlyGross * futureMonthsAfterCurrent) + bonus);
                    }
                    else
                    {
                        if (tdsTaxMonth == tdsPeriod.StartDate)
                        {
                            annualGrossIncome = Math.Max(0m, taxableEarnedGrossAfterLop + (taxableContractualFixedMonthlyGross * futureMonthsAfterCurrent) + bonus);
                        }
                        else
                        {
                            annualGrossIncome = Math.Max(0m, (taxableContractualFixedMonthlyGross * 12m) - lossOfPay + bonus);
                            divisorMonths = remainingMonthsInFy;
                        }
                    }

                    EmployeeTaxDeclaration? employeeTaxDeclaration = null;
                    if (_taxDeclarations is not null)
                    {
                        employeeTaxDeclaration = (await _taxDeclarations.GetAll(x =>
                            x.CompanyId == companyId &&
                            x.EmployeeId == emp.EmployeeId &&
                            x.FinancialYear == tdsPeriod.FinancialYear &&
                            x.Status == "APPROVED"))
                            .OrderByDescending(x => x.Version)
                            .FirstOrDefault();
                    }

                    if (employeeTaxDeclaration is not null)
                    {
                        effectiveTdsMode = employeeTaxDeclaration.Regime == "OLD_REGIME" ? "AUTO_OLD_REGIME" : "AUTO_NEW_REGIME";
                    }

                    decimal monthlyIncomeTax;
                    decimal annualTaxEstimate;
                    string tdsEstimateStatus;
                    List<string> tdsEstimateLimitations;
                    decimal prevEmployerSalary = employeeTaxDeclaration?.PreviousEmployerGrossSalary ?? 0m;
                    decimal prevEmployerTds = employeeTaxDeclaration?.PreviousEmployerTds ?? 0m;
                    decimal otherIncomeDeclared = (employeeTaxDeclaration?.OtherIncomeSavingsInterest ?? 0m) + (employeeTaxDeclaration?.OtherIncomeOther ?? 0m);
                    decimal app80C = employeeTaxDeclaration?.ApprovedSection80C ?? 0m;
                    decimal app80Ccd1B = employeeTaxDeclaration?.ApprovedSection80Ccd1B ?? 0m;
                    decimal app80D = employeeTaxDeclaration?.ApprovedSection80D ?? 0m;
                    decimal appSec24 = employeeTaxDeclaration?.ApprovedSection24HomeLoanInterest ?? 0m;
                    decimal appHra = employeeTaxDeclaration?.ApprovedHraExemption ?? 0m;
                    decimal appOtherDeductions = (employeeTaxDeclaration?.ApprovedSection80E ?? 0m) + (employeeTaxDeclaration?.ApprovedOtherDeductions ?? 0m);
                    decimal totalDeductionsDeclared = app80C + app80Ccd1B + app80D + appSec24 + appHra + appOtherDeductions;

                    if (string.Equals(effectiveTdsMode, "MANUAL_ONLY", StringComparison.OrdinalIgnoreCase))
                    {
                        monthlyIncomeTax = 0m;
                        annualTaxEstimate = 0m;
                        tdsEstimateStatus = "MANUAL_REVIEW";
                        tdsEstimateLimitations = [];
                    }
                    else if (employeeTaxDeclaration is not null)
                    {
                        monthlyIncomeTax = PayrollCalculationRules.CalculateMonthlyTds(
                            annualGrossIncome,
                            effectiveTdsMode,
                            ytdTaxAlreadyDeducted,
                            divisorMonths,
                            prevEmployerSalary,
                            prevEmployerTds,
                            otherIncomeDeclared,
                            app80C,
                            app80Ccd1B,
                            app80D,
                            appSec24,
                            appHra,
                            appOtherDeductions);

                        annualTaxEstimate = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(
                            annualGrossIncome,
                            effectiveTdsMode,
                            prevEmployerSalary,
                            otherIncomeDeclared,
                            app80C,
                            app80Ccd1B,
                            app80D,
                            appSec24,
                            appHra,
                            appOtherDeductions);

                        tdsEstimateStatus = "APPROVED_DECLARATION_PROJECTION";
                        tdsEstimateLimitations =
                        [
                            $"Calculated using approved employee tax declaration for FY {tdsPeriod.FinancialYear} under {employeeTaxDeclaration.Regime}.",
                            $"Approved deductions: {totalDeductionsDeclared:C}, Previous employer taxable salary: {prevEmployerSalary:C}, Previous TDS: {prevEmployerTds:C}."
                        ];
                    }
                    else
                    {
                        monthlyIncomeTax = PayrollCalculationRules.CalculateMonthlyTds(
                            annualGrossIncome,
                            effectiveTdsMode,
                            ytdTaxAlreadyDeducted,
                            divisorMonths);

                        annualTaxEstimate = PayrollCalculationRules.CalculateAnnualIncomeTaxWithCess(annualGrossIncome, effectiveTdsMode);
                        tdsEstimateStatus = "INCOMPLETE_SALARY_ONLY_ESTIMATE";
                        tdsEstimateLimitations =
                        [
                            "Previous-employer salary and tax deducted are not included unless represented by payroll rows in this company.",
                            "For a mid-year joiner with no prior payroll rows in this tax year, salary already earned before the current payroll month is not included in the projection.",
                            "Employee declarations, eligible deduction claims, submitted evidence, and reviewer decisions are not captured by the current payroll flow.",
                            "Other income, perquisites, and tax adjustments outside this employer's payroll are not included.",
                            "This is a salary-only estimate from the current payroll calculation rules; it is not a complete statutory tax computation."
                        ];
                    }

                    var salaryDeductionLines = weightedSalaryDeductions
                        .Select(x => new PayrollDeductionLineDto
                        {
                            Code = $"SALARY_{x.Key.Trim().ToUpperInvariant()}",
                            Description = x.Value.Name,
                            Amount = daysInMonth > 0 ? PayrollCalculationRules.RoundMoney(x.Value.Amount / daysInMonth) : 0m
                        })
                        .Where(x => x.Amount > 0m)
                        .ToList();
                    var payrollDeductionLines = salaryDeductionLines;
                    if (approvedPenaltyDays > 0)
                    {
                        payrollDeductionLines.Add(new PayrollDeductionLineDto
                        {
                            Code = "LHD_ED_POLICY", Description = "Approved LHD/ED monthly policy deduction",
                            DayFraction = approvedPenaltyDays, Amount = PayrollCalculationRules.RoundMoney(approvedPenaltyDays * perDaySalary),
                            SourceId = attendanceException?.Id
                        });
                    }
                    foreach (var adj in employeeAdjustments.Where(x => string.Equals(x.ComponentKind, "DEDUCTION", StringComparison.OrdinalIgnoreCase)))
                    {
                        payrollDeductionLines.Add(new PayrollDeductionLineDto
                        {
                            Code = $"ADJ_{adj.ComponentCode?.Trim().ToUpperInvariant() ?? "DEDUCTION"}",
                            Description = $"{adj.Type}: {adj.ComponentName ?? adj.Reason}",
                            Amount = adj.Amount,
                            SourceId = adj.Id
                        });
                    }
                    if (monthlyPt > 0m)
                    {
                        payrollDeductionLines.Add(new PayrollDeductionLineDto
                        {
                            Code = "PT",
                            Description = $"Professional Tax ({effectiveJurisdiction ?? "Statutory"})",
                            Amount = monthlyPt
                        });
                    }

                    var otherAllowanceLines = weightedSalaryEarnings
                        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new PayrollEarningLineDto
                        {
                            Code = x.Key.Trim().ToUpperInvariant(),
                            Description = x.Value.Name,
                            Amount = daysInMonth > 0 ? PayrollCalculationRules.RoundMoney(x.Value.Amount / daysInMonth) : 0m
                        })
                        .Where(x => x.Amount > 0m)
                        .ToList();
                    foreach (var adj in employeeAdjustments.Where(x => string.Equals(x.ComponentKind, "EARNING", StringComparison.OrdinalIgnoreCase)))
                    {
                        otherAllowanceLines.Add(new PayrollEarningLineDto
                        {
                            Code = string.IsNullOrWhiteSpace(adj.ComponentCode) ? "ADJUSTMENT" : adj.ComponentCode.Trim().ToUpperInvariant(),
                            Description = $"{adj.Type}: {adj.ComponentName ?? adj.Reason}",
                            Amount = adj.Amount
                        });
                    }
                    if (otherAllowanceLines.Count > 0)
                    {
                        // Keep the breakdown's displayed sum aligned exactly with the
                        // canonical OtherAllowance total after per-line currency rounding.
                        var roundingDifference = otherAllowance - otherAllowanceLines.Sum(x => x.Amount);
                        otherAllowanceLines[^1].Amount = PayrollCalculationRules.RoundMoney(otherAllowanceLines[^1].Amount + roundingDifference);
                    }

                    var payrollDto = new AddUpdatePayRollRequestDto
                    {
                        EmployeeId = emp.EmployeeId,
                        PayMonth = monthStart,
                        PaidDate = DateTime.UtcNow,
                        BasicPay = finalBasic,
                        HRA = hra,
                        LTA = lta,
                        OtherAllowance = otherAllowance,
                        EarningLines = otherAllowanceLines,
                        ReplaceEarningLines = true,
                        Bonus = bonus,
                        PaidDays = (float)paidDaysValue,
                        LossOfPayDays = lossOfPayDays,
                        LossOfPay = lossOfPay,
                        EPF = epf,
                        ESIC = esic,
                        ProfessionalTax = monthlyPt,
                        IncomeTax = monthlyIncomeTax,
                        HealthInsurance = daysInMonth > 0 ? PayrollCalculationRules.RoundMoney(weightedHealthInsuranceSum / daysInMonth) : 0m,
                        DeductionLines = payrollDeductionLines,
                        ReplaceDeductionLines = true,
                        CalculationSnapshot = new PayrollCalculationSnapshotDto
                        {
                            PayrollMonth = monthStart,
                            TdsPaymentDate = tdsPaymentDate,
                            TdsFinancialYear = tdsPeriod.FinancialYear,
                            TdsIncomeTaxAct = tdsPeriod.IncomeTaxAct,
                            TdsSection = tdsPeriod.SalaryTdsSection,
                            TdsJurisdiction = tdsPolicyProfile?.StatutoryJurisdiction,
                            TdsPolicyProfileId = tdsPolicyProfile?.Id,
                            TdsPolicyProfileVersion = tdsPolicyProfile?.Version ?? 0,
                            TdsStatutoryRuleVersion = tdsPolicyProfile?.StatutoryRuleVersion,
                            TdsEstimateStatus = tdsEstimateStatus,
                            TdsCurrentYearTaxableSalaryYtd = ytdActualGross,
                            TdsCurrentMonthlyTaxableSalaryBasis = taxableContractualFixedMonthlyGross,
                            TdsProjectedAnnualTaxableSalary = annualGrossIncome,
                            TdsPreviousEmployerSalary = prevEmployerSalary,
                            TdsOtherIncomeDeclared = otherIncomeDeclared,
                            TdsEligibleDeductionsDeclared = totalDeductionsDeclared,
                            TdsYtdTaxDeducted = ytdTaxAlreadyDeducted + prevEmployerTds,
                            TdsEstimatedAnnualTax = annualTaxEstimate,
                            TdsRemainingPayments = divisorMonths,
                            TdsEstimateLimitations = tdsEstimateLimitations,
                            JoiningDateUsed = joiningDate.Date, ExitDateUsed = exitDate,
                            EligibleFrom = eligibleFrom, EligibleTo = eligibleTo, DivisorPolicy = effectiveDivisorPolicy, Divisor = resolvedDivisor, DivisorPolicyVersion = resolvedProfile?.Version ?? divisorSetting.Version,
                            LhdCount = attendanceException?.LhdCount ?? (attendanceSummary?.LhdCount ?? 0),
                            EdCount = attendanceException?.EdCount ?? (attendanceSummary?.EdCount ?? 0),
                            CombinedCount = attendanceException?.CombinedOccurrenceCount ?? ((attendanceSummary?.LhdCount ?? 0) + (attendanceSummary?.EdCount ?? 0)),
                            AllowedCount = attendanceException?.AllowedOccurrenceCount ?? 0,
                            ExceededCount = attendanceException?.ExceededOccurrenceCount ?? 0,
                            ExceptionDecision = attendanceException?.Decision, PenaltyDayFraction = approvedPenaltyDays,
                            PenaltyAmount = PayrollCalculationRules.RoundMoney(approvedPenaltyDays * perDaySalary), PolicyId = attendanceException?.PolicyId,
                            PolicyVersion = attendanceException?.PolicyVersion ?? 0, AttendanceSummaryVersion = attendanceSummary?.Version ?? 0,
                             AttendanceSummaryId = attendanceSummary?.Id,
                             ExpectedWorkingDays = attendanceSummary?.ExpectedWorkingDays ?? 0,
                             EligibleWorkingDays = attendanceSummary?.EligibleWorkingDays ?? 0,
                             PresentDays = attendanceSummary?.PresentDays ?? 0,
                             WfhDays = attendanceSummary?.WfhDays ?? 0,
                             PaidLeaveDays = attendanceSummary?.PaidLeaveDays ?? 0,
                             UnpaidLeaveDays = attendanceSummary?.UnpaidLeaveDays ?? 0,
                             HalfDays = attendanceSummary?.HalfDays ?? 0,
                             AbsentDays = attendanceSummary?.AbsentDays ?? 0,
                             PaidDays = paidDaysValue,
                             UnpaidDays = attendanceSummary?.UnpaidDays ?? 0,
                             LossOfPayDays = lossOfPayDays,
                             LossOfPay = lossOfPay,
                             PayrollAssignmentId = assignment?.Id, PayrollAssignmentVersion = assignment?.Version ?? 0, PayGroupId = resolvedPolicyPeriod.PayGroupId ?? assignment?.PayGroupId, PayGroupVersion = payGroups.FirstOrDefault(x => x.Id == (resolvedPolicyPeriod.PayGroupId ?? assignment?.PayGroupId))?.Version ?? 0, SalaryTemplateId = assignedTemplate?.Id, SalaryTemplateVersion = assignedTemplate?.Version ?? 0, ResolvedPolicyProfileId = resolvedProfile?.Id, ResolvedPolicyVersion = resolvedProfile?.Version ?? 0, EpfMode = effectiveEpfMode, EpfWageCeiling = effectiveEpfWageCeiling, EpfEmployeeRatePercent = effectiveEpfEmployeeRate, EpfEmployerRatePercent = effectiveEpfEmployerRate, EsicMode = effectiveEsicMode, EsicWageCeiling = effectiveEsicWageCeiling, EsicEmployeeRatePercent = effectiveEsicEmployeeRate, EsicEmployerRatePercent = effectiveEsicEmployerRate, TdsMode = effectiveTdsMode, ProfessionalTax = monthlyPt, PtJurisdiction = effectiveJurisdiction, PtMode = effectivePtMode,
                            SalaryRevisions = canonicalRecords.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Version).Select(x => new PayrollSalaryRevisionReferenceDto { SalaryStructureId = x.Id, Version = x.Version }).ToList(),
                            SalaryStructures = canonicalRecords.Count > 0
                                ? canonicalRecords.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Version).Select(x => new PayrollSalaryStructureSnapshotDto
                            {
                                SalaryStructureId = x.Id, Version = x.Version, PayGroupId = x.PayGroupId,
                                EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo,
                                Components = x.Components.Select((component, index) => new PayrollComponentSnapshotDto
                                {
                                    Code = component.ComponentCode, Name = component.ComponentName, Kind = component.Kind,
                                    CalculationType = component.CalculationType, BaseComponentCode = component.BaseComponentCode,
                                    Value = component.Percentage ?? component.Value, IsTaxable = component.IsTaxable,
                                    IsRecurring = component.IsRecurring, IsStatutory = component.IsStatutory,
                                    IsEmployerContribution = component.IsEmployerContribution, SortOrder = index
                                }).ToList()
                            }).ToList()
                                : salaryResolution.UsesCanonicalSource ? []
                                : salaryResolution.Records.OrderBy(x => x.EffectiveFrom).Select(PayrollSalarySourceResolver.SnapshotLegacyRecordDto).ToList()
                        }
                    };

                    // --- Check for existing payroll ---

                    var existingPayroll = _empPayRollRepository.Get(p =>
                        p.EmployeeId == emp.EmployeeId &&
                        p.CompanyId == companyId &&
                        p.PayMonth == payMonth)
                        // Preserve a published payroll snapshot if legacy duplicate data exists. The
                        // service will reject any attempted change to it; ordering also prevents an
                        // arbitrary draft from being selected by the repository.
                        .OrderByDescending(p => p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")
                        .ThenByDescending(p => p.ProcessedAt ?? DateTime.MinValue)
                        .ThenByDescending(p => p.CreatedAt)
                        .FirstOrDefault();
                    var originalPayroll = existingPayroll is null ? null : ClonePayroll(existingPayroll);

                    if (existingPayroll != null)
                    {
                        if (string.Equals(effectiveTdsMode, "MANUAL_ONLY", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(existingPayroll.CalculationSnapshot?.TdsMode, "MANUAL_ONLY", StringComparison.OrdinalIgnoreCase))
                        {
                            // A manually entered draft withholding must survive regeneration; do not recalculate it as zero.
                            payrollDto.IncomeTax = existingPayroll.Deduction.IncomeTax;
                        }
                        payrollDto.PayRollId = existingPayroll.Id; // Update existing payroll
                    }

                    // Add or update payroll
                    var saveResult = await _payRollServices.AddUpdatePayRoll(payrollDto, companyId);
                    if (!saveResult.Success)
                    {
                        await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                        return saveResult;
                    }
                    payrollMutations.Add(new PayrollMutation(emp.EmployeeId, originalPayroll, attendanceException?.Id, attendanceException?.Status));
                    if (attendanceException?.Status == "DEDUCTION_APPROVED")
                    {
                        attendanceException.Status = "APPLIED_TO_PAYROLL";
                        attendanceException.UpdatedDate = DateTime.UtcNow;
                        await _attendanceExceptionRepository.Update(
                            Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, attendanceException.Id) & Builders<AttendancePayrollException>.Filter.Eq(x => x.CompanyId, companyId), attendanceException);
                    }
                    processedCount++;
                }
            }
            catch
            {
                await RollbackPayrollRunAsync(companyId, monthStart, payrollMutations);
                throw;
            }

            var skippedIdsPreview = string.Join(", ", skippedNoSalaryEmployeeIds.Take(10));
            var remainingSkippedCount = skippedNoSalaryCount - Math.Min(skippedNoSalaryCount, 10);
            var skippedIdentityText = remainingSkippedCount > 0 ? $" Employee IDs: {skippedIdsPreview}, and {remainingSkippedCount} more." : $" Employee IDs: {skippedIdsPreview}.";
            return new Result
            {
                Success = true,
                Message = skippedNoSalaryCount > 0
                    ? $"Generated payroll draft for {processedCount} of {consideredCount} employees. {skippedNoSalaryCount} employee(s) have no salary structure defined.{skippedIdentityText}"
                    : $"Generated payroll draft for {processedCount} employee(s). Review it and process the month to publish payslips."
            };
        }

        // This service currently writes through legacy repositories that do not expose a
        // shared MongoDB session. Keep the operation all-or-nothing with compensation:
        // newly created drafts are deleted and pre-existing drafts/exception states are
        // restored whenever any later calculation or persistence step fails.
        private async Task RollbackPayrollRunAsync(string companyId, DateTime monthStart, IEnumerable<PayrollMutation> mutations)
        {
            foreach (var mutation in mutations.Reverse())
            {
                var payrollFilter = Builders<EmpPayRoll>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeId == mutation.EmployeeId &&
                    x.PayMonth == monthStart);
                if (mutation.OriginalPayroll is null)
                    await _empPayRollRepository.GetCollection().DeleteOneAsync(payrollFilter);
                else
                    await _empPayRollRepository.GetCollection().ReplaceOneAsync(
                        Builders<EmpPayRoll>.Filter.Eq(x => x.Id, mutation.OriginalPayroll.Id) & Builders<EmpPayRoll>.Filter.Eq(x => x.CompanyId, companyId), mutation.OriginalPayroll);

                if (mutation.AttendanceExceptionId is not null && mutation.OriginalAttendanceExceptionStatus is not null)
                {
                    await _attendanceExceptionRepository.GetCollection().UpdateOneAsync(
                        Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, mutation.AttendanceExceptionId) & Builders<AttendancePayrollException>.Filter.Eq(x => x.CompanyId, companyId),
                        Builders<AttendancePayrollException>.Update
                            .Set(x => x.Status, mutation.OriginalAttendanceExceptionStatus)
                            .Set(x => x.UpdatedDate, DateTime.UtcNow));
                }
            }
        }

        private static EmpPayRoll ClonePayroll(EmpPayRoll payroll) =>
            MongoDB.Bson.Serialization.BsonSerializer.Deserialize<EmpPayRoll>(payroll.ToBsonDocument());

        private sealed record PayrollMutation(string EmployeeId, EmpPayRoll? OriginalPayroll,
            string? AttendanceExceptionId, string? OriginalAttendanceExceptionStatus);

        private decimal CalculateMonthlyIncomeTax(decimal annualGrossIncome, bool isNewRegime = true) =>
            PayrollCalculationRules.CalculateMonthlyTds(annualGrossIncome, isNewRegime ? "AUTO_NEW_REGIME" : "AUTO_OLD_REGIME");
    }
}

