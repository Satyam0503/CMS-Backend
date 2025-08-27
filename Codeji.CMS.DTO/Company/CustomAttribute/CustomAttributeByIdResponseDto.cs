namespace Codeji.CMS.DTO.Company.CustomAttribute;

public class CustomAttributeByIdResponseDto
{
    public string CustomAttributeId { get; set; }
    public Dictionary<string, string>? CustomAttributeTitle { get; set; }
    public List<CustomAttributeValueResponseDto> CustomAttributeValues { get; set; } = [];
}


public class CustomAttributeValueResponseDto
{
    public string CustomAttributeValueId { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Titles { get; set; }
}