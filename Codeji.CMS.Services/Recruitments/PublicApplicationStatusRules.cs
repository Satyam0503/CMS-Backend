using System.Text.RegularExpressions;
using Codeji.CMS.Repository.Entities.Recruitments;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public static class PublicApplicationStatusRules
{
    public static FilterDefinition<Applicant> BuildApplicantMatchFilter(string applicantId, string email)
    {
        return Builders<Applicant>.Filter.Eq(a => a.ApplicantId, applicantId)
            & Builders<Applicant>.Filter.Ne(a => a.ApplicationSubmissionIdempotencyKey, null)
            & Builders<Applicant>.Filter.Eq(a => a.IsDeleted, false)
            & Builders<Applicant>.Filter.Ne(a => a.ConsentStatus, "Anonymized")
            & Builders<Applicant>.Filter.Regex(a => a.Email, new BsonRegularExpression($"^{Regex.Escape(email)}$", "i"));
    }

    public static string MapStage(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage)) return "In review";
        string normalized = new(stage.Where(char.IsLetterOrDigit).ToArray());
        normalized = normalized.ToLowerInvariant();

        // Offer outcomes must be evaluated before general declined/rejected labels:
        // candidates who decline an offer are not the same as candidates rejected by HR.
        if (normalized is "offerdeclined" or "declinedoffer") return "Offer declined";
        if (normalized.StartsWith("offer", StringComparison.Ordinal) || normalized is "selected" or "hiringoffer") return "Offer stage";
        if (normalized is "hired" or "preboarding" or "employeecreated") return "Hired";
        if (normalized is "withdrawn" or "applicationwithdrawn") return "Application withdrawn";
        if (normalized is "rejected" or "notselected" or "nohire" or "declined") return "Not selected";
        if (normalized.Contains("interview", StringComparison.Ordinal) || normalized.Contains("assessment", StringComparison.Ordinal)) return "Interview process";
        return "In review";
    }
}
