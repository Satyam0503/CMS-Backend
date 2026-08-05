using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Utility;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.CareerPortal;

public sealed class PublicCompanyProfileService : IPublicCompanyProfileService
{
    private readonly IMongoCollection<PublicCompanyProfile> _profiles;
    private readonly IMongoCollection<Company> _companies;

    public PublicCompanyProfileService(
        IMongoDbRepository<PublicCompanyProfile> profileRepository,
        IMongoDbRepository<Company> companyRepository)
    {
        _profiles = profileRepository.GetCollection();
        _companies = companyRepository.GetCollection();
    }

    public async Task<Result<PublicCompanyProfileDto>> GetPublishedProfiles()
    {
        var companies = await _companies.Find(c => c.Status && !c.IsDeleted).ToListAsync();
        var resolvedCompanies = new List<Company>();
        foreach (var company in companies)
        {
            var resolved = await EnsureCompanyPortalAsync(company);
            if (!string.IsNullOrWhiteSpace(resolved.PublicCompanyCode)) resolvedCompanies.Add(resolved);
        }

        var companyIds = resolvedCompanies.Select(c => c.CompanyId).ToList();
        var profiles = companyIds.Count == 0
            ? []
            : await _profiles.Find(p => companyIds.Contains(p.CompanyId) && !p.IsDeleted && p.IsPublished).ToListAsync();
        var profileMap = profiles.ToDictionary(p => p.CompanyId);

        return new Result<PublicCompanyProfileDto>
        {
            MethodResults = resolvedCompanies
                .Select(c => Map(profileMap.GetValueOrDefault(c.CompanyId), c))
                .OrderBy(p => p.DisplayName)
                .ToList(),
            TotalRecords = resolvedCompanies.Count
        };
    }

    public async Task<Result<PublicCompanyProfileDto>> GetPublishedProfile(string companyCode)
    {
        var code = NormalizeCode(companyCode);
        if (!Regex.IsMatch(code, @"^\d{6}$")) return NotFound();

        // Keep this predicate Mongo-translatable. NormalizeCode is a .NET helper and
        // cannot be evaluated by MongoDB's LINQ provider inside a Find expression.
        var company = await _companies.Find(c => c.Status && !c.IsDeleted && c.PublicCompanyCode == code)
            .FirstOrDefaultAsync();
        if (company is null) return NotFound();

        company = await EnsureCompanyPortalAsync(company);
        var profile = await _profiles.Find(p => p.CompanyId == company.CompanyId && !p.IsDeleted && p.IsPublished)
            .FirstOrDefaultAsync();
        return new Result<PublicCompanyProfileDto> { MethodResult = Map(profile, company), TotalRecords = 1 };
    }

    public async Task<Result<PublicCompanyProfileDto>> GetCompanyProfile(string companyId)
    {
        var company = await _companies.Find(c => c.CompanyId == companyId && !c.IsDeleted).FirstOrDefaultAsync();
        if (company is null) return NotFound();
        var profile = await _profiles.Find(p => p.CompanyId == companyId && !p.IsDeleted).FirstOrDefaultAsync();
        return new Result<PublicCompanyProfileDto> { MethodResult = Map(profile, company), TotalRecords = 1 };
    }

