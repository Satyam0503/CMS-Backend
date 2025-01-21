using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class JobVacancy : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string JobId { get; set; }
        public string Title { get; set; }
        public int Vacancies { get; set; }
        public string JobType { get; set; }
        public bool Status { get; set; }
        public string Description { get; set; }
    }
}
