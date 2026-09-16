using System.ComponentModel.DataAnnotations;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.DTO.CareerPortal;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class PublicCareerAnalyticsProtectionTests
{
    [Fact]
    public void AnalyticsWrite_UsesItsDedicatedRateLimitPolicy()
    {
        var method = typeof(PublicCareerEngagementController).GetMethod(nameof(PublicCareerEngagementController.Track))!;
        var policy = Assert.Single(method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>());
        Assert.Equal("career-analytics", policy.PolicyName);
    }

    [Fact]
    public void AnalyticsPayload_HasBoundedPublicFields()
    {
        Assert.Equal(40, MaxLength(nameof(CareerAnalyticsRequest.EventType)));
        Assert.Equal(256, MaxLength(nameof(CareerAnalyticsRequest.VisitorToken)));
        Assert.Equal(64, MaxLength(nameof(CareerAnalyticsRequest.PublicJobId)));
        Assert.Equal(64, MaxLength(nameof(CareerAnalyticsRequest.PublicCompanyCode)));
    }

    private static int MaxLength(string propertyName) =>
        typeof(CareerAnalyticsRequest).GetProperty(propertyName)!
            .GetCustomAttributes(typeof(StringLengthAttribute), inherit: true)
            .Cast<StringLengthAttribute>().Single().MaximumLength;
}
