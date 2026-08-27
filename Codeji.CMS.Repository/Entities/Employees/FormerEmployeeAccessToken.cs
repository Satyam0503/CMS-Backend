using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>Restricted, expiring post-employment access token; never an HRMS login token.</summary>
public sealed class FormerEmployeeAccessToken : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string FormerEmployeeAccessTokenId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? SessionTokenHash { get; set; }
    public DateTime? SessionExpiresAtUtc { get; set; }
    public DateTime? SessionRevokedAtUtc { get; set; }
}
