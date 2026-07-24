using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;

namespace Codeji.CMS.Services.CareerPortal.Interface;

public interface IPublicCompanyProfileService
{
    Task<Result<PublicCompanyProfileDto>> GetPublishedProfiles();
    Task<Result<PublicCompanyProfileDto>> GetPublishedProfile(string companyCode);
    Task<Result<PublicCompanyProfileDto>> GetCompanyProfile(string companyId);
    Task<Result<PublicCompanyProfileDto>> SaveCompanyProfile(
        string companyId,
        string userId,
        UpsertPublicCompanyProfileRequest request);
}
