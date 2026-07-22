using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.DTO.Salary
{
    public class SalaryModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid SalaryId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }
        public string CompanyId { get; set; } = string.Empty;

        public string EmployeeId {get; set;}

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
        public Boolean Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
