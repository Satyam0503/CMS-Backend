using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.DTO.Attendance;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceImportService
{
    Task<(byte[] FileBytes, string FileName)> GenerateTemplateAsync(
        string companyId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<AttendanceImportValidationResultDto> ValidateAsync(
        string companyId,
        int year,
        int month,
        Stream fileStream,
        bool preserveApprovedLeaves = true,
        CancellationToken cancellationToken = default);

    Task<AttendanceImportCommitResultDto> CommitAsync(
        string companyId,
        string actorUserId,
        int year,
        int month,
        Stream fileStream,
        bool preserveApprovedLeaves = true,
        CancellationToken cancellationToken = default);
}

