using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Utility.Helpers;
using LinqKit;

namespace Codeji.CMS.Services.PayRoll;

public class PayRollServices : IPayRollServices
{
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
        PdfService pdfService
    )
    {
        _empPayRollRepository = empPayRollRepository;
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _jobTitlesRepository = jobTitlesRepository;
        _pdfService = pdfService;
    }


    // generate salary slip

    public async Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId)
    {
        try
        {
            // get month and year 
            var month = model.Month;
            var year = model.Year;
            SalarySlipTemplateModel salarySlipModel = new();
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(emp => emp.UserId == userId);
            Company? company = await _companyRepository.FirstOrDefault(c => c.CompanyId == empUser.CompanyId);
            if (company == null || empUser == null) throw new Exception();

            // get pay details based on month and year
            Expression<Func<EmpPayRoll, bool>> expression = p => p.EmployeeId == empUser.EmployeeId && p.UserId == empUser.UserId && p.CompanyId == empUser.CompanyId && p.PayMonth.Month == model.Month && p.PayMonth.Year == model.Year;
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
            salarySlipModel.PaidDate = payRoll.PayMonth.ToString("ddd, dd MMM yyyy");
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
            salarySlipModel.GrossPay = payRoll.BasicPay + payRoll.Allowance.HRA + payRoll.Allowance.LTA + payRoll.Bonus + payRoll.Allowance.OtherAllowance;
            salarySlipModel.TotalDeduction = payRoll.Deduction.IncomeTax + payRoll.Deduction.HealthInsurance + payRoll.Deduction.LossOfPay;
            salarySlipModel.NetSalary = salarySlipModel.GrossPay - salarySlipModel.TotalDeduction;

            string templateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "SalarySlipTemplate.html");
            string fileContent = File.ReadAllText(templateFilePath);

            if (company.CompanyLogo != null)
            {
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CompanyLogo", company.CompanyLogo);
                if (File.Exists(logoPath))
                {
                    byte[] logoByteArray = File.ReadAllBytes(logoPath);
                    string logoBase64Format = Convert.ToBase64String(logoByteArray);
                    string logoExtension = company.CompanyLogo.Split(".").Last();
                    salarySlipModel.CompanyLogo = $"data:image/{logoExtension};base64,{logoBase64Format}";
                }
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

    public async Task<Result> UploadPayrollData(EmplyeePayRollRequestDto payLoad, string companyId)
    {
        Result result = new();
        // check employee existance
        List<EmployeePayRollModel> payData = payLoad.PayData;
        for (int i = payData.Count - 1; i >= 0; i--)
        {
            bool exist = await _employeeRepository.Exist(e => e.EmployeeId == payData[i].EmployeeId && e.CompanyId == companyId);
            if (!exist)
            {
                payData.RemoveAt(i);
            }
        }

        List<string> employeeIds = payData.Select(m => m.EmployeeId).ToList();
        IEnumerable<EmpUser> empUsers = await _employeeRepository.GetAll(e => employeeIds.Contains(e.EmployeeId));
        var dataList = from emp in empUsers
                       join payItem in payData on emp.EmployeeId equals payItem.EmployeeId
                       select new EmpPayRoll()
                       {
                           CompanyId = companyId,
                           UserId = emp.UserId,
                           EmployeeId = emp.EmployeeId,
                           PayMonth = payLoad.PayMonth,
                           BasicPay = payItem.BasicPay ?? 0,
                           Bonus = payItem.Bonus ?? 0,
                           PaidDays = payItem.PaidDays ?? 0,
                           CreatedAt = DateTime.UtcNow,
                           Allowance = new Allowance()
                           {
                               HRA = payItem.HRA ?? 0,
                               LTA = payItem.LTA ?? 0,
                               OtherAllowance = payItem.OtherAllowance ?? 0
                           },
                           Deduction = new Deduction()
                           {
                               LossOfPay = payItem.LossOfPay ?? 0,
                               LossOfPayDays = payItem.LossOfPayDays ?? 0,
                               IncomeTax = payItem.IncomeTax ?? 0,
                               HealthInsurance = payItem.HealthInsurance ?? 0
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
            matchingEmployeeIds = (await _employeeRepository.GetAll(e => (e.FirstName.Contains(payload.EmployeeName, StringComparison.CurrentCultureIgnoreCase) || e.LastName.Contains(payload.EmployeeName, StringComparison.CurrentCultureIgnoreCase)) && e.CompanyId == companyId)).Select(e => e.UserId).ToList();
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
                            BasicPay = payRoll?.BasicPay ?? null,
                            PaidDays = payRoll?.PaidDays ?? null,
                            Bonus = payRoll?.Bonus ?? null,
                            HRA = payRoll?.Allowance.HRA ?? null,
                            LTA = payRoll?.Allowance.LTA ?? null,
                            OtherAllowance = payRoll?.Allowance.OtherAllowance ?? null,
                            LossOfPayDays = payRoll?.Deduction.LossOfPayDays ?? null,
                            LossOfPay = payRoll?.Deduction.LossOfPay ?? null,
                            IncomeTax = payRoll?.Deduction.IncomeTax ?? null,
                            HealthInsurance = payRoll?.Deduction.HealthInsurance ?? null
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
                    LossOfPayDays = model.LossOfPayDays ?? 0,
                    LossOfPay = model.LossOfPay ?? 0,
                    IncomeTax = model.IncomeTax ?? 0,
                    HealthInsurance = model.HealthInsurance ?? 0
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
            payRoll.Allowance.HRA = model.HRA ?? 0;
            payRoll.Allowance.LTA = model.LTA ?? 0;
            payRoll.Allowance.OtherAllowance = model.OtherAllowance ?? 0;
            payRoll.Deduction.LossOfPayDays = model.LossOfPayDays ?? 0;
            payRoll.Deduction.LossOfPay = model.LossOfPay ?? 0;
            payRoll.Deduction.IncomeTax = model.IncomeTax ?? 0;
            payRoll.Deduction.HealthInsurance = model.HealthInsurance ?? 0;
            result = await _empPayRollRepository.Update(expression, payRoll);
        }
        return result;
    }
}