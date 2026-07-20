using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class EmpPayRoll
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string CompanyId { get; set; }
    public required string UserId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime PayMonth { get; set; }
    public required DateTime PaidDate { get; set; }
    public required decimal BasicPay { get; set; }
    public decimal Bonus { get; set; }
    public required float PaidDays { get; set; }
    public Allowance Allowance { get; set; } = new Allowance();
    public Deduction Deduction { get; set; } = new Deduction();
    public DateTime CreatedAt { get; set; }
}

public class Allowance
{
    public decimal HRA { get; set; }
    public decimal LTA { get; set; }
    public decimal OtherAllowance { get; set; }
}

public class Deduction
{
    public float LossOfPayDays { get; set; }
    public decimal LossOfPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal HealthInsurance { get; set; }
    public decimal EPF { get; set; }
    public decimal ESIC { get; set; }
}