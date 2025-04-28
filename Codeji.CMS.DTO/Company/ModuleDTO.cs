namespace Codeji.CMS.DTO.RequestModels.Company
{
    public class ModuleDTO
    {
        public string _id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleConstant { get; set; }
        public bool HasAccess{get;set;}
    }
}