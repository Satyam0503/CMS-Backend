using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.DTO.Company.Policy;

public class PolicyVersionResponseModel
{
    public string? PolicyDocUrl { get; set; }
    public string VersionName { get; set; }
    public string Id { get; set; }
    public bool IsCurrent { get; set; }
}

public class PolicyVersionRequestModel
{
    public required string VersionName { get; set; }
    public required string PolicyId { get; set; }
    public required IFormFile PolicyDoc { get; set; }
    public bool IsCurrent { get; set; }
}

public class PolicyVersionUpdateModel
{
    public required string Id { get; set; }
    public required string VersionName { get; set; }
    public required string PolicyId { get; set; }
    public IFormFile? PolicyDoc { get; set; }
    public bool IsCurrent { get; set; }
}

public class PolicyDocumentResult
{
    public byte[] FileContent { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
}

public class SetCurrentPolicyVersionRequestModel
{
    public required string PolicyId { get; set; }
    public required string VersionId { get; set; }
}
