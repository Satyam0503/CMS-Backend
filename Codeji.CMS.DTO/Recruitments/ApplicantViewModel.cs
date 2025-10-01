

using Codeji.CMS.Utility;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Recruitments
{
    public class ApplicantViewModel
    {
        public string ApplicantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string StatusName { get; set; }
        public ActivityStatus Status { get; set; }
        public string ActivityTypeName { get; set; }
        public ActivityType ActivityType { get; set; }

        public string VacancyName { get; set; }
        public string VacancyId { get; set; }
        public string State { get; set; }
        public decimal Experience { get; set; }
        public DateTime? ApplyDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string ResumeUrl { get; set; }
        public string? FullApplicantResumePath
        {
            get
            {
                return !string.IsNullOrEmpty(ResumeUrl) ? Common.GetApplicantResumeFullPath(ResumeUrl) : null;
            }
        }

    }
}

