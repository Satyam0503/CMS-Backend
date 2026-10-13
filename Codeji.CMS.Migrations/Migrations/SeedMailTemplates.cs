using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class SeedMailTemplates : IMigration
{
    public string Id => $"2024-12-02-{typeof(SeedMailTemplates).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var mailTemplates = db.GetCollection<MailTemplate>(typeof(MailTemplate).Name);

        var existingTypes = (await mailTemplates
            .Find(FilterDefinition<MailTemplate>.Empty)
            .Project(t => t.mailType)
            .ToListAsync()).ToHashSet();

        var defaults = BuildDefaults()
            .Where(t => !existingTypes.Contains(t.mailType))
            .ToList();

        if (defaults.Count > 0)
            await mailTemplates.InsertManyAsync(defaults);

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }

    // Placeholder syntax follows HtmlTemplate.Render — uses [PropertyName] tokens.
    private static IEnumerable<MailTemplate> BuildDefaults()
    {
        string templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");

        string LoadTemplate(string fileName, string fallback)
        {
            var path = Path.Combine(templatesPath, fileName);
            if (File.Exists(path)) return File.ReadAllText(path);
            return fallback;
        }

        return new List<MailTemplate>
        {
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.EmployeeWelcomeMail,
                subject = "Welcome to [CompanyName]",
                body = WrapHtml(LoadTemplate("EmployeeWelcomeMail.html",
                    "<h2>Welcome, [EmployeeName]!</h2><p>Your account at [CompanyName] has been created.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.SelectedMail,
                subject = "Congratulations [CandidateName] — you've been selected",
                body = WrapHtml(LoadTemplate("SelectedMail.html","<p>Hi [CandidateName],</p><p>You've been selected for [JobTitle].</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.RejectedMail,
                subject = "Update on your application for [JobTitle]",
                body = WrapHtml(LoadTemplate("RejectedMail.html","<p>Hi [CandidateName],</p><p>We will not be moving forward at this time.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ApplyNowMailToHR,
                subject = "New application received for [JobTitle]",
                body = WrapHtml(LoadTemplate("ApplyNowMailToHR.html","<p>A new application has been submitted.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ApplyNowMailToApplicant,
                subject = "We received your application for [JobTitle]",
                body = WrapHtml(LoadTemplate("ApplyNowMailToApplicant.html","<p>Hi [CandidateName],</p><p>Thank you for applying.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ContactUsMail,
                subject = "Thanks for contacting [CompanyName]",
                body = WrapHtml(LoadTemplate("ContactUsMail.html","<p>Hi [Name],</p><p>Thanks for reaching out.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ContactUsMailToHR,
                subject = "New contact request from [Name]",
                body = WrapHtml(LoadTemplate("ContactUsMailToHR.html","<p>A new contact request has been received.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.LeaveMailToHR,
                subject = "Leave request from [EmployeeName]",
                body = WrapHtml(LoadTemplate("LeaveMailToHR.html","<p>[EmployeeName] has submitted a leave request.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.LeaveReplyMail,
                subject = "Your leave request has been [Status]",
                body = WrapHtml(LoadTemplate("LeaveReplyMail.html","<p>Your leave request has been [Status].</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ResignationMail,
                subject = "Resignation submitted by [EmployeeName]",
                body = WrapHtml(LoadTemplate("ResignationMail.html","<p>[EmployeeName] has submitted a resignation.</p>"))
            },
            new()
            {
                _id = Guid.NewGuid().ToString(),
                mailType = EnumsHelper.MailType.ResetPassword,
                subject = "Reset your [CompanyName] password",
                body = WrapHtml(LoadTemplate("ResetPassword.html","<p>Reset your password</p>"))
            }
        };
    }

    private static string WrapHtml(string inner) =>
        "<div style=\"font-family:Arial,sans-serif;color:#333;\">" +
        inner +
        "<hr style=\"margin-top:24px;border:none;border-top:1px solid #eee;\" />" +
        "<p style=\"font-size:12px;color:#888;\">&copy; [Year] [CompanyName]</p>" +
        "</div>";
}
