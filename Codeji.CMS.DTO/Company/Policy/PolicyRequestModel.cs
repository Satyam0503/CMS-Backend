namespace Codeji.CMS.DTO.Company.Policy;

public class CreatePolicyRequestModel
{
    public required string PolicyName { get; set; }
    public string Description { get; set; }
    public List<string> Departments { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public class UpdatePolicyRequestModel : CreatePolicyRequestModel
{
    public required string PolicyId { get; set; }
}