using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Services.Interfaces;

namespace Codeji.CMS.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly ISalaryRepository _repository;

        public SalaryService(ISalaryRepository repository)
        {
            _repository = repository;
        }

        public async Task<SalaryModel> CreateSalaryAsync(CreateSalaryDto dto)
        {
            // mark previous active salary inactive
            var activeSalary = await _repository.GetActiveSalaryAsync(dto.UserId);
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

        public async Task<SalaryResponseDto?> GetActiveSalaryAsync(Guid userId)
        {
            var salary = await _repository.GetActiveSalaryAsync(userId);
            return salary == null ? null : SalaryResponseDto.MapFromModel(salary);
        }

        public async Task<List<SalaryResponseDto>> GetSalaryHistoryAsync(Guid userId)
        {
            var salaries = await _repository.GetSalaryHistoryAsync(userId);
            return salaries.Select(SalaryResponseDto.MapFromModel).ToList();
        }
    }
}
