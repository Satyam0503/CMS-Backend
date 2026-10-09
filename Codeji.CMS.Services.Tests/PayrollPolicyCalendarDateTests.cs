using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollPolicyCalendarDateTests
{
    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void PolicyCalendarDates_StoreTheChosenDateWithoutHostTimezoneConversion(DateTimeKind kind)
    {
        var policy = Policy(new DateTime(2026, 10, 1, 0, 0, 0, kind));
        policy.EffectiveTo = new DateTime(2027, 1, 31, 0, 0, 0, kind);
        var bson = policy.ToBsonDocument();
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), bson["EffectiveFrom"].ToUniversalTime());
        Assert.Equal(new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc), bson["EffectiveTo"].ToUniversalTime());
        var restored = BsonSerializer.Deserialize<PayrollPolicyProfile>(bson);
        Assert.Equal(new DateTime(2026, 10, 1), restored.EffectiveFrom);
        Assert.Equal(new DateTime(2027, 1, 31), restored.EffectiveTo);
    }

    [Fact]
    public void LegacyIndiaMidnight_ResolvesOctoberPolicyWithoutBlockingSeptember()
    {
        var bson = Policy(new DateTime(2026, 10, 1)).ToBsonDocument();
        bson["EffectiveFrom"] = new BsonDateTime(new DateTime(2026, 9, 30, 18, 30, 0, DateTimeKind.Utc));
        bson["EffectiveTo"] = new BsonDateTime(new DateTime(2027, 1, 30, 18, 30, 0, DateTimeKind.Utc));
        var restored = BsonSerializer.Deserialize<PayrollPolicyProfile>(bson);

        Assert.Equal(new DateTime(2026, 10, 1), restored.EffectiveFrom);
        Assert.Equal(new DateTime(2027, 1, 31), restored.EffectiveTo);
        var september = PayrollPolicyResolver.ResolveConsistentPeriod("company-1", "employee-1",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), [], [], [restored], []);
        var october = PayrollPolicyResolver.ResolveConsistentPeriod("company-1", "employee-1",
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), [], [], [restored], []);
        Assert.True(september.IsConsistent);
        Assert.Null(september.Profile);
        Assert.True(october.IsConsistent);
        Assert.Equal("policy-1", october.Profile?.Id);
    }

    [Fact]
    public void GenuineMidMonthPolicyChange_RemainsBlockedAndApprovalTimestampIsPreserved()
    {
        var policy = Policy(new DateTime(2026, 9, 15));
        policy.ApprovedAtUtc = new DateTime(2026, 9, 10, 18, 30, 0, DateTimeKind.Utc);
        var restored = BsonSerializer.Deserialize<PayrollPolicyProfile>(policy.ToBson());
        var period = PayrollPolicyResolver.ResolveConsistentPeriod("company-1", "employee-1",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), [], [], [restored], []);
        Assert.False(period.IsConsistent);
        Assert.Equal(new DateTime(2026, 9, 15), period.FirstConflictDate);
        Assert.Equal(policy.ApprovedAtUtc, restored.ApprovedAtUtc);
        Assert.Null(restored.EffectiveTo);
    }

    private static PayrollPolicyProfile Policy(DateTime from) => new()
    {
        Id = "policy-1", CompanyId = "company-1", Name = "Monthly policy",
        ApprovalStatus = "APPROVED", EffectiveFrom = from
    };
}
