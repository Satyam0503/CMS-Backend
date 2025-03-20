namespace Codeji.CMS.DTO.RequestModels
{
    public class CommentRequestModel
    {

        public string ApplicantId { get; set; }
        public string UserId { get; set; }
        public string Description { get; set; }
        public int ActivityCategory { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Username { get; set; }
        public string JobTitle { get; set; }


    }
}
