namespace Codeji.CMS.DTO.Recruitments
{
    public class ProcessLogResponseModel
    {
        public string ApplicantName { get; set; }
        public string JobRole { get; set; }
        public DateOnly CommentedOn { get; set; }
        public string Comment { get; set; }
        public string CommentedBy { get; set; }

    }
}
