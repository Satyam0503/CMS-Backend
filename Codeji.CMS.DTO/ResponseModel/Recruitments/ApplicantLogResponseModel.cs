
namespace Codeji.CMS.DTO.ResponseModel;

public class ApplicantLogResponseModel
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string ApplicantId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string JobRole { get; set; }
    public int ActivityCategory { get; set; }
    public string ApplicantName { get; set; }
    public string CompanyId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}