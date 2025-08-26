using Codeji.CMS.DTO;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company;

public class CustomAttributeTitle : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string CustomAttributeTitleId { get; set; }
    public required string CustomAttributeId { get; set; }
    public bool IsActive { get; set; }
    public List<MultilingualModel> Titles { get; set; } = [];
}
