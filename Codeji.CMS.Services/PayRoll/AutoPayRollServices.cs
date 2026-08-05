using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.DTO.Salary;
using MongoDB.Driver.Linq;
using MongoDB.Driver;

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
            ICompanyWorkingCalendarService workingCalendar)
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
        }

        public async Task<Result> GeneratePayrollForMonthAsync(string companyId, DateTime payMonth)
{
    var employees = (await _employeeRepository.GetAll(e => e.CompanyId == companyId && e.Status && !e.IsDeleted)).ToList();
    var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
    if (await _empPayRollRepository.Exist(p => p.CompanyId == companyId &&
        p.PayMonth.Year == monthStart.Year && p.PayMonth.Month == monthStart.Month && p.IsProcessed))
        return new Result { Success = false, Message = $"Payroll for {monthStart:MMMM yyyy} is already processed and cannot be regenerated." };
    var divisorSetting = await _divisorPolicies.GetEffective(companyId, monthStart);
    var divisorPolicy = divisorSetting.DivisorPolicy;

    // Validate every eligible employee before the first payroll write. This prevents a
    // later employee validation failure from leaving an unnoticed partial company run.
    var validationErrors = new List<string>();
    foreach (var employee in employees)
    {
        // Payroll cannot be calculated without an effective joining date. Skip this
        // employee instead of blocking a completed historical month for everyone else.
        if (!DateTime.TryParse(employee.DateOfJoining, out var joining)) continue;
        if (joining.Date > monthEnd) continue;
        if (DateTime.TryParse(employee.ExitDate, out var exit) && exit.Date < monthStart) continue;
        if (DateTime.TryParse(employee.ExitDate, out exit) && exit.Date < joining.Date) { validationErrors.Add($"{employee.EmployeeId}: exit date cannot be before joining date."); continue; }
        var summary = await _attendanceSummaryRepository.FirstOrDefault(x => x.CompanyId==companyId && x.EmployeeId==employee.EmployeeId && x.PayrollMonth==monthStart && x.IsApproved && x.IsLocked);
        if (summary==null) validationErrors.Add($"{employee.EmployeeId}: monthly attendance must be approved and locked.");
        if (!_salaryRepository.Get(x=>x.CompanyId==companyId && x.EmployeeId==employee.EmployeeId && x.EffectiveFrom<=monthEnd && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value>=monthStart)).Any()) validationErrors.Add($"{employee.EmployeeId}: salary structure effective for the payroll month is required.");
        if (await _attendanceExceptionRepository.Exist(x=>x.CompanyId==companyId && x.EmployeeId==employee.EmployeeId && x.PayrollMonth==monthStart && x.Status=="PENDING_REVIEW")) validationErrors.Add($"{employee.EmployeeId}: unresolved attendance exception requires review.");
    }
    if (validationErrors.Count>0) return new Result { Success=false, Message="Payroll validation failed. No payroll records were changed. "+string.Join("; ",validationErrors) };

    int processedCount = 0;
    int skippedNoSalaryCount = 0;
    int consideredCount = 0;

    foreach (var emp in employees)
    {
        if (string.IsNullOrEmpty(emp.DateOfJoining))
            continue;

        if (!DateTime.TryParse(emp.DateOfJoining, out DateTime joiningDate))
            continue;

        if (joiningDate.Date > monthEnd)
            continue;

        DateTime? exitDate = DateTime.TryParse(emp.ExitDate, out var parsedExitDate) ? parsedExitDate.Date : null;
        if (exitDate.HasValue && exitDate.Value < monthStart)
            continue;

        var eligibleFrom = joiningDate.Date > monthStart ? joiningDate.Date : monthStart;
        var eligibleTo = exitDate.HasValue && exitDate.Value < monthEnd ? exitDate.Value : monthEnd;

        var attendanceSummary = await _attendanceSummaryRepository.FirstOrDefault(x =>
            x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId &&
            x.PayrollMonth == monthStart && x.IsApproved && x.IsLocked);
        if (attendanceSummary == null)
            return new Result { Success = false, Message = $"Attendance must be validated and locked before payroll. Missing lock for employee {emp.EmployeeId}." };

        var attendanceException = await _attendanceExceptionRepository.FirstOrDefault(x =>
            x.CompanyId == companyId && x.EmployeeId == emp.EmployeeId &&
            x.PayrollMonth == monthStart && x.ExceptionType == "LHD_ED_LIMIT_EXCEEDED" && x.Status != "CANCELLED");
        if (attendanceException?.Status == "PENDING_REVIEW")
            return new Result { Success = false, Message = $"Payroll is blocked: the LHD/ED exception for employee {emp.EmployeeId} requires HR/Admin review." };

        consideredCount++;

        var salaryRecords = _salaryRepository
            .Get(s => s.CompanyId == companyId && s.EmployeeId == emp.EmployeeId && s.EffectiveFrom <= monthEnd && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= monthStart))
            .OrderByDescending(s => s.EffectiveFrom).FirstOrDefault();

        if (salaryRecords == null)
        {
            skippedNoSalaryCount++;
            continue;
        }

        int daysInMonth = DateTime.DaysInMonth(payMonth.Year, payMonth.Month);

        decimal originalBasic = salaryRecords.BasicPay;
        decimal originalHRA = salaryRecords.Hra ?? 0m;
        decimal originalLTA = salaryRecords.Lta ?? 0m;
        decimal originalOtherAllowance = salaryRecords.OtherAllowances ?? 0m;
        decimal originalBonus = salaryRecords.Bonus ?? 0m;

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
        decimal resolvedDivisor = PayrollCalculationRules.ResolveDivisor(divisorPolicy, daysInMonth, workingDays);
        if (resolvedDivisor <= 0) return new Result { Success=false, Message=$"Invalid payroll divisor for employee {emp.EmployeeId}. No payroll was changed." };
        decimal perDaySalary = originalBasic / resolvedDivisor;

        var segmentDates = attendanceSegments.Select(x => x.Date.Date).ToHashSet();
        decimal configuredUnpaidDays = attendanceRecords.Where(a => !segmentDates.Contains(a.Date.Date)).Sum(a => statusRules.TryGetValue(a.Status, out var rule)
            ? rule.UnpaidDayFraction
            : a.Status == "A" ? 1m : (a.Status == "HD" || a.Status == "LHD" || a.Status == "WFH-HD") ? .5m : 0m)
            + attendanceSegments.Sum(a => statusRules.TryGetValue(a.Status, out var rule) ? rule.UnpaidDayFraction : 0m);
        decimal approvedPenaltyDays = attendanceException?.Status is "DEDUCTION_APPROVED" or "APPLIED_TO_PAYROLL"
            ? attendanceException.DeductionDayFraction
            : 0m;
        decimal lossOfPayDays = PayrollCalculationRules.TotalLossOfPayDays(configuredUnpaidDays,approvedPenaltyDays);
        decimal lossOfPay = Math.Round(lossOfPayDays * perDaySalary, 2);

        // Joining month proration
        int eligibleDays = (eligibleTo - eligibleFrom).Days + 1;
        decimal joiningProrationFactor = (decimal)eligibleDays / daysInMonth;

        decimal basicAfterJoining = Math.Round(originalBasic * joiningProrationFactor, 2);
        decimal hra = Math.Round(originalHRA * joiningProrationFactor, 2);
        decimal lta = Math.Round(originalLTA * joiningProrationFactor, 2);
        decimal otherAllowance = Math.Round(originalOtherAllowance * joiningProrationFactor, 2);
        decimal bonus = Math.Round(originalBonus * joiningProrationFactor, 2);

        // Transparent payslip model: keep earned/prorated basic before attendance LOP;
        // LOP is stored once in the deductions section and subtracted once from net pay.
        decimal finalBasic = basicAfterJoining;

        decimal paidDaysValue = Math.Max(0m, eligibleDays - lossOfPayDays);

        decimal monthlyGross = finalBasic + hra + lta + otherAllowance + bonus;
        decimal annualGrossIncome = monthlyGross * 12;
        decimal monthlyIncomeTax = CalculateMonthlyIncomeTax(annualGrossIncome, true);

        var payrollDto = new AddUpdatePayRollRequestDto
        {
            EmployeeId = emp.EmployeeId,
            PayMonth = payMonth,
            PaidDate = DateTime.UtcNow,
            BasicPay = finalBasic,
            HRA = hra,
            LTA = lta,
            OtherAllowance = otherAllowance,
            Bonus = bonus,
            PaidDays = (float)paidDaysValue,
            LossOfPayDays = lossOfPayDays,
            LossOfPay = lossOfPay,
            IncomeTax = monthlyIncomeTax,
            HealthInsurance = salaryRecords.HealthInsurance ?? 0,
            DeductionLines = approvedPenaltyDays > 0 ? [new PayrollDeductionLineDto
            {
                Code = "LHD_ED_POLICY", Description = "Approved LHD/ED monthly policy deduction",
                DayFraction = approvedPenaltyDays, Amount = Math.Round(approvedPenaltyDays * perDaySalary, 2),
                SourceId = attendanceException?.Id
            }] : [],
            CalculationSnapshot = new PayrollCalculationSnapshotDto
            {
                PayrollMonth = monthStart, JoiningDateUsed = joiningDate.Date, ExitDateUsed = exitDate,
                EligibleFrom = eligibleFrom, EligibleTo = eligibleTo, DivisorPolicy = divisorPolicy, Divisor = resolvedDivisor, DivisorPolicyVersion = divisorSetting.Version,
                LhdCount = attendanceException?.LhdCount ?? attendanceSummary.LhdCount,
                EdCount = attendanceException?.EdCount ?? attendanceSummary.EdCount,
                CombinedCount = attendanceException?.CombinedOccurrenceCount ?? attendanceSummary.LhdCount + attendanceSummary.EdCount,
                AllowedCount = attendanceException?.AllowedOccurrenceCount ?? 0,
                ExceededCount = attendanceException?.ExceededOccurrenceCount ?? 0,
                ExceptionDecision = attendanceException?.Decision, PenaltyDayFraction = approvedPenaltyDays,
                PenaltyAmount = Math.Round(approvedPenaltyDays * perDaySalary, 2), PolicyId = attendanceException?.PolicyId,
                PolicyVersion = attendanceException?.PolicyVersion ?? 0, AttendanceSummaryVersion = attendanceSummary.Version
            }
        };

        // --- Check for existing payroll ---

