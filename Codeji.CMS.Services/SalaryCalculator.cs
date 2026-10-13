using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.Services
{
    public static class SalaryCalculator
    {
        public static (decimal gross, decimal net, decimal ctc) Calculate(CreateSalaryDto dto)
        {
            var hra = dto.Hra ?? 0;
            var other = dto.OtherAllowances ?? 0;
            var lta = dto.Lta ?? 0;
            var bonus = dto.Bonus ?? 0;
            var insurance = dto.HealthInsurance ?? 0;
            var deductions = dto.TotalDeductions ?? 0;

            var gross = dto.BasicPay + hra + other + lta + bonus;
            var effectiveDeductions = deductions > 0 ? deductions : insurance;
            var net = gross - effectiveDeductions;
            var ctc = gross + insurance;

            return (gross, net, ctc);
        }
    }
}
