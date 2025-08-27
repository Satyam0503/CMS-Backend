using Codeji.CMS.DTO;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company;

public class CustomAttribute : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string CustomAttributeId { get; set; }
    public List<MultilingualModel> CustomAttributeTitle { get; set; } = [];
}
