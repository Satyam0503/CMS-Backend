using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Attendance;
using MongoDB.Bson;
using MongoDB.Driver;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Codeji.CMS.Services.Employees;

namespace Codeji.CMS.Services.PayRoll;

public class PayRollServices : IPayRollServices
{
    readonly IMiddlewareService _middlewareService;
    readonly IMongoDbRepository<EmpPayRoll> _empPayRollRepository;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    readonly IMongoDbRepository<Company> _companyRepository;
    readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
    readonly PdfService _pdfService;
    readonly IAttendancePayrollReadinessService _attendanceReadiness;
    readonly IMongoDbRepository<PayrollRun>? _payrollRunsRepository;
    public PayRollServices(
        IMongoDbRepository<EmpPayRoll> empPayRollRepository,
        IMongoDbRepository<EmpUser> employeeRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<JobTitles> jobTitlesRepository,
        PdfService pdfService,
        IMiddlewareService middlewareService,
        IAttendancePayrollReadinessService attendanceReadiness,
        IMongoDbRepository<PayrollRun>? payrollRunsRepository = null
    )
    {
        _empPayRollRepository = empPayRollRepository;
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _jobTitlesRepository = jobTitlesRepository;
        _pdfService = pdfService;
        _middlewareService = middlewareService;
        _attendanceReadiness = attendanceReadiness;
        _payrollRunsRepository = payrollRunsRepository;
    }


    private static decimal RoundAmount(decimal? value) => Math.Round(value ?? 0, 2, MidpointRounding.AwayFromZero);

    // generate salary slip

