using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class PayRoll
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string CompanyId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime PayMonth { get; set; }
    public required decimal BasicPay { get; set; }
    public decimal Bonus { get; set; } = 0;
    public required decimal PayDays { get; set; }
    public Allowance Allowance { get; set; } = new Allowance();
    public Deduction Deduction { get; set; } = new Deduction();
    public DateTime CreatedAt { get; set; }
}

public class Allowance
{
    public decimal HRA { get; set; } = 0;
    public decimal LTA { get; set; } = 0;
    public decimal Other { get; set; } = 0;
}

public class Deduction
{
    public decimal LossOfPayDays { get; set; }
    public decimal LossOfPay { get; set; } = 0;
    public decimal IncomeTax { get; set; } = 0;
    public decimal HealthInsurance { get; set; } = 0;
}