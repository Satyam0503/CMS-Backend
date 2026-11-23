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
    public bool IsProcessing { get; set; }
    public string? ProcessingAttemptId { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public string? ProcessingFileName { get; set; }
    public string? UploadSha256 { get; set; }
    public DateTime? UploadCompletedAtUtc { get; set; }
    public string? UploadedFileName { get; set; }
}
