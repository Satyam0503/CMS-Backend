using Codeji.CMS.DTO.Salary;

namespace Codeji.CMS.DTO.Salary
{
    public class SalaryResponseDto
    {
        public Guid SalaryId { get; set; }
        public Guid UserId { get; set; }

        public String  EmployeeId {get ; set;}
        public decimal BasicPay { get; set; }
        public decimal? Hra { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? Lta { get; set; }
        public decimal? Bonus { get; set; }
        public decimal? HealthInsurance { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public decimal Ctc { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string PaymentFrequency { get; set; }
        public string Currency { get; set; }
        public Boolean Status { get; set; }

        public static SalaryResponseDto MapFromModel(SalaryModel salary)
        {
            return new SalaryResponseDto
            {
                SalaryId = salary.SalaryId,
                UserId = salary.UserId,
                BasicPay = salary.BasicPay,
                Hra = salary.Hra,
                OtherAllowances = salary.OtherAllowances,
                Lta = salary.Lta,
                Bonus = salary.Bonus,
                HealthInsurance = salary.HealthInsurance,
                GrossSalary = salary.GrossSalary,
                TotalDeductions = salary.TotalDeductions,
                NetSalary = salary.NetSalary,
                Ctc = salary.Ctc,
                EffectiveFrom = salary.EffectiveFrom,
                EffectiveTo = salary.EffectiveTo,
                PaymentFrequency = salary.PaymentFrequency,
                Currency = salary.Currency,
                Status = salary.Status
            };
        }
    }
}
