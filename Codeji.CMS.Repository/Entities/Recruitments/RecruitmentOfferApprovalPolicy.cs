using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentOfferApprovalPolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string PolicyId { get; set; } = string.Empty;
    public string? VacancyId { get; set; }
    public string? LegalEntityId { get; set; }
    public string? DepartmentId { get; set; }
    public string? Grade { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal ApprovalThreshold { get; set; }
    public bool RequireApprovalReference { get; set; } = true;
    public string? ApproverRoleId { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

