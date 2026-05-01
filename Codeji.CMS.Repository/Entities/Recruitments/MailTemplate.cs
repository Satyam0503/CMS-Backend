
using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class MailTemplate
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string _id { get; set; }
        public EnumsHelper.MailType mailType { get; set; }
        public string subject { get; set; }
        public string body { get; set; }

    }
}
