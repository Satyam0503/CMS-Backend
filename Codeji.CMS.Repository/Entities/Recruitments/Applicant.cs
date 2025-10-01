using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class Applicant : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string ApplicantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public required string Phone { get; set; }
        public string VacancyId { get; set; }
        /// <summary>
        /// ActivityType : InProgress,Shorlisted,OnHold,Rejected, 
        /// </summary>
        public ActivityType ActivityType { get; set; }
        public ActivityStatus Status { get; set; }
        public string State { get; set; }
        public decimal Experience { get; set; }
        public string ResumeUrl { get; set; }
    }
}

