using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company
{
    public class Company
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CompanyId { get; set; }
        public bool Status { get; set; }
        public string CompanyName { get; set; }
        public string CompanyLogo { get; set; }
        public string PrimaryContact { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}

