
namespace Codeji.CMS.DTO.RequestModels.Company
{
    public class DepartmentDTO
    {
        public string? DepartmentId { get; set; }
        public bool IsActive {get;set;}
        public List<MultilingualModel> Titles {get;set;}

    }
}
