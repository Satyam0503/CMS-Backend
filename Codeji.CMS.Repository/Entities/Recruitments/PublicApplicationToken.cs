using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public class PublicApplicationToken : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string TokenId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string ApplicationReference { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Purpose { get; set; } = "ResumeUpload";
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
