using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.DTO.Salary;
using MongoDB.Driver.Linq;

namespace Codeji.CMS.Services.PayRoll
{
    public class AutoPayrollServices
    {
        private readonly IPayRollServices _payRollServices;
        private readonly IMongoDbRepository<EmpUser> _employeeRepository;
        private readonly IMongoDbRepository<EmpPayRoll> _empPayRollRepository;
        private readonly IMongoDbRepository<AttendanceModel> _attendanceRepository;
        private readonly IMongoDbRepository<SalaryModel> _salaryRepository;

        public AutoPayrollServices(
            IPayRollServices payRollServices,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<EmpPayRoll> empPayRollRepository,
            IMongoDbRepository<AttendanceModel> attendanceRepository,
            IMongoDbRepository<SalaryModel> salaryRepository)
        {
            _payRollServices = payRollServices;
            _employeeRepository = employeeRepository;
            _empPayRollRepository = empPayRollRepository;
            _attendanceRepository = attendanceRepository;
            _salaryRepository = salaryRepository;
        }

        public async Task GeneratePayrollForMonthAsync(string companyId, DateTime payMonth)
{
    var employees = await _employeeRepository.GetAll(e => e.CompanyId == companyId);

    foreach (var emp in employees)
    {
        if (string.IsNullOrEmpty(emp.DateOfJoining))
            continue;

        if (!DateTime.TryParse(emp.DateOfJoining, out DateTime joiningDate))
            continue;

        var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
        var monthEnd = new DateTime(payMonth.Year, payMonth.Month, DateTime.DaysInMonth(payMonth.Year, payMonth.Month));

        if (joiningDate > monthEnd)
            continue;

        var salaryRecords = _salaryRepository
            .Get(s => s.EmployeeId == emp.EmployeeId)
            .FirstOrDefault();

        if (salaryRecords == null)
            continue;

        int daysInMonth = DateTime.DaysInMonth(payMonth.Year, payMonth.Month);

        decimal originalBasic = salaryRecords.BasicPay;
        decimal originalHRA = salaryRecords.Hra ?? 0m;
        decimal originalLTA = salaryRecords.Lta ?? 0m;
        decimal originalOtherAllowance = salaryRecords.OtherAllowances ?? 0m;
        decimal originalBonus = salaryRecords.Bonus ?? 0m;

        DateTime today = DateTime.UtcNow.Date;

        DateTime calculationEndDate =
            (payMonth.Month == today.Month && payMonth.Year == today.Year)
            ? today
            : monthEnd;

        var attendanceRecords = await _attendanceRepository.GetAll(a =>
            a.EmployeeId == emp.EmployeeId &&
            a.Date >= monthStart &&
            a.Date <= calculationEndDate);

        int fullDayLeaves = attendanceRecords.Count(a => a.Status == AttendanceStatus.A);
        int halfDayLeaves = attendanceRecords.Count(a => a.Status == AttendanceStatus.H);
        int lateCount = attendanceRecords.Sum(a => a.LateCount);
        int earlyExitCount = attendanceRecords.Sum(a => a.EarlyExitCount);

        decimal perDaySalary = originalBasic / daysInMonth;

        int totalLateEarly = lateCount + earlyExitCount;
        decimal lateHalfDays = (totalLateEarly / 4) * 0.5m;

        decimal lossOfPayDays = fullDayLeaves + (halfDayLeaves * 0.5m) + lateHalfDays;
        decimal lossOfPay = Math.Round(lossOfPayDays * perDaySalary, 2);

        // Joining month proration
        decimal joiningProrationFactor = 1m;

        if (joiningDate.Year == payMonth.Year && joiningDate.Month == payMonth.Month)
        {
            int eligibleDays = daysInMonth - joiningDate.Day + 1;
            joiningProrationFactor = (decimal)eligibleDays / daysInMonth;
        }

        decimal basicAfterJoining = Math.Round(originalBasic * joiningProrationFactor, 2);
        decimal hra = Math.Round(originalHRA * joiningProrationFactor, 2);
        decimal lta = Math.Round(originalLTA * joiningProrationFactor, 2);
        decimal otherAllowance = Math.Round(originalOtherAllowance * joiningProrationFactor, 2);
        decimal bonus = Math.Round(originalBonus * joiningProrationFactor, 2);

        decimal finalBasic = basicAfterJoining - lossOfPay;
        if (finalBasic < 0) finalBasic = 0;

        int paidDays = daysInMonth - (int)Math.Floor(lossOfPayDays);
        if (paidDays < 0) paidDays = 0;

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
            PaidDays = paidDays,
            LossOfPayDays = lossOfPayDays,
            LossOfPay = lossOfPay,
            IncomeTax = monthlyIncomeTax,
            HealthInsurance = salaryRecords.HealthInsurance ?? 0
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
await _payRollServices.AddUpdatePayRoll(payrollDto, companyId);
}
}

        internal async Task GenerateOrUpdatePayrollForMonthAsync(string companyId, DateTime currentMonth)
{
    await GeneratePayrollForMonthAsync(companyId, currentMonth);
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