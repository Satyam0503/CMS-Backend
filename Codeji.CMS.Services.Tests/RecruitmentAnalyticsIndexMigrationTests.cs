using Codeji.CMS.Migrations.Migrations;
using Codeji.CMS.Migrations;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentAnalyticsIndexMigrationTests
{
    [Fact]
    public void AnalyticsIndexesCoverTenantPagingBatchJoinsAndSoftDeletedMerges()
    {
        var plans = AddRecruitmentAnalyticsIndexes.CreateIndexPlans();

        Assert.Equal(7, plans.Count);
        Assert.Contains(plans, x => x.CollectionName == "Applicant" && x.Name == "ix_ats_analytics_company_active_page" &&
            x.Keys.GetValue("CompanyId").ToInt32() == 1 && x.Keys.GetValue("ApplicantId").ToInt32() == 1);
        Assert.Contains(plans, x => x.CollectionName == "Applicant" && x.Name == "ix_ats_analytics_company_vacancy_active_page" &&
            x.Keys.GetValue("VacancyId").ToInt32() == 1);
        Assert.Contains(plans, x => x.CollectionName == "Applicant" && x.Name == "ix_ats_analytics_company_merged_count" &&
            x.PartialFilter?.GetValue("MergeStatus").AsString == "Merged");
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentJobApplication" && x.Name == "ix_ats_analytics_application_batch" &&
            x.Keys.GetValue("UpdatedDate").ToInt32() == -1);
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentJobApplication" && x.Name == "ix_ats_analytics_application_vacancy_batch" &&
            x.Keys.GetValue("VacancyId").ToInt32() == 1);
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentOffer" && x.Name == "ix_ats_analytics_offer_batch" &&
            x.Keys.GetValue("Status").ToInt32() == 1);
        Assert.Contains(plans, x => x.CollectionName == "RecruitmentOffer" && x.Name == "ix_ats_analytics_offer_vacancy_batch" &&
            x.Keys.GetValue("VacancyId").ToInt32() == 1);
    }

    [Fact]
    public void MigrationLoaderDiscoversAnalyticsIndexMigration()
    {
        var migration = MigrationLoader.LoadMigrations()
            .SingleOrDefault(x => x is AddRecruitmentAnalyticsIndexes);

        Assert.NotNull(migration);
        Assert.Equal("2026-11-18-AddRecruitmentAnalyticsIndexes", migration.Id);
    }
}
