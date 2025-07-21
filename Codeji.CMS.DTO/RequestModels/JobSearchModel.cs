
namespace Codeji.CMS.DTO.RequestModels
{
    public class JobRequestModel
    {
        public string Search { get; set; }
        public int PageNo { get; set; }
        public int Records { get; set; }
        public List<int> JobTypes { get; set; }
        public bool? Status { get; set; }
    }
}
