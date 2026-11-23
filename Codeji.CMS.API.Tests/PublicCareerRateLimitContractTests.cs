using System.Reflection;
using Codeji.CMS.API.Controllers;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class PublicCareerRateLimitContractTests
{
    [Theory]
    [InlineData(nameof(PublicCareerController.GetJobs))]
    [InlineData(nameof(PublicCareerController.GetJobByPublicId))]
    [InlineData(nameof(PublicCareerController.GetJobBySlug))]
    [InlineData(nameof(PublicCareerController.GetCompanyJobs))]
    [InlineData(nameof(PublicCareerController.GetLocations))]
    public void AnonymousCareerReadEndpoints_UseCareerSearchRateLimit(string actionName)
    {
        var method = typeof(PublicCareerController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var rateLimit = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal("career-search", rateLimit!.PolicyName);
    }

    [Fact]
    public void AnonymousApplicationStatusEndpoint_UsesSensitiveRateLimit()
    {
        var method = typeof(PublicCareerController).GetMethod("GetApplicationStatus", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var rateLimit = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal("career-sensitive", rateLimit!.PolicyName);
    }

    [Fact]
    public void Public_resume_upload_has_bounded_multipart_request_limits()
    {
        var method = typeof(PublicCareerController).GetMethod(nameof(PublicCareerController.UploadResume), BindingFlags.Instance | BindingFlags.Public);
        AssertResumeUploadLimits(method);
    }

    private static void AssertResumeUploadLimits(MethodInfo? method)
    {
        Assert.NotNull(method);
        var requestLimit = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        Assert.NotNull(requestLimit);
        Assert.Equal(6 * 1024 * 1024, ((IRequestSizeLimitMetadata)requestLimit!).MaxRequestBodySize);
        var formLimits = method.GetCustomAttribute<RequestFormLimitsAttribute>();
        Assert.NotNull(formLimits);
        Assert.Equal(6 * 1024 * 1024, formLimits!.MultipartBodyLengthLimit);
        Assert.Equal(8, formLimits.ValueCountLimit);
        Assert.Equal(16, formLimits.MultipartHeadersCountLimit);
        Assert.Equal(8192, formLimits.MultipartHeadersLengthLimit);
    }
}
