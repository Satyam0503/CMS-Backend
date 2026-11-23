using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentResumeOperationsService
{
    Task<Result<RecruitmentResumeScanHealthDto>> GetScanHealth();
}
