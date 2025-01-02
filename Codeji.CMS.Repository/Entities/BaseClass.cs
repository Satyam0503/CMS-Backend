using Codeji.CMS.GenericRepository.Interfaces;
using MongoDB.Bson.Serialization;

namespace Codeji.CMS.Repository.Entities;
public class BaseClass : ISupportAuditing,ISupportSoftDelete
{

    public string CompanyId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}
public class UniqueIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
    {
        return Guid.NewGuid().ToString();
    }
    public bool IsEmpty(object id)
    {
        return id == null || String.IsNullOrEmpty(id.ToString());
    }
}
