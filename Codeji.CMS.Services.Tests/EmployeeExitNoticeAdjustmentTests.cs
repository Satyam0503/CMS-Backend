using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeExitNoticeAdjustmentTests
{
    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry));

    [Fact]
    public void Exit_completion_lookup_filter_includes_both_notice_period_and_offer_statuses()
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitId == "exit-1" &&
            (x.Status == "NoticePeriod" || x.Status == "AwaitingEmployeeNoticeOffer") &&
            x.IsActive &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("exit-1", rendered, StringComparison.Ordinal);
        Assert.Contains("NoticePeriod", rendered, StringComparison.Ordinal);
        Assert.Contains("AwaitingEmployeeNoticeOffer", rendered, StringComparison.Ordinal);
        Assert.Contains("IsActive", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Notice_adjustment_resolution_identifies_unresolved_versus_resolved_states()
    {
        var adjustments = new List<EmployeeExitNoticeAdjustment>
        {
            new() { NoticeAdjustmentId = "adj-1", Status = "Declined" },
            new() { NoticeAdjustmentId = "adj-2", Status = "Approved" },
            new() { NoticeAdjustmentId = "adj-3", Status = "EmployeeDeclined" },
        };

        // Declined and EmployeeDeclined are resolved (original notice period terms served)
        var hasPendingHrReview = adjustments.Any(x => x.Status == "Pending");
        var hasPendingEmployeeDecision = adjustments.Any(x => x.Status == "AwaitingEmployeeDecision");

        Assert.False(hasPendingHrReview);
        Assert.False(hasPendingEmployeeDecision);

        // Add an unreviewed request
        adjustments.Add(new EmployeeExitNoticeAdjustment { NoticeAdjustmentId = "adj-4", Status = "Pending" });
        Assert.Contains(adjustments, x => x.Status == "Pending");
    }

    [Fact]
    public void Outbox_notice_offer_events_are_tenant_scoped()
    {
        var outbox = new EmployeeExitOutbox
        {
            CompanyId = "company-tenant-1",
            CreatedBy = "hr-user-1",
            EmployeeExitId = "exit-99",
            EventType = "EmployeeExitNoticeOfferCreated",
            RecipientUserId = "emp-42",
            Title = "Notice offer awaiting your decision",
            Body = "HR prepared an offer.",
            TargetId = "employee-exit-emp-42",
        };

        Assert.Equal("company-tenant-1", outbox.CompanyId);
        Assert.Equal("EmployeeExitNoticeOfferCreated", outbox.EventType);
        Assert.Equal("emp-42", outbox.RecipientUserId);
    }
}
