using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;

namespace Codeji.CMS.Services.CareerPortal.Interface;

public interface ICareerEngagementService
{
    Task<Result> SaveJob(string publicJobId, string sessionToken, string visitorToken);
    Task<Result> RemoveSavedJob(string publicJobId, string sessionToken, string visitorToken);
    Task<Result<SavedCareerJobDto>> GetSavedJobs(string sessionToken, string visitorToken);
    Task<Result> MergeAnonymousSaves(string sessionToken, string visitorToken);
    Task<Result> FollowCompany(string companyCode, string sessionToken, FollowCompanyRequest request);
    Task<Result> UnfollowCompany(string companyCode, string sessionToken);
    Task<Result<CompanyFollowStatusDto>> GetFollowStatus(string companyCode, string sessionToken);
    Task<Result<JobAlertDto>> CreateAlert(string sessionToken, JobAlertRequest request);
    Task<Result<JobAlertDto>> GetAlerts(string sessionToken);
    Task<Result<JobAlertDto>> UpdateAlert(string alertId, string sessionToken, JobAlertRequest request);
    Task<Result> DeleteAlert(string alertId, string sessionToken);
    Task<Result> Track(CareerAnalyticsRequest request);
    Task<Result> UpdateVisitorPreference(VisitorPreferenceRequest request);
    Task<Result<VisitorPreferenceDto>> GetVisitorPreference(string visitorToken);
}
