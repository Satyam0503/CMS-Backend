using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.PayRoll;

public class PayRollServices : IPayRollServices
{
    readonly IMiddlewareService _middlewareService;
    readonly IMongoDbRepository<EmpPayRoll> _empPayRollRepository;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    readonly IMongoDbRepository<Company> _companyRepository;
    readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
    readonly PdfService _pdfService;
    public PayRollServices(
        IMongoDbRepository<EmpPayRoll> empPayRollRepository,
        IMongoDbRepository<EmpUser> employeeRepository,
        IMongoDbRepository<Company> companyRepository,
        IMongoDbRepository<JobTitles> jobTitlesRepository,
        PdfService pdfService,
        IMiddlewareService middlewareService
    )
    {
        _empPayRollRepository = empPayRollRepository;
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _jobTitlesRepository = jobTitlesRepository;
        _pdfService = pdfService;
        _middlewareService = middlewareService;
    }


    private static decimal RoundAmount(decimal? value) => Math.Round(value ?? 0, 2, MidpointRounding.AwayFromZero);

    // generate salary slip

    public async Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId)
    {
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(emp => emp.UserId == userId) ?? throw new Exception();
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
            Expression<Func<EmpPayRoll, bool>> expression = p => p.EmployeeId == empUser.EmployeeId && p.UserId == empUser.UserId && p.CompanyId == empUser.CompanyId && p.PayMonth.Month == month && p.PayMonth.Year == year;
            EmpPayRoll? payRoll = await _empPayRollRepository.FirstOrDefault(expression) ?? throw new Exception(CustomStatusCode.PayRollNotExist.ToString());

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
            salarySlipModel.GrossPay = payRoll.BasicPay + payRoll.Allowance.HRA + payRoll.Allowance.LTA + payRoll.Bonus + payRoll.Allowance.OtherAllowance;
            salarySlipModel.TotalDeduction = payRoll.Deduction.IncomeTax + payRoll.Deduction.HealthInsurance + payRoll.Deduction.LossOfPay + payRoll.Deduction.EPF + payRoll.Deduction.ESIC;
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
        }

        return errors;
    }

    public async Task<Result<string>> UploadPayrollData(EmplyeePayRollRequestDto payLoad, string companyId)
    {
        Result<string> result = new();
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

        var dataList = from emp in empUsers
                       join payItem in payData on emp.EmployeeId equals payItem.EmployeeId
                       select new EmpPayRoll()
                       {
                           CompanyId = companyId,
                           UserId = emp.UserId,
                           EmployeeId = emp.EmployeeId,
                           PayMonth = payLoad.PayMonth,
                           PaidDate = payItem.PaidDate!.Value,
                           BasicPay = RoundAmount(payItem.BasicPay),
                           Bonus = RoundAmount(payItem.Bonus),
                           PaidDays = payItem.PaidDays ?? 0,
                           CreatedAt = DateTime.UtcNow,
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
                           }
                       };

        var tasks = new List<Task>();
        foreach (var item in dataList)
        {
            var payRoll = await _empPayRollRepository.FirstOrDefault(p => p.EmployeeId == item.EmployeeId && item.PayMonth.Month == p.PayMonth.Month && item.PayMonth.Year == p.PayMonth.Year);
            if (payRoll == null)
            {
                tasks.Add(_empPayRollRepository.AddOne(item));
            }
            else
            {
                Expression<Func<EmpPayRoll, bool>> expression = p => p.Id == payRoll.Id;
                item.Id = payRoll.Id;
                tasks.Add(_empPayRollRepository.Update(expression, item));
            }
        }
        await Task.WhenAll(tasks);
        result.Success = true;
        return result;
    }

    public async Task<Result<GetEmpPayRollResponseDto>> GetEmployeePayRoll(GetEmpPayRollRequestDto payload, string companyId)
    {
        Result<GetEmpPayRollResponseDto> result = new();
        List<string> matchingEmployeeIds = [];
        List<EmpPayRoll> empPayRolls = [];
        IEnumerable<EmpUser> empUsers = [];
        if (!string.IsNullOrEmpty(payload.EmployeeName))
        {
            matchingEmployeeIds = (await _employeeRepository.GetAll(e => (e.FirstName + " " + e.LastName).Contains(payload.EmployeeName, StringComparison.CurrentCultureIgnoreCase) && e.CompanyId == companyId)).Select(e => e.UserId).ToList();
            Expression<Func<EmpPayRoll, bool>> expression = p => p.CompanyId == companyId
                                        && payload.PayMonth.Month == p.PayMonth.Month && payload.PayMonth.Year == p.PayMonth.Year
                                        && matchingEmployeeIds.Contains(p.UserId);
            empPayRolls = _empPayRollRepository.Get(expression).ToList();
            Expression<Func<EmpUser, bool>> empExpression = e => e.CompanyId == companyId && matchingEmployeeIds.Contains(e.UserId);
            empUsers = await _employeeRepository.GetAll(empExpression);
        }
        else
        {
            Expression<Func<EmpPayRoll, bool>> expression = p => p.CompanyId == companyId
                                        && payload.PayMonth.Month == p.PayMonth.Month && payload.PayMonth.Year == p.PayMonth.Year;
            empPayRolls = _empPayRollRepository.Get(expression).ToList();
            Expression<Func<EmpUser, bool>> empExpression = e => e.CompanyId == companyId;
            empUsers = await _employeeRepository.GetAll(empExpression);
        }

        if (empPayRolls.Count == 0)
        {
            return result;
        }
        List<string> jobIds = empUsers.Select(emp => emp.JobRole).Distinct().Where(jt => jt != null).ToList();
        IEnumerable<JobTitles> jobTitles = await _jobTitlesRepository.GetAll(jt => jobIds.Contains(jt.JobTitleId));

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
                            LossOfPayDays = payRoll?.Deduction.LossOfPayDays ?? null,
                            LossOfPay = payRoll?.Deduction.LossOfPay ?? null,
                            IncomeTax = payRoll?.Deduction.IncomeTax ?? null,
                            HealthInsurance = payRoll?.Deduction.HealthInsurance ?? null,
                            EPF = payRoll?.Deduction.EPF ?? null,
                            ESIC = payRoll?.Deduction.ESIC ?? null
                        }
                    }).ToList();
        result.MethodResults = data;
        return result;
    }

    public async Task<Result> AddUpdatePayRoll(AddUpdatePayRollRequestDto model, string companyId)
    {
        Result result = new();
        EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.EmployeeId == model.EmployeeId && e.CompanyId == companyId);
        if (empUser == null) return result;
        EmpPayRoll? payRoll = null;
        if (!string.IsNullOrEmpty(model.PayRollId))
        {
            payRoll = await _empPayRollRepository.FirstOrDefault(p => p.Id == model.PayRollId && p.CompanyId == companyId);
        }
        if (payRoll == null)
        {
            // check if payroll for the month already exist
            Expression<Func<EmpPayRoll, bool>> expression = p => p.EmployeeId == model.EmployeeId && p.UserId == empUser.UserId && p.CompanyId == companyId && p.PayMonth.Month == model.PayMonth.Month && p.PayMonth.Year == model.PayMonth.Year;
            bool exist = await _empPayRollRepository.Exist(expression);
            if (exist)
            {
                result.StatusCode = CustomStatusCode.PayRollAlreadyForMonth;
                result.Message = "Payroll for the month already exist";
                return result;
            }
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
                }
            };
            result = await _empPayRollRepository.AddOne(newPayRoll);
        }
        else
        {
            // update existing
            Expression<Func<EmpPayRoll, bool>> expression = p => p.Id == payRoll.Id;
            payRoll.BasicPay = model.BasicPay;
            payRoll.Bonus = model.Bonus ?? 0;
            payRoll.PaidDays = model.PaidDays;
            payRoll.PayMonth = model.PayMonth;
            payRoll.PaidDate = model.PaidDate;
            payRoll.Allowance.HRA = model.HRA ?? 0;
            payRoll.Allowance.LTA = model.LTA ?? 0;
            payRoll.Allowance.OtherAllowance = model.OtherAllowance ?? 0;
            payRoll.Deduction.LossOfPayDays = (float)(model.LossOfPayDays ?? 0);
            payRoll.Deduction.LossOfPay = model.LossOfPay ?? 0;
            payRoll.Deduction.IncomeTax = model.IncomeTax ?? 0;
            payRoll.Deduction.HealthInsurance = model.HealthInsurance ?? 0;
            payRoll.Deduction.EPF = model.EPF ?? 0;
            payRoll.Deduction.ESIC = model.ESIC ?? 0;
            result = await _empPayRollRepository.Update(expression, payRoll);
        }
        return result;
    }
}