using Codeji.CMS.Migrations;
using Codeji.CMS.Migrations.Migrations;
using MongoDB.Bson;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentConfigCompanyUniqueIndexTests
{
    [Fact]
    public void Active_tenant_configuration_index_is_unique_and_partial_for_active_records()
    {
        var plan = AddRecruitmentConfigCompanyUniqueIndex.CreateIndexPlan();

        Assert.Equal("ux_ats_config_active_company", plan.Name);
        Assert.True(plan.Unique);
        Assert.Equal(1, plan.Keys.GetValue("CompanyId").ToInt32());
        Assert.False(plan.PartialFilter.GetValue("IsDeleted").AsBoolean);
    }

    [Fact]
    public void Preflight_reports_duplicate_tenants_and_active_records_without_company_ids()
    {
        var records = new[]
        {
            new BsonDocument("CompanyId", "tenant-a"),
            new BsonDocument("CompanyId", "tenant-a"),
            new BsonDocument(),
        };

        Assert.Equal(
            ["missing CompanyId (1 active record(s))", "tenant-a (2 active records)"],
            AddRecruitmentConfigCompanyUniqueIndex.FindPreflightIssues(records));
    }

    [Fact]
    public void Preflight_allows_one_active_record_per_company()
    {
        var records = new[]
        {
            new BsonDocument("CompanyId", "tenant-a"),
            new BsonDocument("CompanyId", "tenant-b"),
        };

        Assert.Empty(AddRecruitmentConfigCompanyUniqueIndex.FindPreflightIssues(records));
    }

    [Fact]
    public void Migration_loader_discovers_the_unique_tenant_configuration_index()
    {
        var migration = MigrationLoader.LoadMigrations()
            .SingleOrDefault(candidate => candidate is AddRecruitmentConfigCompanyUniqueIndex);

        Assert.NotNull(migration);
        Assert.Equal("2026-11-19-AddRecruitmentConfigCompanyUniqueIndex", migration.Id);
    }
}
