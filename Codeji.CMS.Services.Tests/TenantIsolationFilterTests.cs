using Codeji.CMS.GenericRepository.Extensions;
using Codeji.CMS.Repository.Entities.Leave;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

/// <summary>
/// Tenant-isolation regression tests for the single place every repository read is
/// funnelled through: <see cref="IQueryableCustomExtensions.ApplyDefaultFilters{T}"/>.
///
/// These are pure expression/BSON assertions - no MongoClient is constructed and no
/// database is contacted.
/// </summary>
public sealed class TenantIsolationFilterTests
{
    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry));

    private static bool MentionsCompanyId(BsonDocument rendered) =>
        rendered.ToJson().Contains("CompanyId", StringComparison.Ordinal);

    [Fact]
    public void AuditedEntity_WithResolvedCompanyId_IsTenantScoped()
    {
        var rendered = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            x => x.EmployeeId == "user-1", withDeletedObjects: false, "company-alpha"));

        Assert.True(MentionsCompanyId(rendered), rendered.ToJson());
        Assert.Contains("company-alpha", rendered.ToJson(), StringComparison.Ordinal);
    }

    /// <summary>
    /// BUG-01. MongoRepository.GetQuery passes GetCompanyId() straight through, and
    /// GetCompanyId() returns null whenever HttpContext.Items["CompanyId"] is absent
    /// (background hosted services, SignalR hub callbacks, anonymous endpoints).
    /// ApplyDefaultFilters then skips the tenant predicate entirely instead of
    /// failing closed, so the query runs across every tenant in the database.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AuditedEntity_WithoutCompanyId_SilentlyDropsTheTenantFilter(string? companyId)
    {
        var rendered = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            x => x.EmployeeId == "user-1", withDeletedObjects: false, companyId!));

        Assert.False(
            MentionsCompanyId(rendered),
            "Expected the tenant filter to be dropped - this assertion documents current, unsafe behaviour. " +
            "If it starts failing the repository now fails closed and BUG-01 is fixed. Rendered: " + rendered.ToJson());
    }

    /// <summary>
    /// BUG-01b. MongoRepository.GetQuery calls
    /// ApplyDefaultFilters(filter, withDeleted, withDefaultFilter ? GetCompanyId() : "")
    /// so the documented "withDefaultFilter: false" escape hatch removes the tenant
    /// scope as well as the soft-delete scope - callers that only wanted deleted rows
    /// silently opt out of multi-tenancy too.
    /// </summary>
    [Fact]
    public void WithDefaultFilterFalse_RemovesTenantScopeNotJustSoftDelete()
    {
        var scoped = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            x => x.EmployeeId == "user-1", false, "company-alpha"));
        var unscoped = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            x => x.EmployeeId == "user-1", false, string.Empty));

        Assert.True(MentionsCompanyId(scoped));
        Assert.False(MentionsCompanyId(unscoped));
    }

    /// <summary>
    /// BUG-02. AttendanceModel and AttendanceStatusSetting carry a CompanyId column but do
    /// not derive from BaseClass, so they implement neither ISupportAuditing nor
    /// ISupportSoftDelete. ApplyDefaultFilters keys off those interfaces, which means the
    /// attendance collection gets no automatic tenant filter and no soft-delete filter at
    /// all - every call site has to remember to add "a.CompanyId == companyId" by hand.
    /// </summary>
    [Fact]
    public void AttendanceModel_NeverReceivesAnAutomaticTenantFilter()
    {
        Assert.False(typeof(Codeji.CMS.GenericRepository.Interfaces.ISupportAuditing)
            .IsAssignableFrom(typeof(AttendanceModel)));

        var rendered = Render(IQueryableCustomExtensions.ApplyDefaultFilters<AttendanceModel>(
            a => a.UserId == "user-1", withDeletedObjects: false, "company-alpha"));

        Assert.False(
            MentionsCompanyId(rendered),
            "Attendance queries are not tenant scoped by the repository. Rendered: " + rendered.ToJson());
    }

    [Fact]
    public void AttendanceStatusSetting_NeverReceivesAnAutomaticTenantFilter()
    {
        Assert.False(typeof(Codeji.CMS.GenericRepository.Interfaces.ISupportAuditing)
            .IsAssignableFrom(typeof(AttendanceStatusSetting)));
    }

    [Fact]
    public void SoftDeleteFilter_IsAppliedForBaseClassEntities()
    {
        var rendered = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            null, withDeletedObjects: false, "company-alpha"));

        Assert.Contains("IsDeleted", rendered.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void SoftDeleteFilter_IsSkippedWhenDeletedRowsRequested()
    {
        var rendered = Render(IQueryableCustomExtensions.ApplyDefaultFilters<LeaveRequest>(
            null, withDeletedObjects: true, "company-alpha"));

        Assert.DoesNotContain("IsDeleted", rendered.ToJson(), StringComparison.Ordinal);
    }
}
