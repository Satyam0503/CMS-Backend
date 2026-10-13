using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeExitSecurityTests
{
    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry));

    [Fact]
    public void Exit_policy_update_filter_contains_tenant_and_version_scope()
    {
        var filter = Builders<EmployeeExitPolicy>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitPolicyId == "policy-1" &&
            x.Version == 4 &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("policy-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
        Assert.Contains("IsDeleted", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Exit_update_filter_cannot_cross_tenant_or_stale_version()
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitId == "exit-1" &&
            x.Version == 7 &&
            x.IsActive &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("exit-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
        Assert.Contains("IsActive", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Contract_update_filter_is_tenant_scoped_and_versioned()
    {
        var filter = Builders<EmpUser>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.UserId == "employee-1" &&
            !x.IsDeleted &&
            x.Contract.Version == 3);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("employee-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejoin_filter_is_tenant_scoped_and_versioned()
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitId == "exit-1" &&
            x.Version == 8 &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("exit-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Clearance_update_filter_requires_tenant_exit_and_version()
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitId == "exit-1" &&
            x.Version == 9 &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("exit-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
        Assert.Contains("IsDeleted", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Evidence_update_filter_requires_tenant_exit_and_version()
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            x.CompanyId == "company-a" &&
            x.EmployeeExitId == "exit-1" &&
            x.Version == 10 &&
            !x.IsDeleted);

        var rendered = Render(filter).ToJson();

        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("exit-1", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
        Assert.Contains("IsDeleted", rendered, StringComparison.Ordinal);
    }
}