    public async Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId, string companyId)
    {
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(emp => emp.UserId == userId && emp.CompanyId == companyId) ?? throw new Exception();
        return await BuildSalarySlipAsync(empUser, model.Month, model.Year);
    }

    // used when an HR/admin previews or downloads another employee's payslip (e.g. from the payroll
    // table); scoped to companyId so a caller can't reach into another company's employee records
    public async Task<(byte[] pdfBytes, string pdfName)> GenerateEmployeeSalarySlip(EmployeePayslipRequestDto model, string companyId)
    {
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.EmployeeId == model.EmployeeId && e.CompanyId == companyId) ?? throw new Exception();
        return await BuildSalarySlipAsync(empUser, model.Month, model.Year);
    }

    private async Task<(byte[] pdfBytes, string pdfName)> BuildSalarySlipAsync(EmpUser empUser, int month, int year)
    {
        try
        {
            SalarySlipTemplateModel salarySlipModel = new();
            Company? company = await _companyRepository.FirstOrDefault(c => c.CompanyId == empUser.CompanyId);
            if (company == null) throw new Exception();

            // get pay details based on month and year
            // A historical data import may have left a draft beside a processed payroll for the
            // same employee and month.  Never select an arbitrary record here: a payslip can
            // only be produced from the published (processed) payroll snapshot.
            // Compare against the canonical first-of-month value rather than .Month/.Year:
            // PayMonth is stored with DateTimeKind.Unspecified, so MongoDB's server-side
            // $month/$year operators can read a shifted calendar month; exact equality is not
            // affected because both sides undergo the same driver-side conversion.
            var monthStart = new DateTime(year, month, 1);
            Expression<Func<EmpPayRoll, bool>> expression = p => p.EmployeeId == empUser.EmployeeId && p.UserId == empUser.UserId && p.CompanyId == empUser.CompanyId && p.PayMonth == monthStart && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED");
            EmpPayRoll? payRoll = _empPayRollRepository.Get(expression)
                .OrderByDescending(p => p.ProcessedAt ?? DateTime.MinValue)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefault();
            if (payRoll == null)
                throw new InvalidOperationException("No processed payroll is available for the selected month. Process the payroll before generating a payslip.");

            // assign payroll data
            if (empUser.JobRole != null)
            {
                JobTitles? jobTitles = await _jobTitlesRepository.FirstOrDefault(jt => jt.JobTitleId == empUser.JobRole && jt.CompanyId == company.CompanyId);
                salarySlipModel.Designation = jobTitles?.Titles?.Find(t => t.Language == company.DefaultLanguage)?.Label ?? string.Empty;
            }
            salarySlipModel.EmployeeName = $"{empUser.FirstName} {empUser.LastName}";
            salarySlipModel.CompanyName = company.CompanyName;
            salarySlipModel.CompanyAddress = company.Address;
            salarySlipModel.PaySlipMonth = payRoll.PayMonth.ToString("MMM yyyy");
            salarySlipModel.EmployeeId = empUser.EmployeeId;
            salarySlipModel.PaidDate = payRoll.PaidDate.ToString("ddd, dd MMM yyyy");
            salarySlipModel.PaidDays = payRoll.PaidDays;
            salarySlipModel.EmployeeType = MapperHelper.GetEmploymentTypeLabel(empUser.EmploymentType);
            salarySlipModel.LossofPayDays = payRoll.Deduction.LossOfPayDays;
            salarySlipModel.BankAccountNo = empUser.BankAccountNumber?.ToString() ?? "NA";
            salarySlipModel.PanNumber = empUser.PanNumber ?? "NA";


            salarySlipModel.BasicSalary = payRoll.BasicPay;
            salarySlipModel.HRA = payRoll.Allowance.HRA;
            salarySlipModel.LtaAllowance = payRoll.Allowance.LTA;
            salarySlipModel.OtherAllowance = payRoll.Allowance.OtherAllowance;
            salarySlipModel.Bonus = payRoll.Bonus;

            salarySlipModel.LossOfPays = payRoll.Deduction.LossOfPay;
            salarySlipModel.IncomeTax = payRoll.Deduction.IncomeTax;
            salarySlipModel.HealthInsurance = payRoll.Deduction.HealthInsurance;
            salarySlipModel.EPF = payRoll.Deduction.EPF;
            salarySlipModel.ESIC = payRoll.Deduction.ESIC;
            salarySlipModel.EsicEmployeeLabel = PayrollPayslipEarningsHtml.GetEsicEmployeeLabel(payRoll);
            salarySlipModel.OtherDeductions = PayrollDeductionLineTotals.AdditionalAmount(payRoll.DeductionLines);
            salarySlipModel.GrossPay = payRoll.BasicPay + payRoll.Allowance.HRA + payRoll.Allowance.LTA + payRoll.Bonus + payRoll.Allowance.OtherAllowance;
            salarySlipModel.TotalDeduction = payRoll.Deduction.IncomeTax + payRoll.Deduction.HealthInsurance + payRoll.Deduction.LossOfPay + payRoll.Deduction.EPF + payRoll.Deduction.ESIC + salarySlipModel.OtherDeductions;
            salarySlipModel.NetSalary = salarySlipModel.GrossPay - salarySlipModel.TotalDeduction;
            salarySlipModel.NetSalaryInWords = IndianCurrencyWords.ToWords(salarySlipModel.NetSalary);

            string templateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "SalarySlipTemplate.html");
            string fileContent = File.ReadAllText(templateFilePath);

            if (company.CompanyLogo != null)
            {
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CompanyLogo", company.CompanyLogo);
                salarySlipModel.CompanyLogo = _middlewareService.GetCompanyLogoAsDataUrl(logoPath);
            }

            string templateContent = HtmlTemplate.Render(fileContent, salarySlipModel);
            templateContent = templateContent.Replace("{{OtherAllowanceRows}}", PayrollPayslipEarningsHtml.RenderOtherAllowanceRows(payRoll), StringComparison.Ordinal);
            templateContent = templateContent.Replace("{{OtherDeductionRows}}", PayrollPayslipEarningsHtml.RenderOtherDeductionRows(payRoll), StringComparison.Ordinal);
            // logic to generate pdfs
            byte[] pdfBytes = await _pdfService.GeneratePdfFormHtml(templateContent);
            string pdfName = $"{empUser.FirstName} {empUser.LastName}_{payRoll.PayMonth.ToString("MMM yyyy")}_Salary_slip.pdf";
            return (pdfBytes, pdfName);
        }
        catch (Exception)
        {
            throw;
        }
    }

    // fields required for every payroll row; a field missing on every single row usually means
    // the uploaded sheet was missing that column entirely, while a field missing on only some
    // rows means that row's cell was left blank
    private static readonly (string Label, Func<EmployeePayRollModel, bool> IsMissing)[] RequiredPayRollFields =
    [
        ("Basic Pay", m => m.BasicPay == null),
        ("Paid Days", m => m.PaidDays == null),
        ("Paid Date", m => m.PaidDate == null),
        ("HRA", m => m.HRA == null),
        ("Loss of Pay Days", m => m.LossOfPayDays == null),
        ("Loss of Pay", m => m.LossOfPay == null),
        ("Income Tax", m => m.IncomeTax == null),
    ];
    // optional - default to 0 when the row/column is blank or missing entirely:
    // Bonus, LTA, Other Allowance, Health Insurance, EPF, ESIC

    private static List<string> ValidatePayRollData(List<EmployeePayRollModel> payData)
    {
        List<string> errors = [];
        if (payData.Count == 0)
        {
            errors.Add("No payroll rows found in the uploaded file.");
            return errors;
        }

        List<string> missingColumns = RequiredPayRollFields
            .Where(field => payData.All(field.IsMissing))
            .Select(field => field.Label)
            .ToList();

        if (missingColumns.Count > 0)
        {
            errors.Add($"Missing required column(s): {string.Join(", ", missingColumns)}");
        }

        HashSet<string> missingColumnLabels = [.. missingColumns];
        for (int i = 0; i < payData.Count; i++)
        {
            EmployeePayRollModel row = payData[i];
            if (string.IsNullOrWhiteSpace(row.EmployeeId))
            {
                errors.Add($"Row {i + 1}: Employee Id is required.");
                continue;
            }

            List<string> rowMissingFields = RequiredPayRollFields
                .Where(field => !missingColumnLabels.Contains(field.Label) && field.IsMissing(row))
                .Select(field => field.Label)
                .ToList();

            if (rowMissingFields.Count > 0)
            {
                errors.Add($"Row {i + 1} ({row.EmployeeId}): missing {string.Join(", ", rowMissingFields)}.");
            }
            if (row.EarningLines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Description) || x.Amount < 0))
                errors.Add($"Row {i + 1} ({row.EmployeeId}): earning component lines need a code, name, and non-negative amount.");
            if (row.EarningLines.Count > 0 && PayrollCalculationRules.RoundMoney(row.EarningLines.Sum(x => x.Amount)) != PayrollCalculationRules.RoundMoney(row.OtherAllowance ?? 0m))
                errors.Add($"Row {i + 1} ({row.EmployeeId}): itemized other earnings must equal the Other Allowance total.");
            if ((row.BasicPay ?? 0) < 0 || (row.HRA ?? 0) < 0 || (row.LTA ?? 0) < 0 || (row.OtherAllowance ?? 0) < 0 ||
                (row.Bonus ?? 0) < 0 || (row.LossOfPayDays ?? 0) < 0 || (row.LossOfPay ?? 0) < 0 || (row.IncomeTax ?? 0) < 0 ||
                (row.HealthInsurance ?? 0) < 0 || (row.EPF ?? 0) < 0 || (row.ESIC ?? 0) < 0 || (row.PaidDays ?? 0) < 0)
                errors.Add($"Row {i + 1} ({row.EmployeeId}): earnings, deductions, and day values cannot be negative.");
            if (row.DeductionLines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Description) || x.Amount < 0))
                errors.Add($"Row {i + 1} ({row.EmployeeId}): deduction component lines need a code, name, and non-negative amount.");
            if (CalculateNetPay(row.BasicPay, row.HRA, row.LTA, row.OtherAllowance, row.Bonus,
                    row.LossOfPay, row.IncomeTax, row.HealthInsurance, row.EPF, row.ESIC, row.DeductionLines) < 0m)
                errors.Add($"Row {i + 1} ({row.EmployeeId}): deductions exceed gross earnings; review the amounts before uploading payroll.");
        }

        foreach (var duplicate in payData.Select((row, index) => (row, index)).Where(x => !string.IsNullOrWhiteSpace(x.row.EmployeeId))
            .GroupBy(x => x.row.EmployeeId, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            errors.Add($"Duplicate Employee Id '{duplicate.Key}' appears on rows {string.Join(", ", duplicate.Select(x => x.index + 1))}.");

        return errors;
    }

    private static decimal CalculateNetPay(decimal? basicPay, decimal? hra, decimal? lta, decimal? otherAllowance,
        decimal? bonus, decimal? lossOfPay, decimal? incomeTax, decimal? healthInsurance, decimal? epf, decimal? esic,
        IEnumerable<PayrollDeductionLineDto>? deductionLines) => PayrollCalculationRules.RoundMoney(
            (basicPay ?? 0m) + (hra ?? 0m) + (lta ?? 0m) + (otherAllowance ?? 0m) + (bonus ?? 0m) -
            (lossOfPay ?? 0m) - (incomeTax ?? 0m) - (healthInsurance ?? 0m) - (epf ?? 0m) - (esic ?? 0m) -
            PayrollDeductionLineTotals.AdditionalAmount(deductionLines));

    public async Task<Result<string>> ValidatePayrollData(PayrollValidationRequestDto model, string companyId)
    {
        var monthStart = new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1);
        var errors = ValidatePayRollData(model.PayData);
        var employeeIds = model.PayData.Select(x => x.EmployeeId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var employees = await _employeeRepository.GetAll(e => employeeIds.Contains(e.EmployeeId) && e.CompanyId == companyId);
        var existingIds = employees.Select(e => e.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < model.PayData.Count; index++)
        {
            var employeeId = model.PayData[index].EmployeeId;
            if (!string.IsNullOrWhiteSpace(employeeId) && !existingIds.Contains(employeeId))
                errors.Add($"Row {index + 1}: Employee Id {employeeId} does not exist or is invalid.");
        }
        if (await _empPayRollRepository.Exist(p => p.CompanyId == companyId && p.PayMonth == monthStart && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")))
            errors.Add("Processed payroll cannot be changed. Select another month.");
        return new Result<string>
        {
            Success = errors.Count == 0,
            StatusCode = errors.Count == 0 ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            Message = errors.Count == 0 ? "Payroll data is ready for upload." : "Payroll data failed validation.",
            MethodResults = errors
        };
    }
    public async Task<Result<string>> UploadPayrollData(EmplyeePayRollRequestDto payLoad, string companyId, string? userId = null)
    {
        Result<string> result = new();
        var monthStart = new DateTime(payLoad.PayMonth.Year, payLoad.PayMonth.Month, 1);
        if (await _empPayRollRepository.Exist(p => p.CompanyId == companyId &&
            p.PayMonth == monthStart && (p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")))
            return new Result<string> { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Processed payroll cannot be changed. Select another month." };

        List<string> validationErrors = ValidatePayRollData(payLoad.PayData);

        List<EmployeePayRollModel> payData = payLoad.PayData;
        List<string> employeeIds = [.. payData.Select(m => m.EmployeeId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct()];
        IEnumerable<EmpUser> empUsers = await _employeeRepository.GetAll(e => employeeIds.Contains(e.EmployeeId) && e.CompanyId == companyId);
        HashSet<string> existingEmployeeIds = [.. empUsers.Select(e => e.EmployeeId)];

        for (int i = 0; i < payData.Count; i++)
        {
            string employeeId = payData[i].EmployeeId;
            if (!string.IsNullOrWhiteSpace(employeeId) && !existingEmployeeIds.Contains(employeeId))
            {
                validationErrors.Add($"Row {i + 1}: Employee Id '{employeeId}' does not exist or is invalid.");
            }
        }

        if (validationErrors.Count > 0)
        {
            result.Success = false;
            result.StatusCode = StatusCodes.Status400BadRequest;
            result.Message = "Payroll data failed validation.";
            result.MethodResults = validationErrors;
            return result;
        }

        var now = DateTime.UtcNow;
        var actor = string.IsNullOrWhiteSpace(userId) ? "System" : userId;
        var dataList = (from emp in empUsers
                       join payItem in payData on emp.EmployeeId equals payItem.EmployeeId
                       select new EmpPayRoll()
                       {
                           CompanyId = companyId,
                           UserId = emp.UserId,
                           EmployeeId = emp.EmployeeId,
                           PayMonth = monthStart,
                           PaidDate = payItem.PaidDate!.Value,
                           BasicPay = RoundAmount(payItem.BasicPay),
                           Bonus = RoundAmount(payItem.Bonus),
                           PaidDays = payItem.PaidDays ?? 0,
                           CreatedAt = now,
                           CalculationSource = "EXCEL_UPLOAD",
                           WorkflowStatus = "DRAFT",
                           Allowance = new Allowance()
                           {
                               HRA = RoundAmount(payItem.HRA),
                               LTA = RoundAmount(payItem.LTA),
                               OtherAllowance = RoundAmount(payItem.OtherAllowance)
                           },
                           Deduction = new Deduction()
                           {
                               LossOfPay = RoundAmount(payItem.LossOfPay),
                               LossOfPayDays = payItem.LossOfPayDays ?? 0,
                               IncomeTax = RoundAmount(payItem.IncomeTax),
                               HealthInsurance = RoundAmount(payItem.HealthInsurance),
                               EPF = RoundAmount(payItem.EPF),
                               ESIC = RoundAmount(payItem.ESIC)
                           },
                           DeductionLines = MapDeductionLines(payItem.DeductionLines),
                           EarningLines = MapEarningLines(payItem.EarningLines),
                           AuditHistory = [
                               new PayrollAuditEvent
                               {
                                   Action = "PayrollUploaded",
                                   ActorId = actor,
                                   OccurredAtUtc = now,
                                   Notes = $"Uploaded from spreadsheet. Basic: {RoundAmount(payItem.BasicPay)}, Paid Days: {payItem.PaidDays ?? 0}, LOP Days: {payItem.LossOfPayDays ?? 0}"
                               }
                           ]
                       }).ToList();

        var tasks = new List<Task>();
        foreach (var item in dataList)
        {
            var payRoll = await _empPayRollRepository.FirstOrDefault(p => p.CompanyId == companyId && p.UserId == item.UserId && p.PayMonth == item.PayMonth);
            if (payRoll == null)
            {
                tasks.Add(_empPayRollRepository.AddOne(item));
            }
            else
            {
                Expression<Func<EmpPayRoll, bool>> expression = p => p.Id == payRoll.Id && p.CompanyId == companyId && p.EmployeeId == item.EmployeeId && p.PayMonth == item.PayMonth;
                item.Id = payRoll.Id;
                item.AuditHistory = payRoll.AuditHistory ?? [];
                item.AuditHistory.Add(new PayrollAuditEvent
                {
                    Action = "PayrollUploaded",
                    ActorId = actor,
                    OccurredAtUtc = now,
                    Notes = $"Updated from spreadsheet. Basic: {item.BasicPay}, Paid Days: {item.PaidDays}, LOP Days: {item.Deduction.LossOfPayDays}"
                });
                tasks.Add(_empPayRollRepository.Update(expression, item));
            }
        }
        await Task.WhenAll(tasks);
        result.Success = true;
        return result;
    }

    public async Task<Result<GetEmpPayRollResponseDto>> GetEmployeePayRoll(GetEmpPayRollRequestDto payload, string companyId, string? viewerUserId = null, bool processedOnly = false)
    {
        Result<GetEmpPayRollResponseDto> result = new();
        List<string> matchingEmployeeIds = [];
        List<EmpPayRoll> empPayRolls = [];
        IEnumerable<EmpUser> empUsers = [];
        var monthStart = new DateTime(payload.PayMonth.Year, payload.PayMonth.Month, 1);
        if (!string.IsNullOrEmpty(payload.EmployeeName))
        {
            matchingEmployeeIds = (await _employeeRepository.GetAll(e => (e.FirstName + " " + e.LastName).Contains(payload.EmployeeName, StringComparison.CurrentCultureIgnoreCase) && e.CompanyId == companyId && !e.IsDeleted)).Select(e => e.UserId).ToList();
            Expression<Func<EmpPayRoll, bool>> expression = p => p.CompanyId == companyId
                                        && p.PayMonth == monthStart
                                        && (!processedOnly || p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")
                                        && (viewerUserId == null || p.UserId == viewerUserId)
                                        && matchingEmployeeIds.Contains(p.UserId);
            empPayRolls = _empPayRollRepository.Get(expression).ToList();
            Expression<Func<EmpUser, bool>> empExpression = e => e.CompanyId == companyId && !e.IsDeleted &&
                (viewerUserId == null || e.UserId == viewerUserId) && matchingEmployeeIds.Contains(e.UserId);
            empUsers = await _employeeRepository.GetAll(empExpression);
        }
        else
        {
            Expression<Func<EmpPayRoll, bool>> expression = p => p.CompanyId == companyId
                                        && p.PayMonth == monthStart
                                        && (!processedOnly || p.IsProcessed || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED" || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED")
                                        && (viewerUserId == null || p.UserId == viewerUserId);
            empPayRolls = _empPayRollRepository.Get(expression).ToList();
            Expression<Func<EmpUser, bool>> empExpression = e => e.CompanyId == companyId && !e.IsDeleted &&
                (viewerUserId == null || e.UserId == viewerUserId);
            empUsers = await _employeeRepository.GetAll(empExpression);
        }

        // One employee must be represented by one payroll row per month.  Older data can
        // contain both a draft and a processed row; retain the processed snapshot in the
        // response so payroll history, paid days and payslip actions stay consistent.  For
        // duplicate drafts, use the newest deterministic record until the data is cleaned up.
        empPayRolls = empPayRolls
            .GroupBy(p => new { p.EmployeeId, p.UserId })
            .Select(group => group
                .OrderByDescending(p => p.IsProcessed || IsPayrollInProcessedState(p))
                .ThenByDescending(p => p.ProcessedAt ?? DateTime.MinValue)
                .ThenByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .First())
            .ToList();

        if (empPayRolls.Count == 0)
        {
            return result;
        }

        var existingPayrollEmployeeIds = empPayRolls.Select(p => p.EmployeeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        empUsers = empUsers.Where(emp =>
            existingPayrollEmployeeIds.Contains(emp.EmployeeId) ||
            (!processedOnly && EmploymentPeriod.IsPayrollIncludedForMonth(emp, monthStart))
        ).ToList();

        List<string> jobIds = empUsers.Select(emp => emp.JobRole).Distinct().Where(jt => jt != null).ToList();
        IEnumerable<JobTitles> jobTitles = await _jobTitlesRepository.GetAll(jt => jt.CompanyId == companyId && jobIds.Contains(jt.JobTitleId));

        var data = (from emp in empUsers
                    join empPayRoll in empPayRolls on emp.EmployeeId equals empPayRoll.EmployeeId into payRollGroup
                    from payRoll in payRollGroup.DefaultIfEmpty()
                    join jobTitle in jobTitles on emp.JobRole equals jobTitle.JobTitleId into jobTitlGroup
                    from empJobTitle in jobTitlGroup.DefaultIfEmpty()
                    select new GetEmpPayRollResponseDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = $"{emp.FirstName} {emp.LastName}",
                        JobTitle = empJobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label) ?? null,
                        PayData = new EmployeePayRollModel
                        {
                            PayRollId = payRoll?.Id ?? "",
                            EmployeeId = emp.EmployeeId,
                            PaidDate = payRoll?.PaidDate,
                            BasicPay = payRoll?.BasicPay ?? null,
                            PaidDays = payRoll?.PaidDays ?? null,
                            Bonus = payRoll?.Bonus ?? null,
                            HRA = payRoll?.Allowance.HRA ?? null,
                            LTA = payRoll?.Allowance.LTA ?? null,
                            OtherAllowance = payRoll?.Allowance.OtherAllowance ?? null,
                            EarningLines = payRoll?.EarningLines.Select(x => new PayrollEarningLineDto { Code = x.Code, Description = x.Description, Amount = x.Amount }).ToList() ?? [],
                            LossOfPayDays = payRoll?.Deduction.LossOfPayDays ?? null,
                            LossOfPay = payRoll?.Deduction.LossOfPay ?? null,
                            IncomeTax = payRoll?.Deduction.IncomeTax ?? null,
                            HealthInsurance = payRoll?.Deduction.HealthInsurance ?? null,
                            EPF = payRoll?.Deduction.EPF ?? null,
                            ESIC = payRoll?.Deduction.ESIC ?? null,
                            DeductionLines = payRoll?.DeductionLines.Select(x => new PayrollDeductionLineDto { Code = x.Code, Description = x.Description, DayFraction = x.DayFraction, Amount = x.Amount, SourceId = x.SourceId }).ToList() ?? [],
                            IsProcessed = payRoll != null && (payRoll.IsProcessed || IsPayrollInProcessedState(payRoll)),
                            ApprovalStatus = payRoll?.ApprovalStatus,
                            WorkflowStatus = payRoll == null ? "DRAFT" : GetEffectiveWorkflowStatus(payRoll),
                            PaymentStatus = payRoll?.PaymentStatus,
                            PaymentReference = payRoll?.PaymentReference,
                            ClosedAt = payRoll?.ClosedAt,
                            ClosedBy = payRoll?.ClosedBy
                        }
                    }).ToList();
        result.MethodResults = data;
        return result;
    }

    private static bool IsEffectivelyProcessed(EmpPayRoll payroll) =>
        payroll.IsProcessed || IsPayrollInProcessedState(payroll);

    private static bool IsPaymentFinal(EmpPayRoll payroll) =>
        IsStatus(payroll.PaymentStatus, "PAID", "CLOSED") || IsStatus(payroll.WorkflowStatus, "PAID", "CLOSED");

    private static FilterDefinition<EmpPayRoll> EffectiveProcessedPayrollFilter() =>
        Builders<EmpPayRoll>.Filter.Or(
            Builders<EmpPayRoll>.Filter.Eq(payroll => payroll.IsProcessed, true),
            Builders<EmpPayRoll>.Filter.Regex(payroll => payroll.WorkflowStatus, new BsonRegularExpression("^(PROCESSED|PAID|CLOSED)$", "i")),
            Builders<EmpPayRoll>.Filter.Regex(payroll => payroll.PaymentStatus, new BsonRegularExpression("^(PAID|CLOSED)$", "i")));

    private static FilterDefinition<EmpPayRoll> UnprocessedPayrollFilter() =>
        Builders<EmpPayRoll>.Filter.Not(Builders<EmpPayRoll>.Filter.Or(
            Builders<EmpPayRoll>.Filter.Eq(payroll => payroll.IsProcessed, true),
            Builders<EmpPayRoll>.Filter.Regex(payroll => payroll.WorkflowStatus, new BsonRegularExpression("^(PROCESSED|PAID|CLOSED)$", "i")),
            Builders<EmpPayRoll>.Filter.Regex(payroll => payroll.PaymentStatus, new BsonRegularExpression("^(PAID|CLOSED)$", "i"))));
    private static bool IsPayrollInProcessedState(EmpPayRoll payroll) =>
        IsStatus(payroll.WorkflowStatus, "PROCESSED", "PAID", "CLOSED") ||
        IsStatus(payroll.PaymentStatus, "PAID", "CLOSED");

    private static string GetEffectiveWorkflowStatus(EmpPayRoll payroll)
    {
        if (IsStatus(payroll.PaymentStatus, "CLOSED") || IsStatus(payroll.WorkflowStatus, "CLOSED")) return "CLOSED";
        if (IsStatus(payroll.PaymentStatus, "PAID") || IsStatus(payroll.WorkflowStatus, "PAID")) return "PAID";
        if (payroll.IsProcessed || IsStatus(payroll.WorkflowStatus, "PROCESSED")) return "PROCESSED";
        if (IsStatus(payroll.ApprovalStatus, "APPROVED") || IsStatus(payroll.WorkflowStatus, "APPROVED")) return "APPROVED";
        return "DRAFT";
    }

    private static bool IsStatus(string? actual, params string[] expected) =>
        expected.Any(value => string.Equals(actual, value, StringComparison.OrdinalIgnoreCase));
    public async Task<Result> AddUpdatePayRoll(AddUpdatePayRollRequestDto model, string companyId)
    {
        Result result = new();
        // EmpPayRoll has a unique (company, employee, month) key. Canonicalising here
        // makes the manual, upload and automatic paths agree on the same month value.
        model.PayMonth = new DateTime(model.PayMonth.Year, model.PayMonth.Month, 1);
        var monetaryValues = new[] { model.BasicPay, model.Bonus ?? 0, model.HRA ?? 0, model.LTA ?? 0, model.OtherAllowance ?? 0,
            model.LossOfPay ?? 0, model.IncomeTax ?? 0, model.HealthInsurance ?? 0, model.EPF ?? 0, model.ESIC ?? 0 };
        if (monetaryValues.Any(x => x < 0) || model.PaidDays < 0 || (model.LossOfPayDays ?? 0) < 0 ||
            model.EarningLines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Description) || x.Amount < 0) ||
            (model.EarningLines.Count > 0 && PayrollCalculationRules.RoundMoney(model.EarningLines.Sum(x => x.Amount)) != PayrollCalculationRules.RoundMoney(model.OtherAllowance ?? 0m)))
            return new Result { Success=false, StatusCode=StatusCodes.Status400BadRequest, Message="Earnings, deductions, and day values cannot be negative." };
        if (model.DeductionLines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Description) || x.Amount < 0))
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Deduction component lines need a code, name, and non-negative amount." };
        var calculatedNetPay = CalculateNetPay(model.BasicPay, model.HRA, model.LTA, model.OtherAllowance, model.Bonus,
            model.LossOfPay, model.IncomeTax, model.HealthInsurance, model.EPF, model.ESIC, model.DeductionLines);
        if (calculatedNetPay < 0m)
            return new Result { Success = false, StatusCode = StatusCodes.Status422UnprocessableEntity, ErrorCode = "PAYROLL_NET_NEGATIVE", Message = "Deductions exceed gross earnings. Review the payroll inputs; this employee's payroll was not saved." };
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.EmployeeId == model.EmployeeId && e.CompanyId == companyId);
        if (empUser == null) return result;
        if (await _empPayRollRepository.Exist(p => p.CompanyId == companyId &&
            p.EmployeeId == model.EmployeeId &&
            p.PayMonth == model.PayMonth &&
            (p.IsProcessed || p.PaymentStatus == "PAID" || p.PaymentStatus == "CLOSED" || p.WorkflowStatus == "PROCESSED" || p.WorkflowStatus == "PAID" || p.WorkflowStatus == "CLOSED")))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "This employee processed payroll cannot be changed." };
        // The (CompanyId, EmployeeId, PayMonth) unique index is the real source of truth for
        // "does this employee already have a payroll row for this month" - UserId is not part
        // of that index and can drift (re-import, offboarding/reactivation), so it must never
        // be part of the lookup used to find an existing row or recover from a conflict.
        Expression<Func<EmpPayRoll, bool>> expression = p => p.EmployeeId == model.EmployeeId && p.CompanyId == companyId && p.PayMonth == model.PayMonth;
        EmpPayRoll? payRoll = null;
        if (!string.IsNullOrEmpty(model.PayRollId))
        {
            payRoll = await _empPayRollRepository.FirstOrDefault(p => p.Id == model.PayRollId && p.CompanyId == companyId && p.EmployeeId == empUser.EmployeeId && p.PayMonth == model.PayMonth);
        }
        if (payRoll == null)
        {
            // check if payroll for the month already exist. model.PayRollId was not supplied
            // (or didn't resolve above), so any row found here is an unexpected pre-existing
            // draft rather than the one the caller intended to update - block instead of
            // silently overwriting it.
            var existingForMonth = await _empPayRollRepository.FirstOrDefault(expression);
            if (existingForMonth != null)
            {
                result.StatusCode = CustomStatusCode.PayRollAlreadyForMonth;
                result.Message = "Payroll for the month already exist";
                return result;
            }
        }
        if (payRoll == null)
        {
            // add new entry
            EmpPayRoll newPayRoll = new()
            {
                CompanyId = companyId,
                UserId = empUser.UserId,
                EmployeeId = empUser.EmployeeId,
                PayMonth = model.PayMonth,
                PaidDate = model.PaidDate,
                BasicPay = model.BasicPay,
                Bonus = model.Bonus ?? 0,
                PaidDays = model.PaidDays,
                CreatedAt = DateTime.UtcNow,
                CalculationSource = model.CalculationSnapshot != null ? "AUTO_CALCULATED" : "MANUAL_ENTRY",
                Allowance = new Allowance()
                {
                    HRA = model.HRA ?? 0,
                    LTA = model.LTA ?? 0,
                    OtherAllowance = model.OtherAllowance ?? 0
                },
                Deduction = new Deduction()
                {
                    LossOfPayDays = (float)(model.LossOfPayDays ?? 0m),
                    LossOfPay = model.LossOfPay ?? 0,
                    IncomeTax = model.IncomeTax ?? 0,
                    HealthInsurance = model.HealthInsurance ?? 0,
                    EPF = model.EPF ?? 0,
                    ESIC = model.ESIC ?? 0
                },
                DeductionLines = MapDeductionLines(model.DeductionLines),
                EarningLines = MapEarningLines(model.EarningLines),
                CalculationSnapshot = MapSnapshot(model.CalculationSnapshot)
            };
            try
            {
                result = await _empPayRollRepository.AddOne(newPayRoll);
            }
            catch (MongoBulkWriteException<EmpPayRoll> exception) when (exception.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey))
            {
                // Two generation requests can reach this point together. The unique
                // index has already protected the data; reload the winning draft and
                // update it instead of surfacing a raw database exception to HR.
                var winningPayRoll = await _empPayRollRepository.FirstOrDefault(expression);
                if (winningPayRoll is null)
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll is being generated for this employee. Refresh and try again." };

                model.PayRollId = winningPayRoll.Id;
                return await AddUpdatePayRoll(model, companyId);
            }

            // MongoRepository.AddOne never rethrows a duplicate-key failure on a non-_id
            // index (e.g. this collection's company/employee/month index) - it swallows the
            // driver exception and returns a failed Result instead. Detect that case here so
            // the same concurrent-generation race is recovered from instead of surfacing the
            // raw database exception to HR.
            if (!result.Success && IsPayrollUniqueKeyConflict(result.Message))
            {
                var winningPayRoll = await _empPayRollRepository.FirstOrDefault(expression);
                if (winningPayRoll is null)
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll is being generated for this employee. Refresh and try again." };

                model.PayRollId = winningPayRoll.Id;
                return await AddUpdatePayRoll(model, companyId);
            }
        }
        else
        {
            // update existing
            Expression<Func<EmpPayRoll, bool>> updateExpression = p => p.Id == payRoll.Id && p.CompanyId == companyId && p.EmployeeId == empUser.EmployeeId && p.PayMonth == model.PayMonth;
            var otherAllowanceChanged = PayrollCalculationRules.RoundMoney(payRoll.Allowance.OtherAllowance) != PayrollCalculationRules.RoundMoney(model.OtherAllowance ?? 0m);
            payRoll.UserId = empUser.UserId; // self-heal a stale UserId link found via the unique key
            payRoll.BasicPay = model.BasicPay;
            payRoll.Bonus = model.Bonus ?? 0;
            payRoll.PaidDays = model.PaidDays;
            payRoll.PayMonth = model.PayMonth;
            payRoll.PaidDate = model.PaidDate;
            if (model.CalculationSnapshot != null)
                payRoll.CalculationSource = "AUTO_CALCULATED";
            else if (payRoll.CalculationSource != "EXCEL_UPLOAD")
                payRoll.CalculationSource = "MANUAL_ENTRY";
            payRoll.Allowance.HRA = model.HRA ?? 0;
            payRoll.Allowance.LTA = model.LTA ?? 0;
            payRoll.Allowance.OtherAllowance = model.OtherAllowance ?? 0;
            payRoll.Deduction.LossOfPayDays = (float)(model.LossOfPayDays ?? 0);
            payRoll.Deduction.LossOfPay = model.LossOfPay ?? 0;
            payRoll.Deduction.IncomeTax = model.IncomeTax ?? 0;
            payRoll.Deduction.HealthInsurance = model.HealthInsurance ?? 0;
            payRoll.Deduction.EPF = model.EPF ?? 0;
            payRoll.Deduction.ESIC = model.ESIC ?? 0;
            if (model.ReplaceDeductionLines || model.DeductionLines.Count > 0)
                payRoll.DeductionLines = MapDeductionLines(model.DeductionLines);
            if (model.ReplaceEarningLines || model.EarningLines.Count > 0)
                payRoll.EarningLines = MapEarningLines(model.EarningLines);
            else if (otherAllowanceChanged)
                payRoll.EarningLines = [];
            payRoll.CalculationSnapshot = MapSnapshot(model.CalculationSnapshot);
            result = await _empPayRollRepository.Update(updateExpression, payRoll);
        }
        return result;
    }

    public async Task<Result<List<PayrollAuditEventDto>>> GetPayrollAuditHistory(string payrollId, string companyId)
    {
        if (string.IsNullOrWhiteSpace(payrollId))
            return new Result<List<PayrollAuditEventDto>> { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Payroll id is required." };

        var payroll = await _empPayRollRepository.FirstOrDefault(p => p.Id == payrollId.Trim() && p.CompanyId == companyId);
        if (payroll is null)
            return new Result<List<PayrollAuditEventDto>> { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Payroll record was not found." };

        return new Result<List<PayrollAuditEventDto>>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            MethodResult = payroll.AuditHistory
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(x => new PayrollAuditEventDto { Action = x.Action, ActorId = x.ActorId, OccurredAtUtc = x.OccurredAtUtc, Notes = x.Notes })
                .ToList()
        };
    }
    public async Task<Result> ClosePayrollMonth(DateTime payMonth, string companyId, string closedBy)
    {
        var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
        var rows = _empPayRollRepository.Get(p => p.CompanyId == companyId && p.PayMonth == monthStart).ToList();
        if (rows.Count == 0)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "No generated payroll was found for this month." };
        if (rows.Any(p => !IsEffectivelyProcessed(p)))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "All payroll rows must be processed before closing the month." };
        if (rows.Any(p => !string.Equals(p.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase) && !string.Equals(p.PaymentStatus, "CLOSED", StringComparison.OrdinalIgnoreCase)))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "All payroll payments must be recorded before closing the month." };
        if (rows.All(p => string.Equals(p.PaymentStatus, "CLOSED", StringComparison.OrdinalIgnoreCase)))
            return new Result { Success = true, Message = $"Payroll for {monthStart:MMMM yyyy} is already closed." };

        var filter = Builders<EmpPayRoll>.Filter.Eq(p => p.CompanyId, companyId)
            & Builders<EmpPayRoll>.Filter.Eq(p => p.PayMonth, monthStart)
            & EffectiveProcessedPayrollFilter()
            & Builders<EmpPayRoll>.Filter.Or(
                Builders<EmpPayRoll>.Filter.Regex(p => p.PaymentStatus, new BsonRegularExpression("^paid$", "i")),
                Builders<EmpPayRoll>.Filter.Regex(p => p.PaymentStatus, new BsonRegularExpression("^closed$", "i")));
        var closedAt = DateTime.UtcNow;
        var update = Builders<EmpPayRoll>.Update.Set(p => p.IsProcessed, true).Set(p => p.PaymentStatus, "CLOSED").Set(p => p.WorkflowStatus, "CLOSED").Set(p => p.ClosedAt, closedAt).Set(p => p.ClosedBy, closedBy).Push(p => p.AuditHistory, new PayrollAuditEvent { Action = "PayrollClosed", ActorId = closedBy, OccurredAtUtc = closedAt });
        var result = await _empPayRollRepository.UpdateMany(filter, update);
        var currentRows = _empPayRollRepository.Get(p => p.CompanyId == companyId && p.PayMonth == monthStart).ToList();
        if (currentRows.Count == 0 || currentRows.Any(p => !string.Equals(p.PaymentStatus, "CLOSED", StringComparison.OrdinalIgnoreCase) || !string.Equals(p.WorkflowStatus, "CLOSED", StringComparison.OrdinalIgnoreCase)))
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll changed while it was being closed. Refresh and try again." };
        }
        result.Message = result.Success ? $"Payroll for {monthStart:MMMM yyyy} has been closed by {closedBy}." : "Unable to close payroll month.";
        return result;
    }
    public async Task<Result> RecordPayrollPayment(RecordPayrollPaymentRequestDto model, string companyId, string recordedBy)
    {
        if (string.IsNullOrWhiteSpace(model.PayrollId) || string.IsNullOrWhiteSpace(model.PaymentReference))
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Payment reference is required." };

        var paymentDate = model.PaymentDate == default ? DateTime.UtcNow : model.PaymentDate;
        var payroll = await _empPayRollRepository.FirstOrDefault(p => p.Id == model.PayrollId && p.CompanyId == companyId);
        if (payroll is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Payroll record was not found." };
        if (!IsEffectivelyProcessed(payroll))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll must be processed before payment is recorded." };
        var grossPay = payroll.BasicPay + (payroll.Allowance?.HRA ?? 0m) + (payroll.Allowance?.LTA ?? 0m) + (payroll.Allowance?.OtherAllowance ?? 0m) + payroll.Bonus;
        var totalDeductions = (payroll.Deduction?.LossOfPay ?? 0m) + (payroll.Deduction?.IncomeTax ?? 0m) + (payroll.Deduction?.HealthInsurance ?? 0m) + (payroll.Deduction?.EPF ?? 0m) + (payroll.Deduction?.ESIC ?? 0m) + PayrollDeductionLineTotals.AdditionalAmount(payroll.DeductionLines);
        if (grossPay - totalDeductions <= 0m)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll has no positive net payable amount and cannot be recorded as a payment." };
        if (IsPaymentFinal(payroll))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payment has already been recorded for this payroll." };

        var paymentFilter = Builders<EmpPayRoll>.Filter.Eq(p => p.Id, payroll.Id)
            & Builders<EmpPayRoll>.Filter.Eq(p => p.CompanyId, companyId)
            & EffectiveProcessedPayrollFilter()
            & Builders<EmpPayRoll>.Filter.Not(Builders<EmpPayRoll>.Filter.Or(
                Builders<EmpPayRoll>.Filter.Regex(p => p.PaymentStatus, new BsonRegularExpression("^(PAID|CLOSED)$", "i")),
                Builders<EmpPayRoll>.Filter.Regex(p => p.WorkflowStatus, new BsonRegularExpression("^(PAID|CLOSED)$", "i"))));
        var paymentUpdate = Builders<EmpPayRoll>.Update
            .Set(p => p.IsProcessed, true)
            .Set(p => p.PaymentStatus, "PAID")
            .Set(p => p.WorkflowStatus, "PAID")
            .Set(p => p.PaymentMethod, string.IsNullOrWhiteSpace(model.PaymentMethod) ? "MANUAL" : model.PaymentMethod.Trim().ToUpperInvariant())
            .Set(p => p.PaymentReference, model.PaymentReference.Trim())
            .Set(p => p.PaymentDate, paymentDate)
            .Set(p => p.PaymentRecordedBy, recordedBy)
            .Set(p => p.PaymentNotes, model.Notes?.Trim())
            .Push(p => p.AuditHistory, new PayrollAuditEvent { Action = "PaymentRecorded", ActorId = recordedBy, OccurredAtUtc = DateTime.UtcNow, Notes = model.PaymentReference.Trim() });
        var paymentResult = await _empPayRollRepository.UpdateMany(paymentFilter, paymentUpdate);
        if (!paymentResult.Success)
        {
            paymentResult.StatusCode = StatusCodes.Status409Conflict;
            paymentResult.Message = "Payment was already recorded or the payroll changed while it was being paid.";
        }
        return paymentResult;
    }
    public async Task<Result> ProcessPayrollMonth(
        DateTime payMonth,
        string companyId,
        string processedBy,
        IReadOnlyCollection<string>? employeeIds = null)
    {
        var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
        var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        if (monthStart >= currentMonthStart)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll can only be processed after the payroll month has ended." };
        // Publishing an incomplete selection creates mixed payroll state for one calendar
        // month. Drafts may be reviewed individually, but month-end processing is always a
        // company-wide operation, including employees whose employment ended during the month.
        if (employeeIds is not null)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Payroll must be processed for all generated employees in the month." };
        var payrollRows = _empPayRollRepository.Get(p => p.CompanyId == companyId &&
            p.PayMonth == monthStart).ToList();

        if (payrollRows.Count == 0)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "No generated payroll was found for this month." };

        if (payrollRows.All(IsEffectivelyProcessed))
            return new Result { Success = true, Message = $"Payroll for {monthStart:MMMM yyyy} is already processed." };

        if (payrollRows.Any(p => !IsEffectivelyProcessed(p) && string.Equals(p.ApprovalStatus, "PENDING", StringComparison.OrdinalIgnoreCase)))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll must be approved before it can be processed." };

        var readiness = await _attendanceReadiness.EvaluateAsync(companyId, monthStart);
        if (!readiness.IsReady)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll is blocked until attendance corrections are reviewed: " + string.Join("; ", readiness.BlockingReasons) };

        var filter = Builders<EmpPayRoll>.Filter.Eq(p => p.CompanyId, companyId) &
                     Builders<EmpPayRoll>.Filter.Gte(p => p.PayMonth, monthStart) &
                     Builders<EmpPayRoll>.Filter.Lt(p => p.PayMonth, monthStart.AddMonths(1)) &
                     UnprocessedPayrollFilter();

        var pendingCount = payrollRows.Count(p => !IsEffectivelyProcessed(p));
        var update = Builders<EmpPayRoll>.Update
            .Set(p => p.IsProcessed, true)
            .Set(p => p.WorkflowStatus, "PROCESSED")
            .Set(p => p.ProcessedAt, DateTime.UtcNow)
            .Set(p => p.ProcessedBy, processedBy)
            .Push(p => p.AuditHistory, new PayrollAuditEvent { Action = "PayrollProcessed", ActorId = processedBy, OccurredAtUtc = DateTime.UtcNow });

        var result = await _empPayRollRepository.UpdateMany(filter, update);
        // UpdateMany succeeds after any row changes, so confirm the whole tenant/month
        // reached the processed state before reporting the month as complete.
        var currentRows = _empPayRollRepository.Get(p => p.CompanyId == companyId && p.PayMonth == monthStart).ToList();
        if (currentRows.Count == 0 || currentRows.Any(p => !IsEffectivelyProcessed(p)))
        {
            if (result.Success || result.StatusCode < StatusCodes.Status500InternalServerError)
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll changed while it was being processed. Refresh and try again." };
            return result;
        }

        if (!result.Success)
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"Payroll for {monthStart:MMMM yyyy} is already processed." };

        if (_payrollRunsRepository is not null)
        {
            try
            {
                var matchingRun = await _payrollRunsRepository.FirstOrDefault(r =>
                    r.CompanyId == companyId &&
                    r.PeriodStart == monthStart &&
                    r.RunType == "REGULAR" &&
                    r.IsActive &&
                    r.Status != "CANCELLED" &&
                    r.Status != "PROCESSED" &&
                    r.Status != "PAID" &&
                    r.Status != "CLOSED");
                if (matchingRun is not null)
                {
                    var previousStatus = matchingRun.Status;
                    matchingRun.Status = "PROCESSED";
                    matchingRun.ProcessedAtUtc = DateTime.UtcNow;
                    matchingRun.Version++;
                    matchingRun.AuditHistory.Add(new PayrollRunAuditEvent
                    {
                        Action = "RunProcessedViaRegister",
                        ActorId = processedBy,
                        OccurredAtUtc = DateTime.UtcNow,
                        Notes = "Synchronized from Monthly Register processing."
                    });
                    await _payrollRunsRepository.Update(
                        Builders<PayrollRun>.Filter.Eq(r => r.Id, matchingRun.Id) &
                        Builders<PayrollRun>.Filter.Eq(r => r.CompanyId, companyId) &
                        Builders<PayrollRun>.Filter.Eq(r => r.Status, previousStatus),
                        matchingRun);
                }
            }
            catch
            {
                // Non-fatal synchronization
            }
        }

        result.Message = $"Payroll for {pendingCount} employee{(pendingCount == 1 ? "" : "s")} in {monthStart:MMMM yyyy} has been processed.";
        return result;
    }

    // Recognises the swallowed duplicate-key failure message produced by MongoRepository.AddOne
    // for the (CompanyId, EmployeeId, PayMonth) unique index specifically, so a real insert
    // failure for any other reason is never mistaken for a recoverable generation race.
    private static bool IsPayrollUniqueKeyConflict(string? message) =>
        !string.IsNullOrEmpty(message)
        && message.Contains("E11000", StringComparison.Ordinal)
        && message.Contains("ux_payroll_company_employee_month", StringComparison.Ordinal);

    private static List<PayrollDeductionLine> MapDeductionLines(IEnumerable<PayrollDeductionLineDto> lines) => lines.Select(x => new PayrollDeductionLine
    {
        Code = x.Code, Description = x.Description, DayFraction = x.DayFraction, Amount = x.Amount, SourceId = x.SourceId
    }).ToList();

    private static List<PayrollEarningLine> MapEarningLines(IEnumerable<PayrollEarningLineDto> lines) => lines.Select(x => new PayrollEarningLine
    {
        Code = x.Code.Trim().ToUpperInvariant(), Description = x.Description.Trim(), Amount = RoundAmount(x.Amount)
    }).ToList();

    private static PayrollCalculationSnapshot? MapSnapshot(PayrollCalculationSnapshotDto? x) => x == null ? null : new PayrollCalculationSnapshot
    {
        PayrollMonth=x.PayrollMonth, JoiningDateUsed=x.JoiningDateUsed, ExitDateUsed=x.ExitDateUsed,
        EligibleFrom=x.EligibleFrom, EligibleTo=x.EligibleTo, DivisorPolicy=x.DivisorPolicy, Divisor=x.Divisor, DivisorPolicyVersion=x.DivisorPolicyVersion,
        LhdCount=x.LhdCount, EdCount=x.EdCount, CombinedCount=x.CombinedCount, AllowedCount=x.AllowedCount,
        ExceededCount=x.ExceededCount, ExceptionDecision=x.ExceptionDecision, PenaltyDayFraction=x.PenaltyDayFraction, PayrollAssignmentId=x.PayrollAssignmentId, PayGroupId=x.PayGroupId, SalaryTemplateId=x.SalaryTemplateId, ResolvedPolicyProfileId=x.ResolvedPolicyProfileId, ResolvedPolicyVersion=x.ResolvedPolicyVersion, EpfMode=x.EpfMode, EpfWageCeiling=x.EpfWageCeiling, EpfEmployeeRatePercent=x.EpfEmployeeRatePercent, EpfEmployerRatePercent=x.EpfEmployerRatePercent, EsicMode=x.EsicMode, EsicWageCeiling=x.EsicWageCeiling, EsicEmployeeRatePercent=x.EsicEmployeeRatePercent, EsicEmployerRatePercent=x.EsicEmployerRatePercent, TdsMode=x.TdsMode,
        PenaltyAmount=x.PenaltyAmount, PolicyId=x.PolicyId, PolicyVersion=x.PolicyVersion,
        AttendanceSummaryVersion=x.AttendanceSummaryVersion,
        PayrollAssignmentVersion=x.PayrollAssignmentVersion, PayGroupVersion=x.PayGroupVersion, SalaryTemplateVersion=x.SalaryTemplateVersion,
        SalaryRevisions=x.SalaryRevisions.Select(s => new PayrollSalaryRevisionReference { SalaryStructureId=s.SalaryStructureId, Version=s.Version }).ToList(),
        SalaryStructures=x.SalaryStructures.Select(s => new PayrollSalaryStructureSnapshot
        {
            SalaryStructureId=s.SalaryStructureId, Version=s.Version, PayGroupId=s.PayGroupId,
            EffectiveFrom=s.EffectiveFrom, EffectiveTo=s.EffectiveTo,
            Components=s.Components.Select(c => new PayrollComponentSnapshot
            {
                Code=c.Code, Name=c.Name, Kind=c.Kind, CalculationType=c.CalculationType,
                BaseComponentCode=c.BaseComponentCode, Value=c.Value, IsTaxable=c.IsTaxable,
                IsRecurring=c.IsRecurring, IsStatutory=c.IsStatutory,
                IsEmployerContribution=c.IsEmployerContribution, SortOrder=c.SortOrder
            }).ToList()
        }).ToList()
    };
    public async Task<Result> ApprovePayrollMonth(DateTime payMonth, string companyId, string approvedBy)
    {
        var monthStart = new DateTime(payMonth.Year, payMonth.Month, 1);
        var rows = _empPayRollRepository.Get(x => x.CompanyId == companyId && x.PayMonth == monthStart).ToList();
        if (rows.Count == 0) return new Result { Success = false, StatusCode = 400, Message = "No generated payroll was found for this month." };
        if (rows.Any(IsEffectivelyProcessed)) return new Result { Success = false, StatusCode = 409, Message = "Processed or paid payroll cannot be approved again." };
        if (rows.All(x => string.Equals(x.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))) return new Result { Success = true, Message = "Payroll month is already approved." };
        var filter = Builders<EmpPayRoll>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmpPayRoll>.Filter.Eq(x => x.PayMonth, monthStart) & UnprocessedPayrollFilter() & Builders<EmpPayRoll>.Filter.Ne(x => x.ApprovalStatus, "APPROVED");
        var now = DateTime.UtcNow;
        var update = Builders<EmpPayRoll>.Update.Set(x => x.ApprovalStatus, "APPROVED").Set(x => x.WorkflowStatus, "APPROVED").Set(x => x.ApprovedBy, approvedBy).Set(x => x.ApprovedAtUtc, now).Push(x => x.AuditHistory, new PayrollAuditEvent { Action = "PayrollApproved", ActorId = approvedBy, OccurredAtUtc = now });
        var result = await _empPayRollRepository.UpdateMany(filter, update);
        var currentRows = _empPayRollRepository.Get(x => x.CompanyId == companyId && x.PayMonth == monthStart).ToList();
        if (currentRows.Count == 0 || currentRows.Any(x => !string.Equals(x.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) || IsEffectivelyProcessed(x)))
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "Payroll changed while it was being approved. Refresh and try again." };
        }
        result.Message = result.Success ? $"Payroll for {monthStart:MMMM yyyy} has been approved." : "Unable to approve payroll month.";
        return result;
    }
}
