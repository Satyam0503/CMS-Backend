using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Company.CustomAttribute;

public class CustomAttributeRequestDto
{
    [Required]
    public string CustomAttributeId { get; set; }
    public List<MultilingualModel> CustomAttributeTitle { get; set; }
    public List<CustomAttributeValueRequestDto> CustomAttributeValues { get; set; }
}

public class CustomAttributeValueRequestDto
{
    public string? CustomAttributeValueId { get; set; }
    public bool IsActive { get; set; }
    public List<MultilingualModel> Titles { get; set; }
}