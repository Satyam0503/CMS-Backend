using Codeji.CMS.Migrations;
using Codeji.CMS.Migrations.Migrations;
using Codeji.CMS.Services.Recruitments;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeScanMigrationContractTests
{
    [Fact]
    public void Migration_loader_discovers_private_resume_scan_index()
    {
        var migration = Assert.Single(MigrationLoader.LoadMigrations(), item => item is AddRecruitmentResumeScanIndex);
        var plan = AddRecruitmentResumeScanIndex.CreateIndexPlan();

        Assert.Equal("Applicant", plan.CollectionName);
        Assert.Equal("ix_ats_resume_scan_migration", plan.Name);
        Assert.True(plan.Keys.Contains("ResumeScannedAtUtc"));
        Assert.True(plan.PartialFilter.Contains("ResumeUrl"));
        Assert.NotNull(typeof(RecruitmentResumeScanMigrationWorker));
    }

    [Fact]
    public void Migration_loader_discovers_tenant_scoped_scan_health_index()
    {
        var migration = Assert.Single(MigrationLoader.LoadMigrations(), item => item is AddRecruitmentResumeScanHealthIndex);
        var plan = AddRecruitmentResumeScanHealthIndex.CreateIndexPlan();

        Assert.Equal("Applicant", plan.CollectionName);
        Assert.Equal("ix_ats_resume_scan_health_by_company", plan.Name);
        Assert.True(plan.Keys.Contains("CompanyId"));
        Assert.True(plan.Keys.Contains("ResumeScanVerdict"));
        Assert.True(plan.PartialFilter.Contains("ResumeUrl"));
        Assert.NotNull(migration);
    }
}
