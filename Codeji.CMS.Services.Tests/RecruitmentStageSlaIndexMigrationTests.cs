using Codeji.CMS.Migrations.Migrations;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentStageSlaIndexMigrationTests
{
    [Fact]
    public void Stage_sla_indexes_support_tenant_scoped_paging_and_location_policy_lookup()
    {
        var plans = AddRecruitmentStageSlaIndexes.CreateIndexPlans();
        Assert.Equal(3, plans.Count);
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentJobApplication" && x.Keys.Contains("ApplicationId"));
        Assert.Contains(plans, x => x.CollectionName == "JobVacancy" && x.Keys.Contains("JobId"));
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentStageSlaPolicy" && x.Keys.Contains("Stage") && x.Keys.Contains("Location"));
    }

    [Fact]
    public void Migration_loader_discovers_stage_sla_index_migration()
    {
        var migration = typeof(AddRecruitmentStageSlaIndexes).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(Codeji.CMS.Migrations.Interface.IMigration).IsAssignableFrom(x))
            .Select(x => (Codeji.CMS.Migrations.Interface.IMigration)Activator.CreateInstance(x)!)
            .SingleOrDefault(x => x is AddRecruitmentStageSlaIndexes);
        Assert.NotNull(migration);
        Assert.Equal("2026-11-18-AddRecruitmentStageSlaIndexes", migration.Id);
    }
}
