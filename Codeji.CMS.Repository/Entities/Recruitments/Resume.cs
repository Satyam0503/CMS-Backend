using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class Resume
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string ResumeId { get; set; }
        public string ApplicantId { get; set; }
        public string CompanyId { get; set; }
        public string FileName { get; set; }
        public string ResumeUrl { get; set; }

        public IFormFile File { get; set; }
    }
}
