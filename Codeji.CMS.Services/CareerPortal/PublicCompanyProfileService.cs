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
        var companies = await _companies.Find(c =>
            c.Status && !c.IsDeleted && c.CareerPortalEnabled &&
            !string.IsNullOrEmpty(c.PublicCompanyCode)).ToListAsync();
        var companyIds = companies.Select(c => c.CompanyId).ToList();
        var profiles = companyIds.Count == 0
            ? []
            : await _profiles.Find(p => companyIds.Contains(p.CompanyId) && !p.IsDeleted && p.IsPublished).ToListAsync();
        var profileMap = profiles.ToDictionary(p => p.CompanyId);

        return new Result<PublicCompanyProfileDto>
        {
            MethodResults = companies
                .Select(c => Map(profileMap.GetValueOrDefault(c.CompanyId), c))
                .OrderBy(p => p.DisplayName)
                .ToList(),
            TotalRecords = companies.Count
        };
    }

    public async Task<Result<PublicCompanyProfileDto>> GetPublishedProfile(string companyCode)
    {
        var code = NormalizeCode(companyCode);
        var company = await _companies.Find(c =>
            c.Status && !c.IsDeleted && c.CareerPortalEnabled && c.PublicCompanyCode == code).FirstOrDefaultAsync();
        if (company is null) return NotFound();

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
        profile.DisplayName = CleanText(request.DisplayName);
        profile.ShortDescription = CleanText(request.ShortDescription);
        profile.AboutCompanyHtml = CleanHtml(request.AboutCompanyHtml);
        profile.LogoUrl = CleanText(request.LogoUrl);
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
        profile.SocialLinks = request.SocialLinks.ToDictionary(x => CleanText(x.Key) ?? "", x => CleanText(x.Value) ?? "");
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
        var urls = new[] { request.LogoUrl, request.CoverImageUrl, request.WebsiteUrl }
            .Concat(request.SocialLinks.Values);
        return urls.Any(url => !string.IsNullOrWhiteSpace(url) &&
            (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
             parsed.Scheme != Uri.UriSchemeHttps || parsed.IsLoopback ||
             !string.IsNullOrEmpty(parsed.UserInfo)))
            ? "External profile URLs must be valid HTTPS URLs."
            : null;
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

    private static string NormalizeCode(string value) => Regex.Replace(value ?? "", @"\D", "");
    private static string? CleanHtml(string? value) => string.IsNullOrWhiteSpace(value) ? null : Sanitizer.EncodingHtmlText(value.Trim());
    private static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : Sanitizer.EncodingHtmlText(value.Trim());
    private static List<string> CleanList(IEnumerable<string>? values) =>
        values?.Select(CleanText).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct().ToList() ?? [];

    private static PublicCompanyProfileDto Map(PublicCompanyProfile? p, Company c) => new()
    {
        PublicCompanyCode = c.PublicCompanyCode,
        DisplayName = p?.DisplayName ?? c.CompanyName,
        ShortDescription = p?.ShortDescription,
        AboutCompanyHtml = p?.AboutCompanyHtml,
        LogoUrl = p?.LogoUrl ?? c.CompanyLogo,
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

    private static Result<PublicCompanyProfileDto> NotFound() => new()
    {
        Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "Career company profile not found."
    };
}
