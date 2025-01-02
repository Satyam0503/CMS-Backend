using Codeji.CMS.Domain.Models;

namespace Codeji.CMS.GenericRepository.Interfaces
{
    public interface ISupportFiltering
    {
        IEnumerable<FilterInfo> filters { get; set; }
    }
}
