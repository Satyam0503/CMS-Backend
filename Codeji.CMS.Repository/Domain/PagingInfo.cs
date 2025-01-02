namespace Codeji.CMS.Domain.Models
{
    public class PagingInfo
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public FilterInfo filters { get; set; }
        public bool isTipLibrary { get; set; }
    }
}
