using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class CertificationDetails : BaseClass

    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CertificationDetailId { get; set; }
        public string UserId { get; set; }
        public string CertificationTitle { get; set; }
        public string OrganisationName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Mode { get; set; }
    }

}
