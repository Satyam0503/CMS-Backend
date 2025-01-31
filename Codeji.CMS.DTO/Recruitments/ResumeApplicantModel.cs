using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.DTO.Recruitments
{
    public class ResumeApplicantModel
    {
        public ApplicantRegisterModel Applicant { get; set; }
        public IFormFile File { get; set; }
    }
}
