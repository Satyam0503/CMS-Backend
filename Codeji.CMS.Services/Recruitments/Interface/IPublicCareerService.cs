using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PublicCareers;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IPublicCareerService
{
    Task<Result<PublicJobSummaryDto>> GetMasterJobs(PublicJobSearchRequest request);
    Task<Result<PublicJobSummaryDto>> GetCompanyJobs(string companyCode, PublicJobSearchRequest request);
    Task<Result<PublicJobDetailsDto>> GetJobByPublicId(string publicJobId);
    Task<Result<PublicJobDetailsDto>> GetJobBySlug(string jobSlug);
    Task<Result<PublicJobApplicationResponse>> Apply(string publicJobId, PublicJobApplicationRequest request);
    Task<Result<PublicResumeUploadResult>> UploadResume(string applicationReference, string token, IFormFile resume);
}
