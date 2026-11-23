namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentCandidateDataRedactor
{
    Task RedactAsync(string companyId, string applicantId, string? originalEmail, string actor, DateTime nowUtc);
}
