using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Salary
{
    public class CreateSalaryDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]

        public String EmployeeId {get; set;}
        public decimal BasicPay { get; set; }

        public decimal? Hra { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? Lta { get; set; }
        public decimal? Bonus { get; set; }
        public decimal? HealthInsurance { get; set; }
        public decimal? TotalDeductions { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        [Required]
        public string PaymentFrequency { get; set; } 

        [Required]
        public string Currency { get; set; }
    }
}
