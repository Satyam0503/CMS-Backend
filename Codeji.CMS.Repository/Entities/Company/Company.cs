using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company
{
    public class Company
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CompanyId { get; set; }
        public bool Status { get; set; }
        public required string CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string PrimaryContact { get; set; }
        public required string DefaultLanguage { get; set; }
        public List<string> ApplicationLanguage { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}

