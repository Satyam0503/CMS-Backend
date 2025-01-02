namespace Codeji.CMS.Domain.Models
{
    public class PagingResult<TEntity>
    {
        public IEnumerable<TEntity> data { get; set; }
        public int totalItems { get; set; }
        public int showingRecordsFrom { get; set; }
        public int showingRecordsTo { get; set; }
    }
}
