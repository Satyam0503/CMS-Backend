using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentDuplicateDetectionService(IMongoDbRepository<Applicant> applicants, IHttpContextAccessor context) : IRecruitmentDuplicateDetectionService
{
    public async Task<Result<RecruitmentDuplicateCandidate>> Find(RecruitmentDuplicateLookupRequest request)
    {
        var companyId = CurrentContext.CompanyId(context);
        var result = new Result<RecruitmentDuplicateCandidate> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var email = Normalize(request.Email); var phone = NormalizePhone(request.Phone); var linkedin = Normalize(request.LinkedInUrl);
        if (string.IsNullOrWhiteSpace(companyId) || (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(linkedin)))
        { result.ErrorCode = RecruitmentErrorCodes.InvalidRequest; result.Message = "At least one candidate identifier is required."; return result; }
        var currentId = request.ApplicantId?.Trim();
        var all = await applicants.GetAll(x => x.CompanyId == companyId && !x.IsDeleted);
        foreach (var applicant in all)
        {
            if (!string.IsNullOrWhiteSpace(currentId) && applicant.ApplicantId == currentId) continue;
            var reasons = new List<string>();
            if (!string.IsNullOrWhiteSpace(email) && Normalize(applicant.Email) == email) reasons.Add("Email");
            if (!string.IsNullOrWhiteSpace(phone) && NormalizePhone(applicant.Phone) == phone) reasons.Add("Phone");
            if (!string.IsNullOrWhiteSpace(linkedin) && Normalize(applicant.LinkedInUrl) == linkedin) reasons.Add("LinkedIn");
            if (reasons.Count == 0) continue;
            result.MethodResults.Add(new RecruitmentDuplicateCandidate { ApplicantId = applicant.ApplicantId, FullName = $"{applicant.FirstName} {applicant.LastName}".Trim(), Email = applicant.Email, Phone = applicant.Phone, LinkedInUrl = applicant.LinkedInUrl, MatchReasons = reasons });
        }
        result.Success = true; result.StatusCode = StatusCodes.Status200OK; result.TotalRecords = result.MethodResults.Count; result.Message = result.TotalRecords == 0 ? "No likely duplicate candidates found." : "Potential duplicate candidates found."; return result;
    }
    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static string NormalizePhone(string? value) => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