var existingPayroll = _empPayRollRepository.Get(p =>
    p.EmployeeId == emp.EmployeeId &&
    p.CompanyId == companyId &&
    p.PayMonth.Year == payMonth.Year &&
    p.PayMonth.Month == payMonth.Month)
.FirstOrDefault();

if (existingPayroll != null)
{
    payrollDto.PayRollId = existingPayroll.Id; // Update existing payroll
}

// Add or update payroll
var saveResult = await _payRollServices.AddUpdatePayRoll(payrollDto, companyId);
if (!saveResult.Success)
    return saveResult;
if (attendanceException?.Status == "DEDUCTION_APPROVED")
{
    attendanceException.Status = "APPLIED_TO_PAYROLL";
    attendanceException.UpdatedDate = DateTime.UtcNow;
    await _attendanceExceptionRepository.Update(
        Builders<AttendancePayrollException>.Filter.Eq(x => x.Id, attendanceException.Id), attendanceException);
}
processedCount++;
}

    return new Result
    {
        Success = true,
        Message = skippedNoSalaryCount > 0
            ? $"Generated payroll draft for {processedCount} of {consideredCount} employees. {skippedNoSalaryCount} employee(s) have no salary structure defined."
            : $"Generated payroll draft for {processedCount} employee(s). Review it and process the month to publish payslips."
    };
}

        private decimal CalculateMonthlyIncomeTax(decimal annualGrossIncome, bool isNewRegime = true)
        {
            decimal taxableIncome = annualGrossIncome;
            decimal tax = 0m;

            if (isNewRegime)
            {
                if (taxableIncome <= 300000)
                    tax = 0;
                else if (taxableIncome <= 600000)
                    tax = (taxableIncome - 300000) * 0.05m;
                else if (taxableIncome <= 900000)
                    tax = (300000 * 0.05m) +
                          (taxableIncome - 600000) * 0.10m;
                else if (taxableIncome <= 1200000)
                    tax = (300000 * 0.05m) +
                          (300000 * 0.10m) +
                          (taxableIncome - 900000) * 0.15m;
                else if (taxableIncome <= 1500000)
                    tax = (300000 * 0.05m) +
                          (300000 * 0.10m) +
                          (300000 * 0.15m) +
                          (taxableIncome - 1200000) * 0.20m;
                else
                    tax = (300000 * 0.05m) +
                          (300000 * 0.10m) +
                          (300000 * 0.15m) +
                          (300000 * 0.20m) +
                          (taxableIncome - 1500000) * 0.30m;

                if (taxableIncome <= 700000)
                    tax = 0;
            }
            else
            {
                if (taxableIncome <= 250000)
                    tax = 0;
                else if (taxableIncome <= 500000)
                    tax = (taxableIncome - 250000) * 0.05m;
                else if (taxableIncome <= 1000000)
                    tax = (250000 * 0.05m) +
                          (taxableIncome - 500000) * 0.20m;
                else
                    tax = (250000 * 0.05m) +
                          (500000 * 0.20m) +
                          (taxableIncome - 1000000) * 0.30m;

                if (taxableIncome <= 500000)
                    tax = 0;
            }

            tax += tax * 0.04m;

            return Math.Round(tax / 12, 2);
        }
    }
}
