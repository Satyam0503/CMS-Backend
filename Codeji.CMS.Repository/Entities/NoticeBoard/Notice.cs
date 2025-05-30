using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Repository.Entities.NoticeBoard;

public class Notice : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string NoticeId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Target { get; set; }
    public string Departments { get; set; }
    public EnumsHelper.NoticeType NoticeType { get; set; }
}