using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentDuplicateLookupRequest
{
    public string? ApplicantId { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
}
public sealed class RecruitmentDuplicateCandidate
{
    public string ApplicantId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public List<string> MatchReasons { get; set; } = [];
}
