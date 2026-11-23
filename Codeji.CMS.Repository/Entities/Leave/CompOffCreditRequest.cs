using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class CompOffCreditRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime WorkedDate { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal CreditDays { get; set; } = 1.0m;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

