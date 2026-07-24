using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCareerPortalModule : IMigration
{
    public string Id => $"2026-07-24-06-{typeof(AddCareerPortalModule).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await InitializeJobFields(db.GetCollection<BsonDocument>("JobVacancy"));
        await CreateIndexes(db);
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static Task InitializeJobFields(IMongoCollection<BsonDocument> jobs)
    {
        var missing = Builders<BsonDocument>.Filter.Exists("Summary", false);
        var update = Builders<BsonDocument>.Update
            .Set("Summary", BsonNull.Value).Set("Department", BsonNull.Value)
            .Set("FunctionalArea", BsonNull.Value).Set("Industry", BsonNull.Value)
            .Set("RoleCategory", BsonNull.Value).Set("EducationRequirement", BsonNull.Value)
            .Set("Responsibilities", new BsonArray()).Set("RequiredSkills", new BsonArray())
            .Set("PreferredSkills", new BsonArray()).Set("Benefits", new BsonArray())
            .Set("Keywords", new BsonArray()).Set("ShiftType", BsonNull.Value)
            .Set("WorkingDays", BsonNull.Value).Set("TravelRequirement", BsonNull.Value)
            .Set("IsFeatured", BsonNull.Value).Set("IsUrgentHiring", BsonNull.Value)
            .Set("IsWalkIn", BsonNull.Value).Set("WalkInStartAt", BsonNull.Value)
            .Set("WalkInEndAt", BsonNull.Value).Set("WalkInAddress", BsonNull.Value)
            .Set("RecruiterContactEmail", BsonNull.Value).Set("NoticePeriodMaxDays", BsonNull.Value)
            .Set("ShowSalary", BsonNull.Value);
        return jobs.UpdateManyAsync(missing, update);
    }

    private static async Task CreateIndexes(IMongoDatabase db)
    {
        await Index(db, "CareerSubscriber", "ux_career_subscriber_email", true, Keys(("EmailNormalized", 1)));
        await Index(db, "CareerSubscriber", "ix_career_verification_token", false, Keys(("VerificationTokenHash", 1)));
        await Index(db, "CareerSubscriber", "ix_career_unsubscribe_token", false, Keys(("UnsubscribeTokenHash", 1)));
        await Index(db, "CareerSubscriber", "ix_career_session_token", false, Keys(("SessionTokenHash", 1), ("SessionExpiresAt", 1)));
        await Index(db, "PublicCompanyProfile", "ux_career_profile_company", true, Keys(("CompanyId", 1)));
        await Index(db, "PublicCompanyProfile", "ux_career_profile_code", true, Keys(("PublicCompanyCode", 1)));
        await Index(db, "PublicCompanyProfile", "ix_career_profile_industry", false, Keys(("Industry", 1), ("IsPublished", 1)));
        await Index(db, "CompanyFollower", "ux_career_follower", true, Keys(("SubscriberId", 1), ("CompanyId", 1)));
        await Index(db, "CompanyFollower", "ix_career_company_followers", false, Keys(("CompanyId", 1), ("IsActive", 1)));
        await Index(db, "JobAlertSubscription", "ix_career_alert_subscriber", false, Keys(("SubscriberId", 1), ("IsActive", 1)));
        await Index(db, "JobAlertSubscription", "ix_career_alert_frequency", false, Keys(("NotificationFrequency", 1), ("IsActive", 1)));
        await PartialIndex(db, "SavedJob", "ux_career_saved_subscriber_job", Keys(("SubscriberId", 1), ("JobId", 1)), "{ SubscriberId: { $type: 'string' } }");
        await PartialIndex(db, "SavedJob", "ux_career_saved_visitor_job", Keys(("AnonymousVisitorIdHash", 1), ("JobId", 1)), "{ AnonymousVisitorIdHash: { $type: 'string' } }");
        await Index(db, "SavedJob", "ix_career_saved_job", false, Keys(("JobId", 1), ("IsActive", 1)));
        await Index(db, "CareerVisitorPreference", "ux_career_visitor_preference", true, Keys(("VisitorIdHash", 1)));
        await Index(db, "CareerNotificationOutbox", "ix_career_outbox_claim", false, Keys(("Status", 1), ("AvailableAt", 1)));
        await Index(db, "CareerNotificationOutbox", "ix_career_outbox_subscriber", false, Keys(("SubscriberId", 1), ("Status", 1)));
        await Index(db, "CareerNotificationDelivery", "ux_career_delivery", true, Keys(("SubscriberId", 1), ("JobId", 1), ("NotificationReason", 1)));
        await Index(db, "CareerAnalyticsEvent", "ix_career_analytics_job", false, Keys(("PublicJobId", 1), ("EventType", 1), ("CreatedAt", -1)));
        await Index(db, "JobVacancy", "ix_career_discovery", false, Keys(("PublishToMasterPortal", 1), ("Status", 1), ("IsDeleted", 1), ("PublishedAt", -1)));
    }

    private static BsonDocument Keys(params (string Field, int Direction)[] fields) =>
        new(fields.Select(x => new BsonElement(x.Field, x.Direction)));

    private static Task Index(IMongoDatabase db, string collection, string name, bool unique, BsonDocument keys) =>
        db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name, Unique = unique }));

    private static Task PartialIndex(IMongoDatabase db, string collection, string name, BsonDocument keys, string partial) =>
        db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions<BsonDocument>
            {
                Name = name, Unique = true, PartialFilterExpression = BsonDocument.Parse(partial)
            }));
}
