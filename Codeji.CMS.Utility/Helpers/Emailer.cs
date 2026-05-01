
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MimeKit;
namespace Codeji.CMS.Utility.Helpers
{

    public class Emailer
    {
        public static async Task<(bool isSent, string log)> SendMail(string to, string subject, string body, string[] cc = null, string[] bcc = null, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            try
            {
                // Validate email addresses 
                if (!string.IsNullOrEmpty(to) && !Regex.IsMatch(to, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return (false, "Invalid email address format.");
                }
                if (cc != null)
                {
                    foreach (var email in cc)
                    {
                        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            cc = null;
                            continue;
                        }
                    }
                }
                if (bcc != null)
                {
                    foreach (var email in bcc)
                    {
                        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            bcc = null;
                            continue;
                        }
                    }
                }
                await SendEmailAsync(to, subject, body, cc, bcc, attachments);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString());
            }
        }
        public static async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string[] cc = null,
        string[] bcc = null,
        List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(ConfigManager.EmailSettings.FromName, ConfigManager.EmailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(to));

            if (cc != null)
            {
                foreach (var ccEmail in cc)
                    email.Cc.Add(MailboxAddress.Parse(ccEmail));
            }

            if (bcc != null)
            {
                foreach (var bccEmail in bcc)
                    email.Bcc.Add(MailboxAddress.Parse(bccEmail));
            }

            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };

            if (attachments != null)
            {
                foreach (var (fileName, content, contentType) in attachments)
                {
                    builder.Attachments.Add(fileName, content, ContentType.Parse(contentType));
                }
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(ConfigManager.EmailSettings.Host, ConfigManager.EmailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(ConfigManager.EmailSettings.FromEmail, ConfigManager.EmailSettings.SecretKey);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
