using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities {
    public class Skills {
         [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string  Id {get;set;}
        public required string Name {get; set;}
    }
}