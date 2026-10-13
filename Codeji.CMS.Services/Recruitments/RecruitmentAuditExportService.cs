using System.Linq.Expressions;
using System.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentAuditExportService(
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context) : IRecruitmentAuditExportService
{
    private const int MaxRows = 10000;

    public async Task<Result<RecruitmentAuditExportResult>> Export(RecruitmentAuditExportQuery query)
    {
        query ??= new RecruitmentAuditExportQuery();
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return Fail(400, "A tenant context is required.");
        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc) return Fail(400, "FromUtc must be earlier than ToUtc.");
        var limit = Math.Clamp(query.Limit <= 0 ? MaxRows : query.Limit, 1, MaxRows);
        var applicantId = query.ApplicantId?.Trim();
        var eventType = query.EventType?.Trim();
        Expression<Func<ApplicantLogs, bool>> filter = x =>
            x.CompanyId == companyId &&
            (!x.IsDeleted) &&
            (string.IsNullOrWhiteSpace(applicantId) || x.ApplicantId == applicantId) &&
            (string.IsNullOrWhiteSpace(eventType) || x.EventType == eventType) &&
            (!query.FromUtc.HasValue || x.CreatedDate >= query.FromUtc.Value) &&
            (!query.ToUtc.HasValue || x.CreatedDate <= query.ToUtc.Value);
        var rows = (await logs.GetAll(filter)).OrderByDescending(x => x.CreatedDate).Take(limit).ToList();
        var csv = new StringBuilder();
        csv.AppendLine("Id,ApplicantId,ApplicantName,JobRole,UserId,EventType,PreviousStage,CurrentStage,Description,CreatedDate,UpdatedDate");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',',
                Csv(row.Id), Csv(row.ApplicantId), Csv(row.ApplicantName), Csv(row.JobRole), Csv(row.UserId),
                Csv(row.EventType), Csv(row.PreviousStage), Csv(row.CurrentStage), Csv(row.Description),
                Csv(row.CreatedDate?.ToUniversalTime().ToString("O")), Csv(row.UpdatedDate?.ToUniversalTime().ToString("O"))));
        }
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return new Result<RecruitmentAuditExportResult>
        {
            Success = true,
            MethodResult = new RecruitmentAuditExportResult
            {
                FileName = $"recruitment-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
                ContentBase64 = Convert.ToBase64String(bytes),
                Count = rows.Count
            },
            TotalRecords = rows.Count,
            Message = "Recruitment audit export generated."
        };
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static Result<RecruitmentAuditExportResult> Fail(int status, string message) => new()
    {
        Success = false,
        StatusCode = status,
        ErrorCode = RecruitmentErrorCodes.InvalidRequest,
        Message = message
    };
}
