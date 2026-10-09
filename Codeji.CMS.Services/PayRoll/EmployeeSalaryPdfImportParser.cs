using System.Globalization;
using System.Text.RegularExpressions;
using Codeji.CMS.DTO.PayRoll;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace Codeji.CMS.Services.PayRoll;

public static class EmployeeSalaryPdfImportParser
{
    private static readonly string[] RequiredNumericLabels = ["Basic Pay", "HRA", "LTA", "Other Allowance", "Bonus", "Health Insurance"];
    private static readonly string[] AllLabels = RequiredNumericLabels
        .Concat(["Annual CTC (INR)", "Annual CTC", "Total Deductions", "Employee Id", "Employee ID", "Employee Code", "Effective From", "Joining Date", "Start Date"])
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

        var employeeId = MatchText(text, "Employee Id", "Employee ID", "Employee Code");
        if (string.IsNullOrWhiteSpace(employeeId)) employeeId = employeeIdFallback;
        var effectiveFromText = MatchText(text, "Effective From", "Joining Date", "Start Date");
        if (string.IsNullOrWhiteSpace(employeeId)) result.Errors.Add("Employee Id is missing from the PDF and onboarding context.");
        DateTime effectiveFrom;
        if (DateTime.TryParse(effectiveFromText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedEffectiveFrom))
            effectiveFrom = parsedEffectiveFrom.Date;
        else if (string.IsNullOrWhiteSpace(effectiveFromText) && effectiveFromFallback.HasValue)
            effectiveFrom = effectiveFromFallback.Value.Date;
        else
        {
            effectiveFrom = default;
            result.Errors.Add("Effective From or Joining Date is missing or invalid.");
        }

        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in RequiredNumericLabels)
        {
            var valueText = MatchText(text, label);
            if (!TryMoney(valueText, out var value)) result.Errors.Add($"{label} is missing or invalid.");
            else values[label] = value;
        }

        var totalText = MatchText(text, "Total Deductions");
        if (!string.IsNullOrWhiteSpace(totalText))
        {
            if (!TryMoney(totalText, out var total))
                result.Errors.Add("Total Deductions is invalid.");
            else if (values.TryGetValue("Health Insurance", out var itemizedTotal) && total != itemizedTotal)
                result.Errors.Add("Total Deductions does not match the supported itemized deductions. Review and itemize the difference before importing.");
        }
        decimal? annualCtc = null;
        var annualCtcText = MatchText(text, "Annual CTC (INR)", "Annual CTC");
        if (!string.IsNullOrWhiteSpace(annualCtcText))
        {
            if (!TryMoney(annualCtcText, out var parsedAnnualCtc)) result.Errors.Add("Annual CTC is invalid.");
            else annualCtc = parsedAnnualCtc;
        }
        if (result.Errors.Count > 0) return result;
        var components = new List<EmployeeSalaryComponentValueDto>
        {
            Component("BASIC", "Basic Pay", values["Basic Pay"], "EARNING"),
            Component("HRA", "HRA", values["HRA"], "EARNING"),
            Component("LTA", "LTA", values["LTA"], "EARNING"),
            Component("OTHER_ALLOWANCE", "Other Allowance", values["Other Allowance"], "EARNING"),
            Component("BONUS", "Bonus", values["Bonus"], "EARNING"),
            Component("HEALTH_INSURANCE", "Health Insurance", values["Health Insurance"], "DEDUCTION")
        };
        var gross = components.Where(x => x.Kind == "EARNING").Sum(x => x.Value);
        result.Row = new EmployeeSalaryImportRowDto
        {
            RowNumber = rowNumber, EmployeeId = employeeId.Trim(), EffectiveFrom = effectiveFrom.Date,
            Currency = "INR", PaymentFrequency = "MONTHLY", MonthlyGross = gross, AnnualCtc = annualCtc ?? gross * 12,
            AnnualCtcSource = annualCtc.HasValue ? "OFFER_PDF" : "ANNUALIZED_GROSS_ESTIMATE", Components = components
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
            $@"(?:{requestedLabels})\s*[:\-]?\s*(?<value>.*?)(?=(?:\s*{knownLabels})\s*[:\-]\s*|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static bool TryMoney(string value, out decimal result) =>
        decimal.TryParse(value.Replace("₹", string.Empty).Replace(",", string.Empty).Trim(), NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result) && result >= 0;
}
