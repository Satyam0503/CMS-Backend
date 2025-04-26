namespace Codeji.CMS.DTO.ResponseModel;
public class EmployeeSkillsDTO
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public List<SkillsDTO> Skills { get; set; }
}
public class SkillsDTO
{
    public string Id { get; set; }
    public required string Name { get; set; }
}