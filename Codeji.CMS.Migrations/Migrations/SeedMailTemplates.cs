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
    private static IEnumerable<MailTemplate> BuildDefaults() =>
    [
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.EmployeeWelcomeMail,
            subject  = "Welcome to [CompanyName]",
            body     = WrapHtml(
                "<h2>Welcome, [EmployeeName]!</h2>" +
                "<p>Your account at [CompanyName] has been created. " +
                "Please set your password using the link below.</p>" +
                "<p><a href=\"[PasswordCreationLink]\">Create your password</a></p>" +
                "<p>This link will expire in [LinkExpiryTime].</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.SelectedMail,
            subject  = "Congratulations [CandidateName] — you've been selected",
            body     = WrapHtml(
                "<p>Hi [CandidateName],</p>" +
                "<p>We're delighted to inform you that you have been selected for the " +
                "<strong>[JobTitle]</strong> position. Our team will reach out shortly with next steps.</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.RejectedMail,
            subject  = "Update on your application for [JobTitle]",
            body     = WrapHtml(
                "<p>Hi [CandidateName],</p>" +
                "<p>Thank you for applying for the <strong>[JobTitle]</strong> role. " +
                "After careful consideration we have decided not to move forward at this time. " +
                "We wish you the very best in your job search.</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ApplyNowMailToHR,
            subject  = "New application received for [JobTitle]",
            body     = WrapHtml(
                "<p>A new application has been submitted.</p>" +
                "<ul>" +
                "<li><strong>Candidate:</strong> [CandidateName]</li>" +
                "<li><strong>Position:</strong> [JobTitle]</li>" +
                "<li><strong>Email:</strong> [CandidateEmail]</li>" +
                "</ul>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ApplyNowMailToApplicant,
            subject  = "We received your application for [JobTitle]",
            body     = WrapHtml(
                "<p>Hi [CandidateName],</p>" +
                "<p>Thank you for applying for the <strong>[JobTitle]</strong> position. " +
                "Our team will review your application and get back to you soon.</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ContactUsMail,
            subject  = "Thanks for contacting [CompanyName]",
            body     = WrapHtml(
                "<p>Hi [Name],</p>" +
                "<p>Thanks for reaching out. We've received your message and will respond shortly.</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ContactUsMailToHR,
            subject  = "New contact request from [Name]",
            body     = WrapHtml(
                "<p>A new contact request has been received.</p>" +
                "<ul>" +
                "<li><strong>Name:</strong> [Name]</li>" +
                "<li><strong>Email:</strong> [Email]</li>" +
                "<li><strong>Message:</strong> [Message]</li>" +
                "</ul>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.LeaveMailToHR,
            subject  = "Leave request from [EmployeeName]",
            body     = WrapHtml(
                "<p>[EmployeeName] has submitted a leave request.</p>" +
                "<ul>" +
                "<li><strong>Type:</strong> [LeaveType]</li>" +
                "<li><strong>From:</strong> [FromDate]</li>" +
                "<li><strong>To:</strong> [ToDate]</li>" +
                "<li><strong>Reason:</strong> [Reason]</li>" +
                "</ul>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.LeaveReplyMail,
            subject  = "Your leave request has been [Status]",
            body     = WrapHtml(
                "<p>Hi [EmployeeName],</p>" +
                "<p>Your leave request from <strong>[FromDate]</strong> to <strong>[ToDate]</strong> " +
                "has been <strong>[Status]</strong>.</p>" +
                "<p>[Comments]</p>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ResignationMail,
            subject  = "Resignation submitted by [EmployeeName]",
            body     = WrapHtml(
                "<p>[EmployeeName] has submitted a resignation.</p>" +
                "<ul>" +
                "<li><strong>Last working day:</strong> [LastWorkingDay]</li>" +
                "<li><strong>Reason:</strong> [Reason]</li>" +
                "</ul>"),
        },
        new()
        {
            _id      = Guid.NewGuid().ToString(),
            mailType = EnumsHelper.MailType.ResetPassword,
            subject  = "Reset your [CompanyName] password",
            body     = WrapHtml(
                "<h2>Hi [EmployeeName],</h2>" +
                "<p>We received a request to reset your password. " +
                "Click the link below to set a new password.</p>" +
                "<p><a href=\"[PasswordResetLink]\">Reset password</a></p>" +
                "<p>This link will expire in [LinkExpiryTime]. " +
                "If you didn't request a password reset, you can safely ignore this email.</p>"),
        },
    ];

    private static string WrapHtml(string inner) =>
        "<div style=\"font-family:Arial,sans-serif;color:#333;\">" +
        inner +
        "<hr style=\"margin-top:24px;border:none;border-top:1px solid #eee;\" />" +
        "<p style=\"font-size:12px;color:#888;\">&copy; [Year] [CompanyName]</p>" +
        "</div>";
}
