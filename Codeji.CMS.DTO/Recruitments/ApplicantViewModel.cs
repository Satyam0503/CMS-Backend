

using Codeji.CMS.Utility;

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
        public int Status { get; set; }
        public string ActivityTypeName { get; set; }
        public int ActivityType { get; set; }

        public string VacancyName { get; set; }
        public string VacancyId { get; set; }

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

