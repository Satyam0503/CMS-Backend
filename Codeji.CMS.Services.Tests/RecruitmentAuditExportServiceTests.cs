using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.DTO.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentAuditExportServiceTests
{
    [Fact]
    public async Task Export_is_tenant_scoped_and_escapes_csv_values()
    {
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.GetAll(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<ApplicantLogs, bool>> filter, bool _, bool __) => new[]
            {
                new ApplicantLogs { CompanyId = "company-1", Id = "l-1", ApplicantId = "a-1", ApplicantName = "Ada, Lovelace", Description = "Moved to \"Interview\"", EventType = "StageChanged", CreatedDate = DateTime.UtcNow },
                new ApplicantLogs { CompanyId = "company-2", Id = "l-2", ApplicantId = "a-2", ApplicantName = "Other", CreatedDate = DateTime.UtcNow }
            }.AsQueryable().Where(filter).ToArray());
        var service = new RecruitmentAuditExportService(logs.Object, TenantContext("company-1"));

        var result = await service.Export(new RecruitmentAuditExportQuery());

        Assert.True(result.Success);
        Assert.Equal(1, result.MethodResult.Count);
        var csv = Encoding.UTF8.GetString(Convert.FromBase64String(result.MethodResult.ContentBase64));
        Assert.Contains("\"Ada, Lovelace\"", csv);
        Assert.Contains("\"Moved to \"\"Interview\"\"\"", csv);
        Assert.DoesNotContain("Other", csv);
    }

    [Fact]
    public async Task Export_rejects_inverted_date_range()
    {
        var service = new RecruitmentAuditExportService(new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Export(new RecruitmentAuditExportQuery { FromUtc = DateTime.UtcNow, ToUtc = DateTime.UtcNow.AddDays(-1) });
        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