    public async Task<Result<PublicCompanyProfileDto>> SaveCompanyProfile(
        string companyId,
        string userId,
        UpsertPublicCompanyProfileRequest request)
    {
        var company = await _companies.Find(c => c.CompanyId == companyId && !c.IsDeleted).FirstOrDefaultAsync();
        if (company is null) return NotFound();

        var lengthError = ValidateLengths(request);
        if (lengthError is not null)
            return new Result<PublicCompanyProfileDto>
            {
                Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = lengthError
            };
        var urlError = ValidateUrls(request);
        if (urlError is not null)
        {
            return new Result<PublicCompanyProfileDto>
            {
                Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = urlError
            };
        }

        var now = DateTime.UtcNow;
        var existing = await _profiles.Find(p => p.CompanyId == companyId && !p.IsDeleted).FirstOrDefaultAsync();
        var profile = existing ?? new PublicCompanyProfile
        {
            CompanyId = companyId,
            PublicCompanyCode = company.PublicCompanyCode,
            CreatedBy = userId,
            CreatedDate = now
        };

        profile.PublicCompanyCode = company.PublicCompanyCode;
        // Company Details owns the canonical company name. Keep public profiles in
        // sync with it instead of accepting an alternate candidate-facing name.
        profile.DisplayName = company.CompanyName;
        profile.ShortDescription = CleanText(request.ShortDescription);
        profile.AboutCompanyHtml = CleanHtml(request.AboutCompanyHtml);
        // The company logo is managed by the authenticated Company Details upload.
        // Never persist a public-profile override: this also prevents a generated
        // local static-file URL from being submitted back as an external URL.
        profile.LogoUrl = null;
        profile.CoverImageUrl = CleanText(request.CoverImageUrl);
        profile.WebsiteUrl = CleanText(request.WebsiteUrl);
        profile.Industry = CleanText(request.Industry);
        profile.CompanyType = CleanText(request.CompanyType);
        profile.CompanySize = CleanText(request.CompanySize);
        profile.FoundedYear = request.FoundedYear;
        profile.Headquarters = CleanText(request.Headquarters);
        profile.OfficeLocations = CleanList(request.OfficeLocations);
        profile.Mission = CleanText(request.Mission);
        profile.Vision = CleanText(request.Vision);
        profile.Values = CleanList(request.Values);
        profile.Benefits = CleanList(request.Benefits);
        profile.Technologies = CleanList(request.Technologies);

        var socialLinksResult = NormalizeSocialLinks(request.SocialLinks);
        if (!socialLinksResult.Success)
        {
            return new Result<PublicCompanyProfileDto>
            {
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = socialLinksResult.Message
            };
        }

        profile.SocialLinks = socialLinksResult.Data!;
        profile.WorkCultureHtml = CleanHtml(request.WorkCultureHtml);
        profile.HiringProcessHtml = CleanHtml(request.HiringProcessHtml);
        profile.DiversityStatementHtml = CleanHtml(request.DiversityStatementHtml);
        profile.IsPublished = request.IsPublished;
        profile.UpdatedBy = userId;
        profile.UpdatedDate = now;

        if (existing is null) await _profiles.InsertOneAsync(profile);
        else await _profiles.ReplaceOneAsync(p => p.PublicCompanyProfileId == existing.PublicCompanyProfileId, profile);

        return new Result<PublicCompanyProfileDto>
        {
            MethodResult = Map(profile, company), TotalRecords = 1,
            Message = profile.IsPublished ? "Career profile published." : "Career profile saved as draft."
        };
    }

    private static string? ValidateUrls(UpsertPublicCompanyProfileRequest request)
    {
        // Only validate absolute external URLs. Allow relative resource keys for logos/cover images.
        var socialValues = request.SocialLinks != null ? request.SocialLinks.Values.AsEnumerable() : Enumerable.Empty<string>();
        var urls = new[] { request.CoverImageUrl, request.WebsiteUrl }
            .Concat(socialValues);

        var invalidAbsolute = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Any(url => Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                        // allow http or https schemes for external links
                        ((parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) || parsed.IsLoopback || !string.IsNullOrEmpty(parsed.UserInfo)));

        return invalidAbsolute ? "External profile URLs must be valid HTTP or HTTPS absolute URLs." : null;
    }

    private static string? ValidateLengths(UpsertPublicCompanyProfileRequest request)
    {
        var shortValues = new[]
        {
            request.DisplayName, request.Industry, request.CompanyType, request.CompanySize,
            request.Headquarters, request.WebsiteUrl, request.LogoUrl, request.CoverImageUrl
        };
        if (shortValues.Any(x => (x?.Length ?? 0) > 500)) return "A profile field exceeds the 500 character limit.";
        var richValues = new[]
        {
            request.ShortDescription, request.AboutCompanyHtml, request.Mission, request.Vision,
            request.WorkCultureHtml, request.HiringProcessHtml, request.DiversityStatementHtml
        };
        if (richValues.Any(x => (x?.Length ?? 0) > 50_000)) return "Profile rich content exceeds the 50,000 character limit.";
        if (request.OfficeLocations.Count > 100 || request.Values.Count > 100 ||
            request.Benefits.Count > 100 || request.Technologies.Count > 100 ||
            request.SocialLinks.Count > 30) return "Too many profile list items were supplied.";
        return null;
    }

