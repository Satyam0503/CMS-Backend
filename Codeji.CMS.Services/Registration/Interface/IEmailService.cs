namespace Codeji.CMS.Services.Registration.Interface
{
    public interface IEmailService
    {
        Task SendPasswordEmail(string email);
    }
}
