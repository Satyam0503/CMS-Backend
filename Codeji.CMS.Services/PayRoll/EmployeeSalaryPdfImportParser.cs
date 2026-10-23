using System.Globalization;
using System.Text.RegularExpressions;
using Codeji.CMS.DTO.PayRoll;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace Codeji.CMS.Services.PayRoll;

public static class EmployeeSalaryPdfImportParser
{
    private sealed record ComponentDef(string CanonicalName, string Code, string Kind, string[] Labels, bool IsRequired);

    private static readonly ComponentDef[] ComponentDefinitions =
    [
        new("Basic Pay", "BASIC", "EARNING", ["Basic Pay", "Basic Salary", "Basic"], true),
        new("HRA", "HRA", "EARNING", ["HRA", "House Rent Allowance"], false),
        new("LTA", "LTA", "EARNING", ["LTA", "Leave Travel Allowance"], false),
        new("Other Allowance", "OTHER_ALLOWANCE", "EARNING", ["Other Allowance", "Other Allowances", "Special Allowance", "Special Allowances"], false),
        new("Bonus", "BONUS", "EARNING", ["Bonus", "Performance Bonus", "Statutory Bonus"], false),
        new("Health Insurance", "HEALTH_INSURANCE", "DEDUCTION", ["Health Insurance", "Medical Insurance", "Mediclaim"], false)
    ];

    private static readonly string[] MetadataLabels =
    [
        "Annual CTC (INR)", "Annual CTC", "Cost to Company", "Total CTC", "CTC",
        "Total Deductions", "Total Deduction",
        "Employee Id", "Employee ID", "Employee Code", "Emp ID", "Emp Id",
        "Effective From", "Joining Date", "Date of Joining", "Start Date"
    ];

    private static readonly string[] AllLabels = ComponentDefinitions
        .SelectMany(c => c.Labels)
        .Concat(MetadataLabels)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(label => label.Length)
        .ToArray();

    public static EmployeeSalaryPdfImportParseResult Parse(Stream pdf, int rowNumber = 1, string? employeeIdFallback = null, DateTime? effectiveFromFallback = null)
    {
        var result = new EmployeeSalaryPdfImportParseResult();
        if (pdf is null || !pdf.CanRead || (pdf.CanSeek && pdf.Length == 0)) { result.Errors.Add("A readable PDF is required."); return result; }

        string text;
        try
        {
            using var document = PdfDocument.Open(pdf);
            text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or PdfDocumentFormatException or IOException)
        {
            result.Errors.Add("The PDF could not be read. Upload a text-based salary document.");
            return result;
        }

        var employeeId = MatchText(text, "Employee Id", "Employee ID", "Employee Code", "Emp ID", "Emp Id");
        if (string.IsNullOrWhiteSpace(employeeId)) employeeId = employeeIdFallback;
        if (string.IsNullOrWhiteSpace(employeeId)) result.Errors.Add("Employee Id is missing from the PDF and onboarding context.");

        var effectiveFromText = MatchText(text, "Effective From", "Joining Date", "Date of Joining", "Start Date");
        DateTime effectiveFrom;
        if (TryParseDate(effectiveFromText, out var parsedEffectiveFrom))
            effectiveFrom = parsedEffectiveFrom.Date;
        else if (string.IsNullOrWhiteSpace(effectiveFromText) && effectiveFromFallback.HasValue)
            effectiveFrom = effectiveFromFallback.Value.Date;
        else
        {
            effectiveFrom = default;
            result.Errors.Add("Effective From or Joining Date is missing or invalid.");
        }

        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in ComponentDefinitions)
        {
            var valueText = MatchText(text, def.Labels);
            if (string.IsNullOrWhiteSpace(valueText))
            {
                if (def.IsRequired)
                    result.Errors.Add($"{def.CanonicalName} is missing or invalid.");
                else
                    values[def.CanonicalName] = 0m;
            }
            else
            {
                if (!TryMoney(valueText, out var value))
                    result.Errors.Add($"{def.CanonicalName} is invalid.");
                else
                    values[def.CanonicalName] = value;
            }
        }

