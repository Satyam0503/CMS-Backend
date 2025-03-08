using SendGrid;
using SendGrid.Helpers.Mail;

namespace Codeji.CMS.Utility.Helpers
{
    public class EmailFunctionality
    {

        public static async Task SendEmailFromAPI(string to, string subject, string body)
        {

            string SendGridAPIKey = ConfigManager.SENDGRID_API_KEY;
            SendGridClient SendGridClient = new SendGridClient(SendGridAPIKey);
            EmailAddress fromAddress = new EmailAddress(ConfigManager.SendGridEmail, ConfigManager.SendGridSenderName);
            EmailAddress toAddress = new EmailAddress(to);
            SendGridMessage Message = MailHelper.CreateSingleEmail(fromAddress, toAddress, subject, null, body);
            Response response = await SendGridClient.SendEmailAsync(Message).ConfigureAwait(false);

        }
    }
}
