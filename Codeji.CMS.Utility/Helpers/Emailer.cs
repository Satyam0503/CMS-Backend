
using System.Text.RegularExpressions;
using System.Net;
using MailKit.Net.Smtp;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
namespace Codeji.CMS.Utility.Helpers
{

    public class Emailer
    {
        public static string BuildProfessionalHtmlBody(string? body)
        {
            var content = string.IsNullOrWhiteSpace(body)
                ? "<p>No additional details were provided.</p>"
                : body;

            // Full HTML documents are already responsible for their own outer structure.
            if (Regex.IsMatch(content, @"<html[\s>]", RegexOptions.IgnoreCase)) return content;

            var brandName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(ConfigManager.EmailSettings?.FromName)
                ? "Codeji"
                : ConfigManager.EmailSettings.FromName.Trim());
            var year = DateTime.UtcNow.Year;
            return $"<!doctype html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" /></head><body style=\"margin:0;padding:0;background:#f3f6fb;font-family:Arial,Helvetica,sans-serif;color:#1e293b;\">" +
                   $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background:#f3f6fb;padding:32px 12px;\"><tr><td align=\"center\">" +
                   $"<table role=\"presentation\" width=\"600\" cellspacing=\"0\" cellpadding=\"0\" style=\"width:100%;max-width:600px;background:#ffffff;border:1px solid #dbe3ef;border-radius:14px;overflow:hidden;box-shadow:0 10px 28px rgba(15,23,42,0.08);\">" +
                   $"<tr><td style=\"padding:24px 32px;background:#183b52;color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.1px;\">{brandName}</td></tr>" +
                   $"<tr><td style=\"padding:32px;font-size:15px;line-height:1.6;\">{content}</td></tr>" +
                   $"<tr><td style=\"padding:18px 32px;background:#f8fafc;border-top:1px solid #e2e8f0;color:#64748b;font-size:12px;line-height:1.5;\">This is an automated message from {brandName}. Please do not reply directly to this email.<br />&copy; {year} {brandName}. All rights reserved.</td></tr>" +
                   "</table></td></tr></table></body></html>";
        }

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
                await SendEmailAsync(to, subject, BuildProfessionalHtmlBody(body), cc, bcc, attachments);
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