        var totalText = MatchText(text, "Total Deductions", "Total Deduction");
        if (!string.IsNullOrWhiteSpace(totalText))
        {
            if (!TryMoney(totalText, out var total))
                result.Errors.Add("Total Deductions is invalid.");
            else if (values.TryGetValue("Health Insurance", out var itemizedTotal) && total != itemizedTotal)
                result.Errors.Add("Total Deductions does not match the supported itemized deductions. Review and itemize the difference before importing.");
        }

        decimal? annualCtc = null;
        var annualCtcText = MatchText(text, "Annual CTC (INR)", "Annual CTC", "Cost to Company", "Total CTC", "CTC");
        if (!string.IsNullOrWhiteSpace(annualCtcText))
        {
            if (!TryMoney(annualCtcText, out var parsedAnnualCtc)) result.Errors.Add("Annual CTC is invalid.");
            else annualCtc = parsedAnnualCtc;
        }

        if (result.Errors.Count > 0) return result;

        var components = ComponentDefinitions.Select(def =>
            Component(def.Code, def.CanonicalName, values[def.CanonicalName], def.Kind)).ToList();

        var gross = components.Where(x => x.Kind == "EARNING").Sum(x => x.Value);
        result.Row = new EmployeeSalaryImportRowDto
        {
            RowNumber = rowNumber,
            EmployeeId = employeeId.Trim(),
            EffectiveFrom = effectiveFrom.Date,
            Currency = "INR",
            PaymentFrequency = "MONTHLY",
            MonthlyGross = gross,
            AnnualCtc = annualCtc ?? gross * 12,
            AnnualCtcSource = annualCtc.HasValue ? "OFFER_PDF" : "ANNUALIZED_GROSS_ESTIMATE",
            Components = components
        };
        return result;
    }

    private static EmployeeSalaryComponentValueDto Component(string code, string name, decimal value, string kind) => new()
    {
        ComponentCode = code, ComponentName = name, Value = value, Kind = kind, CalculationType = "FIXED",
        IsTaxable = kind == "EARNING", IsRecurring = true, IsStatutory = false, IsEmployerContribution = false
    };

    private static string MatchText(string text, params string[] labels)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        var requestedLabels = string.Join("|", labels.Select(Regex.Escape));
        var knownLabels = string.Join("|", AllLabels.Select(Regex.Escape));
        var match = Regex.Match(
            normalized,
            $@"(?:{requestedLabels})\s*[:\-]?\s*(?<value>.*?)(?=(?:\s*(?:{knownLabels}))\s*[:\-]|(?:\s*(?:{knownLabels}))\s+(?:₹|Rs\.?|INR|\d)|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static bool TryParseDate(string? text, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string[] formats =
        [
            "yyyy-MM-dd", "yyyy/MM/dd",
            "dd/MM/yyyy", "d/M/yyyy",
            "dd-MM-yyyy", "d-M-yyyy",
            "MM/dd/yyyy", "M/d/yyyy",
            "dd MMM yyyy", "d MMM yyyy",
            "dd MMMM yyyy", "d MMMM yyyy",
            "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
        ];

        if (DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result))
            return true;

        if (DateTime.TryParse(text.Trim(), new CultureInfo("en-IN"), DateTimeStyles.AllowWhiteSpaces, out result))
            return true;

        return DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result);
    }

    private static bool TryMoney(string value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0m;
            return false;
        }

        var cleaned = Regex.Replace(value, @"(?i)\binr\b|rs\.?|[₹?$,\s]", string.Empty);
        if (cleaned.EndsWith("/-", StringComparison.Ordinal) || cleaned.EndsWith("/=", StringComparison.Ordinal))
        {
            cleaned = cleaned[..^2];
        }
        cleaned = cleaned.Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result) && result >= 0;
    }
}
