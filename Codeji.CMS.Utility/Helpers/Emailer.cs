
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
namespace Codeji.CMS.Utility.Helpers
{

    public class Emailer
    {
        public static async Task<(bool isSent, string log)> SendMail(string to, string subject, string body, string[] cc = null, string[] bcc = null, List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            try
            {
                cc = SanitizeEmails(cc);
                bcc = SanitizeEmails(bcc);

                // Validate email addresses 
                if (!string.IsNullOrEmpty(to) && !Regex.IsMatch(to, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return (false, "Invalid email address format.");
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
            var smtpConfigured = !string.IsNullOrWhiteSpace(ConfigManager.EmailSettings.Host)
                && !string.IsNullOrWhiteSpace(ConfigManager.EmailSettings.SecretKey);

            if (!smtpConfigured)
            {
                await SendViaSendGridAsync(to, subject, body, cc, bcc, attachments);
            }
            else
            {
                await SendViaSmtpAsync(to, subject, body, cc, bcc, attachments);
            }
        }

        private static async Task SendViaSmtpAsync(
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

        private static async Task SendViaSendGridAsync(
        string to,
        string subject,
        string body,
        string[] cc = null,
        string[] bcc = null,
        List<(string FileName, byte[] FileContent, string ContentType)> attachments = null)
        {
            var client = new SendGridClient(ConfigManager.EmailSettings.SendGridApiKey);
            var from = new EmailAddress(ConfigManager.EmailSettings.FromEmail, ConfigManager.EmailSettings.FromName);
            var to_ = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, to_, subject, null, body);

            if (cc != null)
            {
                msg.AddCcs(cc.Select(e => new EmailAddress(e)).ToList());
            }

            if (bcc != null)
            {
                msg.AddBccs(bcc.Select(e => new EmailAddress(e)).ToList());
            }

            if (attachments != null)
            {
                foreach (var (fileName, content, contentType) in attachments)
                {
                    await msg.AddAttachmentAsync(fileName, new MemoryStream(content), contentType);
                }
            }

            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid request failed with status {(int)response.StatusCode}: {responseBody}");
            }
        }

        private static string[]? SanitizeEmails(IEnumerable<string>? emails)
        {
            if (emails == null) return null;
            var clean = emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim())
                .Where(email => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return clean.Length == 0 ? null : clean;
        }
    }
}
