using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class UserSummary
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string SummaryId { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string EmployeeSummary { get; set; }
    }
}
