using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Utility.Helpers;

/// <summary>
/// Loads deployable email templates from the API's Templates directory.
/// Templates are intentionally versioned with the application rather than read from MongoDB.
/// </summary>
public static class RepositoryEmailTemplate
{
    private static readonly IReadOnlyDictionary<EnumsHelper.MailType, (string FileName, string Subject)> Definitions =
        new Dictionary<EnumsHelper.MailType, (string, string)>
        {
            [EnumsHelper.MailType.EmployeeWelcomeMail] = ("EmployeeWelcomeMail.html", "Welcome to [CompanyName]"),
            [EnumsHelper.MailType.SelectedMail] = ("SelectedMail.html", "Congratulations [CandidateName] - you've been selected"),
            [EnumsHelper.MailType.RejectedMail] = ("RejectedMail.html", "Update on your application for [JobTitle]"),
            [EnumsHelper.MailType.ApplyNowMailToHR] = ("ApplyNowMailToHR.html", "New application received for [JobTitle]"),
            [EnumsHelper.MailType.ApplyNowMailToApplicant] = ("ApplyNowMailToApplicant.html", "We received your application for [JobTitle]"),
            [EnumsHelper.MailType.ResetPassword] = ("ResetPassword.html", "Reset your password"),
            [EnumsHelper.MailType.LeaveMailToHR] = ("LeaveMailToHR.html", "Leave request update"),
            [EnumsHelper.MailType.LeaveReplyMail] = ("LeaveReplyMail.html", "Leave request update")
        };

    public static bool TryGet(EnumsHelper.MailType mailType, out string subject, out string body)
    {
        subject = string.Empty;
        body = string.Empty;
        if (!Definitions.TryGetValue(mailType, out var definition)) return false;

        // AppContext.BaseDirectory is correct after publish; the second path supports local development.
        string[] paths =
        [
            Path.Combine(AppContext.BaseDirectory, "Templates", definition.FileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", definition.FileName)
        ];
        string? path = paths.FirstOrDefault(File.Exists);
        if (path is null) return false;

        subject = definition.Subject;
        body = File.ReadAllText(path);
        return !string.IsNullOrWhiteSpace(body);
    }
}
