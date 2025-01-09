using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

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
        public string VacanyId { get; set; }
        /// <summary>
        /// ActivityType : InProgress,Shorlisted,OnHold,Rejected, 
        /// </summary>
        public int ActivityType { get; set; }
        /// <summary>
        /// Status: Inactive,Active,Closed
        /// </summary>
        public int Status { get; set; } = 0;
        public decimal Exprience { get; set; }
        public string ResumeUrl { get; set; }
    }
}

