
namespace Codeji.CMS.DTO.RequestModels.Company
{
    public class CompanyRequestModel
    {
        public required string CompanyName { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
