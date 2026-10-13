using Codeji.CMS.Services.CareerPortal;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Codeji.CMS.Services.Tests;

public sealed class CareerPortalSecurityTests
{
    public CareerPortalSecurityTests()
    {
        ConfigManager.Jwt = new JwtSettings { SecretKey = "career-portal-test-signing-key-with-enough-entropy" };
    }

    [Fact]
    public void UnsubscribeToken_RoundTripsSubscriberId()
    {
        const string subscriberId = "subscriber-42";

        var token = CareerTokenProtector.CreateUnsubscribeToken(subscriberId);

        Assert.NotEqual(subscriberId, token);
        Assert.Equal(subscriberId, CareerTokenProtector.ReadUnsubscribeToken(token));
    }

    [Fact]
    public void UnsubscribeToken_RejectsTampering()
    {
        var token = CareerTokenProtector.CreateUnsubscribeToken("subscriber-42");
        var tampered = token[..^1] + (token[^1] == 'a' ? "b" : "a");

        Assert.Null(CareerTokenProtector.ReadUnsubscribeToken(tampered));
    }

    [Fact]
    public void RichProfileHtml_RemovesExecutableContent()
    {
        var sanitized = Sanitizer.EncodingHtmlText(
            "<h2>Life at Codeji</h2><script>alert('x')</script><p onclick=\"bad()\">Welcome</p>");

        Assert.Contains("<h2>Life at Codeji</h2>", sanitized);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicationEligibility_RequiresBothMasterFlags()
    {
        var now = DateTime.UtcNow;
        var company = new Company
        {
            CompanyName = "Codeji", DefaultLanguage = "en", Status = true,
            CareerPortalEnabled = true, PublishJobsToMasterPortal = false
        };
        var job = new JobVacancy
        {
            Title = "Engineer", Description = "Build", Status = true,
            PublishToCareerPortal = true, PublishToMasterPortal = true,
            ExpiresAt = now.AddDays(2), ApplicationDeadline = now.AddDays(1)
        };

        Assert.True(CareerNotificationService.Eligible(job, company, now, master: false));
        Assert.False(CareerNotificationService.Eligible(job, company, now, master: true));

        company.PublishJobsToMasterPortal = true;
        Assert.True(CareerNotificationService.Eligible(job, company, now, master: true));
    }

    [Fact]
    public void PublicationEligibility_RejectsClosedJob()
    {
        var now = DateTime.UtcNow;
        var company = new Company
        {
            CompanyName = "Codeji", DefaultLanguage = "en", Status = true,
            CareerPortalEnabled = true
        };
        var job = new JobVacancy
        {
            Title = "Engineer", Description = "Build", Status = true,
            PublishToCareerPortal = true, ApplicationDeadline = now.AddSeconds(-1)
        };

        Assert.False(CareerNotificationService.Eligible(job, company, now, master: false));
    }

    [Fact]
    public void PublicFieldsChanged_IgnoresInternalOnlyChanges()
    {
        var before = new JobVacancy
        {
            Title = "Engineer", Description = "Build", Skills = ["C#"], Vacancies = 1
        };
        var after = new JobVacancy
        {
            Title = "Engineer", Description = "Build", Skills = ["C#"], Vacancies = 5
        };

        Assert.False(CareerNotificationService.PublicFieldsChanged(before, after));
        after.Title = "Senior Engineer";
        Assert.True(CareerNotificationService.PublicFieldsChanged(before, after));
    }

    [Fact]
    public void JobAlertMatch_UsesSkillsLocationAndExperience()
    {
        var alert = new JobAlertSubscription
        {
            Name = "Backend", Locations = ["Pune"], Skills = ["C#"],
            ExperienceMin = 2, ExperienceMax = 5
        };
        var job = new JobVacancy
        {
            Title = "Backend Engineer", Description = "APIs", Location = "Pune, India",
            Skills = ["C#", "MongoDB"], ExperienceMin = 3, ExperienceMax = 4
        };

        Assert.True(CareerNotificationService.Matches(alert, job));
        job.Location = "Delhi";
        Assert.False(CareerNotificationService.Matches(alert, job));
    }

    [Fact]
    public void SavedJob_DeserializesExistingMongoObjectId()
    {
        var id = ObjectId.GenerateNewId();
        var saved = BsonSerializer.Deserialize<SavedJob>(new BsonDocument
        {
            ["_id"] = id,
            ["JobId"] = "job-1",
            ["PublicJobId"] = "public-1",
            ["CompanyId"] = "company-1",
            ["SavedAt"] = DateTime.UtcNow,
            ["IsActive"] = true
        });

        Assert.Equal(id.ToString(), saved.SavedJobId);
    }

    [Theory]
    [InlineData(typeof(CareerVisitorPreference), "VisitorPreferenceId")]
    [InlineData(typeof(CompanyFollower), "CompanyFollowerId")]
    public void UpsertedCareerEntity_DeserializesMongoObjectId(Type entityType, string idProperty)
    {
        var id = ObjectId.GenerateNewId();
        var entity = BsonSerializer.Deserialize(new BsonDocument { ["_id"] = id }, entityType);

        Assert.Equal(id.ToString(), entityType.GetProperty(idProperty)!.GetValue(entity));
    }
}
