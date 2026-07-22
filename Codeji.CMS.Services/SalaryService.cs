using Codeji.CMS.DTO.Salary;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly ISalaryRepository _repository;
        private readonly IMongoDbRepository<EmpUser> _employeeRepository;
        private readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
        private readonly ILogger<SalaryService> _logger;

        public SalaryService(
            ISalaryRepository repository,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<JobTitles> jobTitlesRepository,
            ILogger<SalaryService> logger)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _jobTitlesRepository = jobTitlesRepository;
            _logger = logger;
        }

        public async Task<SalaryModel> CreateSalaryAsync(string companyId, CreateSalaryDto dto)
        {
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.Status && !x.IsDeleted && x.UserId == dto.UserId.ToString());
            if (employee == null) throw new InvalidOperationException("Employee does not belong to the authenticated company.");
            dto.EmployeeId = employee.EmployeeId;
            // mark previous active salary inactive
            var activeSalary = await _repository.GetActiveSalaryAsync(companyId, dto.UserId);
            if (activeSalary != null)
            {
                activeSalary.Status = false;
                activeSalary.EffectiveTo = dto.EffectiveFrom.AddDays(-1);
                activeSalary.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(activeSalary);
            }

            var (gross, net, ctc) = SalaryCalculator.Calculate(dto);

            var newSalary = new SalaryModel
            {
                SalaryId = Guid.NewGuid(),
                UserId = dto.UserId,
                CompanyId = companyId,
                EmployeeId = dto.EmployeeId,
                BasicPay = dto.BasicPay,
                Hra = dto.Hra,
                OtherAllowances = dto.OtherAllowances,
                Lta = dto.Lta,
                Bonus = dto.Bonus,
                HealthInsurance = dto.HealthInsurance,
                TotalDeductions = dto.TotalDeductions,
                GrossSalary = gross,
                NetSalary = net,
                Ctc = ctc,
                EffectiveFrom = dto.EffectiveFrom,
                Status = true,
                PaymentFrequency = dto.PaymentFrequency,
                Currency = dto.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(newSalary);

            return newSalary;
        }

        public async Task<SalaryResponseDto?> GetActiveSalaryAsync(string companyId, Guid userId)
        {
            var salary = await _repository.GetActiveSalaryAsync(companyId, userId);
            return salary == null ? null : SalaryResponseDto.MapFromModel(salary);
        }

        public async Task<List<SalaryResponseDto>> GetSalaryHistoryAsync(string companyId, Guid userId)
        {
            var salaries = await _repository.GetSalaryHistoryAsync(companyId, userId);
            return salaries.Select(SalaryResponseDto.MapFromModel).ToList();
        }

        public async Task<List<CompanySalaryResponseDto>> GetCompanySalariesAsync(string companyId, string? employeeName)
        {
            var employees = string.IsNullOrEmpty(employeeName)
                ? await _employeeRepository.GetAll(e => e.CompanyId == companyId)
                : await _employeeRepository.GetAll(e => e.CompanyId == companyId
                    && (e.FirstName + " " + e.LastName).Contains(employeeName, StringComparison.CurrentCultureIgnoreCase));

            // SalaryModel.UserId is a Guid while EmpUser.UserId is a string - only employees
            // whose id parses as a Guid can have a salary structure looked up
            var employeeGuidIds = employees
                .Select(e => (Employee: e, Parsed: Guid.TryParse(e.UserId, out var guid), Guid: guid))
                .ToList();

            List<SalaryModel> activeSalaries = [];
            try
            {
                var validUserIds = employeeGuidIds
                    .Where(e => e.Parsed)
                    .Select(e => e.Guid)
                    .ToList();
                if (validUserIds.Count > 0)
                {
                    activeSalaries = await _repository.GetActiveSalariesAsync(companyId, validUserIds);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to load active salary structures for company {CompanyId}; returning employees without salary data.",
                    companyId);
            }
            var activeSalaryByUserId = activeSalaries
                .GroupBy(s => s.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(s => s.EffectiveFrom).First());

            List<string> jobIds = employees.Select(e => e.JobRole).Distinct().Where(jt => jt != null).ToList();
            IEnumerable<JobTitles> jobTitles = [];
            try
            {
                if (jobIds.Count > 0)
                {
                    jobTitles = await _jobTitlesRepository.GetAll(jt => jobIds.Contains(jt.JobTitleId));
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to load job titles for company {CompanyId}; returning employees without job titles.",
                    companyId);
            }

            return (from emp in employeeGuidIds
                    join jobTitle in jobTitles on emp.Employee.JobRole equals jobTitle.JobTitleId into jobTitleGroup
                    from empJobTitle in jobTitleGroup.DefaultIfEmpty()
                    select new CompanySalaryResponseDto
                    {
                        UserId = emp.Employee.UserId,
                        EmployeeId = emp.Employee.EmployeeId,
                        EmployeeName = $"{emp.Employee.FirstName} {emp.Employee.LastName}",
                        JobTitle = empJobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label) ?? null,
                        ActiveSalary = emp.Parsed && activeSalaryByUserId.TryGetValue(emp.Guid, out var salary)
                            ? SalaryResponseDto.MapFromModel(salary)
                            : null
                    })
                    .OrderBy(employee => employee.EmployeeName)
                    .ToList();
        }
    }
}
