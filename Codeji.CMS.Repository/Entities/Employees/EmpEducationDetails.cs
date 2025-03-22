using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class EmpEducationDetails
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string EducationId { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string EducationTitle { get; set; }
        public string CollegeName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Type { get; set; }
    }
}
