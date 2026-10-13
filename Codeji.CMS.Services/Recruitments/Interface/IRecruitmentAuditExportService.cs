using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;

namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentAuditExportService
{
    Task<Result<RecruitmentAuditExportResult>> Export(RecruitmentAuditExportQuery query);
}