    public static Result<Dictionary<string, string>> NormalizeSocialLinks(IEnumerable<KeyValuePair<string, string>>? links)
    {
        var result = new Result<Dictionary<string, string>>
        {
            Success = true,
            Data = []
        };

        if (links is null) return result;

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in links)
        {
            var key = CleanText(link.Key);
            var value = CleanText(link.Value);
            if (string.IsNullOrWhiteSpace(key))
            {
                result.Success = false;
                result.StatusCode = StatusCodes.Status400BadRequest;
                result.Message = "Social links contain invalid or duplicate keys.";
                return result;
            }

            if (normalized.ContainsKey(key))
            {
                result.Success = false;
                result.StatusCode = StatusCodes.Status400BadRequest;
                result.Message = "Social links contain invalid or duplicate keys.";
                return result;
            }

            normalized[key] = value ?? string.Empty;
        }

        result.Data = normalized;
        return result;
    }

    internal static string NormalizeCode(string? value) => Regex.Replace(value ?? string.Empty, @"\D", string.Empty);

    private async Task<Company> EnsureCompanyPortalAsync(Company company)
    {
        if (!Regex.IsMatch(company.PublicCompanyCode ?? string.Empty, @"^\d{6}$"))
        {
            company.PublicCompanyCode = await GenerateUniquePublicCompanyCodeAsync();
            company.CareerPortalEnabled = true;
            await _companies.UpdateOneAsync(
                c => c.CompanyId == company.CompanyId,
                Builders<Company>.Update
                    .Set(x => x.PublicCompanyCode, company.PublicCompanyCode)
                    .Set(x => x.CareerPortalEnabled, true)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
        }
        else if (!company.CareerPortalEnabled)
        {
            company.CareerPortalEnabled = true;
            await _companies.UpdateOneAsync(
                c => c.CompanyId == company.CompanyId,
                Builders<Company>.Update
                    .Set(x => x.CareerPortalEnabled, true)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
        }

        return company;
    }

    private async Task<string> GenerateUniquePublicCompanyCodeAsync()
    {
        while (true)
        {
            var code = Random.Shared.Next(100000, 1000000).ToString();
            var exists = await _companies.Find(c => c.PublicCompanyCode == code).AnyAsync();
            if (!exists) return code;
        }
    }
    private static string? CleanHtml(string? value) => string.IsNullOrWhiteSpace(value) ? null : Sanitizer.EncodingHtmlText(value.Trim());
    private static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : Sanitizer.EncodingHtmlText(value.Trim());
    private static List<string> CleanList(IEnumerable<string>? values) =>
        values?.Select(CleanText).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct().ToList() ?? [];

    private static PublicCompanyProfileDto Map(PublicCompanyProfile? p, Company c) => new()
    {
        PublicCompanyCode = c.PublicCompanyCode,
        DisplayName = c.CompanyName,
        ShortDescription = p?.ShortDescription,
        AboutCompanyHtml = p?.AboutCompanyHtml,
        // Company logos are persisted as file names and are owned by Company Details.
        // Public profiles always reflect that backend-managed asset.
        LogoUrl = ResolveLogoUrl(c.CompanyLogo),
        CoverImageUrl = p?.CoverImageUrl,
        WebsiteUrl = p?.WebsiteUrl,
        Industry = p?.Industry,
        CompanyType = p?.CompanyType,
        CompanySize = p?.CompanySize,
        FoundedYear = p?.FoundedYear,
        Headquarters = p?.Headquarters,
        OfficeLocations = p?.OfficeLocations ?? [],
        Mission = p?.Mission,
        Vision = p?.Vision,
        Values = p?.Values ?? [],
        Benefits = p?.Benefits ?? [],
        Technologies = p?.Technologies ?? [],
        SocialLinks = p?.SocialLinks ?? [],
        WorkCultureHtml = p?.WorkCultureHtml,
        HiringProcessHtml = p?.HiringProcessHtml,
        DiversityStatementHtml = p?.DiversityStatementHtml,
        IsPublished = p?.IsPublished ?? false
    };

    private static string? ResolveLogoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var logo = value.Trim();

        if (Uri.TryCreate(logo, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return logo;

        // Only turn a plain upload key into a static URL. This avoids treating a
        // user-supplied relative path as a server-side file location.
        return logo.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\']) < 0
            ? Common.GetCompanyLogoUrl(logo)
            : null;
    }

    private static Result<PublicCompanyProfileDto> NotFound() => new()
    {
        Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Career company profile not found."
    };
}
