namespace Codeji.CMS.Services.Registration;
using System.Net.Mail;
using Codeji.CMS.Services.Registration.Interface;

public class EmailService : IEmailService
{
    //private readonly string _smtpServer;
    //private readonly int _smtpPort;
    //private readonly string _smtpUser;
    //private readonly string _smtpPass;

    //public EmailService(string smtpServer, int smtpPort, string smtpUser, string smtpPass)
    //{
    //    _smtpServer = smtpServer;
    //    _smtpPort = smtpPort;
    //    _smtpUser = smtpUser;
    //    _smtpPass = smtpPass;
    //}

    public async Task SendPasswordEmail(string email)
    {
        string subject = "Welcome to the Company!";
        string body = $"Please set your password by clicking this link: setPasswordLink";

        MailMessage message = new("testcodeji_sumit@yopmail.com", email, subject, body);

        using (SmtpClient smtp = new("smtp.gmail.com"))
        {
            smtp.EnableSsl = true;
            smtp.Credentials = new System.Net.NetworkCredential("abhirawatb2@gmail.com", "");
            await smtp.SendMailAsync(message);
        };

    }
}
