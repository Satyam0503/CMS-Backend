namespace Codeji.CMS.DTO.Company;

public sealed class PublicCareerCompanyDto
{
    public required string CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required string CareerSlug { get; init; }
    public required string PublicCompanyCode { get; init; }
    public string? CompanyLogo { get; init; }
}
