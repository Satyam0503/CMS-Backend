using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Attendance;
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
            IMongoDbRepository<EmployeeSalaryStructure>? employeeSalaryStructures = null)
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
        }

        public async Task<Result> GeneratePayrollForMonthAsync(string companyId, DateTime payMonth)
        {
            var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
            // Payroll keys are calendar months. Use the canonical first-of-month value for
            // every read and write so a time component or client timezone cannot create a
            // second key for the same displayed month.
            payMonth = monthStart;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var employees = (await _employeeRepository.GetAll(e => e.CompanyId == companyId && !e.IsDeleted))
                .Where(e => EmploymentPeriod.IsPayrollIncludedForMonth(e, monthStart))
                .ToList();
            if (await _empPayRollRepository.Exist(p => p.CompanyId == companyId &&
                p.PayMonth == monthStart && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")))
                return new Result { Success = false, Message = $"Payroll for {monthStart:MMMM yyyy} is already processed and cannot be regenerated." };

            var divisorSetting = await _divisorPolicies.GetEffective(companyId, monthStart);
            var divisorPolicy = divisorSetting.DivisorPolicy;
            var requireAttendanceLock = divisorSetting.RequireAttendanceLock;
            var policyProfiles = await _policyProfiles.GetAll(x => x.CompanyId == companyId && x.IsActive);
            var payGroups = _payGroups is null ? Array.Empty<PayrollPayGroup>() : (await _payGroups.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToArray();
            var salaryTemplates = _salaryTemplates is null ? Array.Empty<SalaryTemplate>() : (await _salaryTemplates.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToArray();
            var canonicalSalaryStructures = _employeeSalaryStructures is null ? Array.Empty<EmployeeSalaryStructure>() : (await _employeeSalaryStructures.GetAll(x => x.CompanyId == companyId && x.ApprovalStatus == "APPROVED" && x.Status == "ACTIVE")).ToArray();

            // Validate every eligible employee before the first payroll write. This prevents a
            // later employee validation failure from leaving an unnoticed partial company run.
            var validationErrors = new List<string>();
            foreach (var employee in employees)
            {
                // Payroll cannot be calculated without an effective joining date. Skip this
                // employee instead of blocking a completed historical month for everyone else.
                if (!DateTime.TryParse(employee.DateOfJoining, out var joining)) continue;
                if (joining.Date > monthEnd) continue;
                var exitDate = EmploymentPeriod.GetLastWorkingDate(employee);
                if (exitDate.HasValue && exitDate.Value < monthStart) continue;
                if (exitDate.HasValue && exitDate.Value < joining.Date) { validationErrors.Add($"{employee.EmployeeId}: exit date cannot be before joining date."); continue; }
                var validationAssignment = PayrollPolicyResolver.ResolveAssignment(companyId, employee.EmployeeId, monthStart, await _assignments.GetAll(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId));
                var validationProfile = PayrollPolicyResolver.ResolvePolicy(companyId, monthStart, validationAssignment, policyProfiles, payGroups);
                var validationRequiresAttendanceLock = validationProfile?.RequireAttendanceLock ?? requireAttendanceLock;
                var summary = await _attendanceSummaryRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == monthStart && x.IsApproved && x.IsLocked);
                if (summary == null && validationRequiresAttendanceLock) validationErrors.Add($"{employee.EmployeeId}: monthly attendance must be approved and locked.");
                // Missing salary structure is not a company-wide blocker: the generation loop below
                // already skips that employee individually (tracked via skippedNoSalaryCount) and
                // reports the count in the result message, so other employees can still be paid.
                if (await _attendanceExceptionRepository.Exist(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == monthStart && x.Status == "PENDING_REVIEW")) validationErrors.Add($"{employee.EmployeeId}: unresolved attendance exception requires review.");
            }
            if (validationErrors.Count > 0) return new Result { Success = false, Message = "Payroll validation failed. No payroll records were changed. " + string.Join("; ", validationErrors) };

            int processedCount = 0;
            int skippedNoSalaryCount = 0;
            int consideredCount = 0;
            var payrollMutations = new List<PayrollMutation>();

            try
            {
                foreach (var emp in employees)
                {
                    if (string.IsNullOrEmpty(emp.DateOfJoining))
                        continue;

                    if (!DateTime.TryParse(emp.DateOfJoining, out DateTime joiningDate))
                        continue;

                    if (joiningDate.Date > monthEnd)
                        continue;

                    DateTime? exitDate = EmploymentPeriod.GetLastWorkingDate(emp);
                    if (exitDate.HasValue && exitDate.Value < monthStart)
                        continue;

                    var eligibleFrom = joiningDate.Date > monthStart ? joiningDate.Date : monthStart;
                    var eligibleTo = exitDate.HasValue && exitDate.Value < monthEnd ? exitDate.Value : monthEnd;

                    var assignment = PayrollPolicyResolver.ResolveAssignment(companyId, emp.EmployeeId, monthStart, await _assignments.GetAll(x => x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId));
                    var resolvedProfile = PayrollPolicyResolver.ResolvePolicy(companyId, monthStart, assignment, policyProfiles, payGroups);
                    var effectiveDivisorPolicy = resolvedProfile?.DivisorPolicy ?? divisorPolicy;
                    var effectiveRequireAttendanceLock = resolvedProfile?.RequireAttendanceLock ?? requireAttendanceLock;
                    var effectiveEpfMode = resolvedProfile?.EpfMode ?? divisorSetting.EpfMode;
                    var effectiveEpfWageCeiling = resolvedProfile?.EpfWageCeiling ?? divisorSetting.EpfWageCeiling;
                    var effectiveEsicMode = resolvedProfile?.EsicMode ?? divisorSetting.EsicMode;
                    var effectiveEsicWageCeiling = resolvedProfile?.EsicWageCeiling ?? divisorSetting.EsicWageCeiling;
                    var effectiveTdsMode = resolvedProfile?.TdsMode ?? divisorSetting.TdsMode;
                    var attendanceSummary = await _attendanceSummaryRepository.FirstOrDefault(x =>
                        x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId &&
                        x.PayrollMonth == monthStart && x.IsApproved && x.IsLocked);
                    if (attendanceSummary == null && effectiveRequireAttendanceLock)
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

                    var canonicalRecords = canonicalSalaryStructures
                        .Where(x => x.EmployeeId == emp.EmployeeId && x.EffectiveFrom.Date <= monthEnd.Date && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= monthStart.Date))
                        .Select(MapCanonicalSalary)
                        .ToList();
                    var overlappingSalaryRecords = canonicalRecords.Count > 0
                        ? canonicalRecords
                        : _salaryRepository
                            .Get(s => s.CompanyId == companyId && s.EmployeeId == emp.EmployeeId && s.EffectiveFrom <= monthEnd && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= monthStart))
                            .OrderByDescending(s => s.EffectiveFrom)
                            .ToList();

                    var latestSalaryRecord = overlappingSalaryRecords.FirstOrDefault();
                    var assignedTemplate = assignment?.SalaryTemplateId is null ? null : salaryTemplates.FirstOrDefault(x => x.Id == assignment.SalaryTemplateId && (x.PayGroupId is null || x.PayGroupId == assignment.PayGroupId) && x.EffectiveFrom.Date <= monthStart.Date && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= monthStart.Date));
                    if (latestSalaryRecord == null)
                    {
                        skippedNoSalaryCount++;
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
                    }

                    decimal originalBasic = assignedTemplate?.BasicPay ?? (eligibleDays > 0 ? weightedBasicSum / eligibleDays : latestSalaryRecord?.BasicPay ?? 0m);
                    decimal originalHRA = assignedTemplate?.Hra ?? (eligibleDays > 0 ? weightedHraSum / eligibleDays : (latestSalaryRecord?.Hra ?? 0m));
                    decimal originalLTA = assignedTemplate?.Lta ?? (eligibleDays > 0 ? weightedLtaSum / eligibleDays : (latestSalaryRecord?.Lta ?? 0m));
                    decimal originalOtherAllowance = assignedTemplate?.OtherAllowance ?? (eligibleDays > 0 ? weightedOtherSum / eligibleDays : (latestSalaryRecord?.OtherAllowances ?? 0m));
                    decimal originalBonus = assignedTemplate?.Bonus ?? (eligibleDays > 0 ? weightedBonusSum / eligibleDays : (latestSalaryRecord?.Bonus ?? 0m));
                    decimal contractualFixedMonthlyGross = originalBasic + originalHRA + originalLTA + originalOtherAllowance;

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

                    // Joining / exit / split-month earned components
                    decimal basicAfterJoining = PayrollCalculationRules.RoundMoney(weightedBasicSum / daysInMonth);
                    decimal hra = PayrollCalculationRules.RoundMoney(weightedHraSum / daysInMonth);
                    decimal lta = PayrollCalculationRules.RoundMoney(weightedLtaSum / daysInMonth);
                    decimal otherAllowance = PayrollCalculationRules.RoundMoney(weightedOtherSum / daysInMonth);
                    decimal bonus = PayrollCalculationRules.RoundMoney(weightedBonusSum / daysInMonth);

                    // Transparent payslip model: keep earned/prorated basic before attendance LOP;
                    // LOP is stored once in the deductions section and subtracted once from net pay.
                    decimal finalBasic = basicAfterJoining;
                    decimal earnedFixedGross = finalBasic + hra + lta + otherAllowance;

                    decimal perDaySalary = PayrollCalculationRules.ResolvePerDayLopRate(
                        divisorSetting.LopCalculationBase,
                        originalBasic,
                        contractualFixedMonthlyGross,
                        resolvedDivisor);

                    var segmentDates = attendanceSegments.Select(x => x.Date.Date).ToHashSet();
                    decimal configuredUnpaidDays = attendanceRecords.Where(a => !segmentDates.Contains(a.Date.Date)).Sum(a => statusRules.TryGetValue(a.Status, out var rule)
                        ? rule.UnpaidDayFraction
                        : PayrollCalculationRules.ResolveFallbackUnpaidDayFraction(a.Status))
                        + attendanceSegments.Sum(a => statusRules.TryGetValue(a.Status, out var rule) ? rule.UnpaidDayFraction : 0m);
                    decimal approvedPenaltyDays = attendanceException?.Status is "DEDUCTION_APPROVED" or "APPLIED_TO_PAYROLL"
                        ? attendanceException.DeductionDayFraction
                        : 0m;
                    decimal lossOfPayDays = PayrollCalculationRules.TotalLossOfPayDays(configuredUnpaidDays, approvedPenaltyDays);
                    decimal lossOfPay = PayrollCalculationRules.CalculateCappedLossOfPay(lossOfPayDays, perDaySalary, earnedFixedGross);

                    decimal paidDaysValue = Math.Max(0m, eligibleDays - lossOfPayDays);

                    decimal monthlyGross = earnedFixedGross + bonus;
                    decimal earnedGrossAfterLop = Math.Max(0m, monthlyGross - lossOfPay);

                    // Statutory EPF & ESIC auto-calculation according to company policy
                    decimal earnedBasicAfterLop = Math.Max(0m, finalBasic - Math.Min(finalBasic, lossOfPay));
                    decimal epf = PayrollCalculationRules.CalculateMonthlyEpf(
                        effectiveEpfMode,
                        earnedBasicAfterLop,
                        effectiveEpfWageCeiling);
                    decimal esic = PayrollCalculationRules.CalculateMonthlyEsic(
                        effectiveEsicMode,
                        contractualFixedMonthlyGross,
                        earnedGrossAfterLop,
                        effectiveEsicWageCeiling);

                    // Year-To-Date (YTD) + Remaining FY Months projection for Section 192 Income Tax (TDS):
                    // One-time bonus is added once (never multiplied by 12), and full-year fixed earnings are projected accurately.
                    var fyStartYear = monthStart.Month >= 4 ? monthStart.Year : monthStart.Year - 1;
                    var fyStart = new DateTime(fyStartYear, 4, 1);
                    var fyEnd = new DateTime(fyStartYear + 1, 3, 1);
                    int remainingMonthsInFy = ((fyEnd.Year - monthStart.Year) * 12) + (fyEnd.Month - monthStart.Month) + 1;

                    var priorFyPayrolls = _empPayRollRepository
                        .Get(p => p.CompanyId == companyId && p.EmployeeId == emp.EmployeeId && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED") && p.PayMonth >= fyStart && p.PayMonth < monthStart)
                        .ToList();

                    decimal ytdTaxAlreadyDeducted = priorFyPayrolls.Sum(p => p.Deduction?.IncomeTax ?? 0m);
                    decimal annualGrossIncome;
                    if (priorFyPayrolls.Count > 0)
                    {
                        decimal ytdActualGross = priorFyPayrolls.Sum(p =>
                            Math.Max(0m, p.BasicPay + (p.Allowance?.HRA ?? 0m) + (p.Allowance?.LTA ?? 0m) + (p.Allowance?.OtherAllowance ?? 0m) + p.Bonus - (p.Deduction?.LossOfPay ?? 0m)));
                        int futureMonthsAfterCurrent = Math.Max(0, remainingMonthsInFy - 1);
                        annualGrossIncome = ytdActualGross + earnedGrossAfterLop + (contractualFixedMonthlyGross * futureMonthsAfterCurrent);
                    }
                    else
                    {
                        // Standalone / first-month projection: recurring fixed gross * 12, minus current month LOP, plus one-time bonus once
                        annualGrossIncome = Math.Max(0m, (contractualFixedMonthlyGross * 12m) - lossOfPay + bonus);
                    }

                    decimal monthlyIncomeTax = PayrollCalculationRules.CalculateMonthlyTds(
                        annualGrossIncome,
                        effectiveTdsMode,
                        ytdTaxAlreadyDeducted,
                        priorFyPayrolls.Count > 0 ? remainingMonthsInFy : 12);

                    var payrollDto = new AddUpdatePayRollRequestDto
                    {
                        EmployeeId = emp.EmployeeId,
                        PayMonth = monthStart,
                        PaidDate = DateTime.UtcNow,
                        BasicPay = finalBasic,
                        HRA = hra,
                        LTA = lta,
                        OtherAllowance = otherAllowance,
                        Bonus = bonus,
                        PaidDays = (float)paidDaysValue,
                        LossOfPayDays = lossOfPayDays,
                        LossOfPay = lossOfPay,
                        EPF = epf,
                        ESIC = esic,
                        IncomeTax = monthlyIncomeTax,
                        HealthInsurance = latestSalaryRecord?.HealthInsurance ?? 0,
                        DeductionLines = approvedPenaltyDays > 0 ? [new PayrollDeductionLineDto
                        {
                            Code = "LHD_ED_POLICY", Description = "Approved LHD/ED monthly policy deduction",
                            DayFraction = approvedPenaltyDays, Amount = PayrollCalculationRules.RoundMoney(approvedPenaltyDays * perDaySalary),
                            SourceId = attendanceException?.Id
                        }] : [],
                        CalculationSnapshot = new PayrollCalculationSnapshotDto
                        {
                            PayrollMonth = monthStart, JoiningDateUsed = joiningDate.Date, ExitDateUsed = exitDate,
                            EligibleFrom = eligibleFrom, EligibleTo = eligibleTo, DivisorPolicy = effectiveDivisorPolicy, Divisor = resolvedDivisor, DivisorPolicyVersion = resolvedProfile?.Version ?? divisorSetting.Version,
                            LhdCount = attendanceException?.LhdCount ?? (attendanceSummary?.LhdCount ?? 0),
                            EdCount = attendanceException?.EdCount ?? (attendanceSummary?.EdCount ?? 0),
                            CombinedCount = attendanceException?.CombinedOccurrenceCount ?? ((attendanceSummary?.LhdCount ?? 0) + (attendanceSummary?.EdCount ?? 0)),
                            AllowedCount = attendanceException?.AllowedOccurrenceCount ?? 0,
                            ExceededCount = attendanceException?.ExceededOccurrenceCount ?? 0,
                            ExceptionDecision = attendanceException?.Decision, PenaltyDayFraction = approvedPenaltyDays,
                            PenaltyAmount = PayrollCalculationRules.RoundMoney(approvedPenaltyDays * perDaySalary), PolicyId = attendanceException?.PolicyId,
                            PolicyVersion = attendanceException?.PolicyVersion ?? 0, AttendanceSummaryVersion = attendanceSummary?.Version ?? 0, PayrollAssignmentId = assignment?.Id, PayGroupId = assignment?.PayGroupId, SalaryTemplateId = assignment?.SalaryTemplateId, ResolvedPolicyProfileId = resolvedProfile?.Id, ResolvedPolicyVersion = resolvedProfile?.Version ?? 0, EpfMode = effectiveEpfMode, EpfWageCeiling = effectiveEpfWageCeiling, EsicMode = effectiveEsicMode, EsicWageCeiling = effectiveEsicWageCeiling, TdsMode = effectiveTdsMode
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

            return new Result
            {
                Success = true,
                Message = skippedNoSalaryCount > 0
                    ? $"Generated payroll draft for {processedCount} of {consideredCount} employees. {skippedNoSalaryCount} employee(s) have no salary structure defined."
                    : $"Generated payroll draft for {processedCount} employee(s). Review it and process the month to publish payslips."
            };
        }

        // This service currently writes through legacy repositories that do not expose a
        // shared MongoDB session. Keep the operation all-or-nothing with compensation:
        // newly created drafts are deleted and pre-existing drafts/exception states are
        // restored whenever any later calculation or persistence step fails.
        private static SalaryModel MapCanonicalSalary(EmployeeSalaryStructure structure)
        {
            decimal Component(string code) => structure.Components.FirstOrDefault(x => string.Equals(x.ComponentCode, code, StringComparison.OrdinalIgnoreCase))?.Value ?? 0m;
            var basic = Component("BASIC");
            var hra = Component("HRA");
            var lta = Component("LTA");
            var other = Component("OTHER_ALLOWANCE") + Component("SPECIAL_ALLOWANCE");
            var bonus = Component("BONUS");
            var gross = structure.MonthlyGross ?? basic + hra + lta + other;
            return new SalaryModel
            {
                SalaryId = Guid.TryParse(structure.Id, out var id) ? id : Guid.NewGuid(), UserId = Guid.TryParse(structure.UserId, out var userId) ? userId : Guid.Empty,
                CompanyId = structure.CompanyId, EmployeeId = structure.EmployeeId, BasicPay = basic, Hra = hra, Lta = lta,
                OtherAllowances = other, Bonus = bonus, GrossSalary = gross, NetSalary = gross, Ctc = structure.AnnualCtc ?? gross * 12,
                EffectiveFrom = structure.EffectiveFrom, EffectiveTo = structure.EffectiveTo, PaymentFrequency = structure.PaymentFrequency, Currency = structure.Currency, Status = true
            };
        }

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

