using System.Globalization;
using System.Text.RegularExpressions;
using Codeji.CMS.DTO.PayRoll;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace Codeji.CMS.Services.PayRoll;

public static class EmployeeSalaryPdfImportParser
{
    private static readonly string[] RequiredNumericLabels = ["Basic Pay", "HRA", "LTA", "Other Allowance", "Bonus", "Health Insurance", "Total Deductions"];
    public static EmployeeSalaryPdfImportParseResult Parse(Stream pdf, int rowNumber = 1)
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
        var effectiveFromText = MatchText(text, "Effective From", "Joining Date", "Start Date");
        if (string.IsNullOrWhiteSpace(employeeId)) result.Errors.Add("Employee Id is missing from the PDF.");
        if (!DateTime.TryParse(effectiveFromText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var effectiveFrom))
            result.Errors.Add("Effective From or Joining Date is missing or invalid.");

        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in RequiredNumericLabels)
        {
            var valueText = MatchText(text, label);
            if (!TryMoney(valueText, out var value)) result.Errors.Add($"{label} is missing or invalid.");
            else values[label] = value;
        }

        if (result.Errors.Count > 0) return result;
        var components = new List<EmployeeSalaryComponentValueDto>
        {
            Component("BASIC", "Basic Pay", values["Basic Pay"], "EARNING"),
            Component("HRA", "HRA", values["HRA"], "EARNING"),
            Component("LTA", "LTA", values["LTA"], "EARNING"),
            Component("OTHER_ALLOWANCE", "Other Allowance", values["Other Allowance"], "EARNING"),
            Component("BONUS", "Bonus", values["Bonus"], "EARNING"),
            Component("HEALTH_INSURANCE", "Health Insurance", values["Health Insurance"], "DEDUCTION"),
            Component("TOTAL_DEDUCTIONS", "Total Deductions", values["Total Deductions"], "DEDUCTION")
        };
        var gross = components.Where(x => x.Kind == "EARNING").Sum(x => x.Value);
        result.Row = new EmployeeSalaryImportRowDto
        {
            RowNumber = rowNumber, EmployeeId = employeeId.Trim(), EffectiveFrom = effectiveFrom.Date,
            Currency = "INR", PaymentFrequency = "MONTHLY", MonthlyGross = gross, AnnualCtc = gross * 12, Components = components
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
        foreach (var label in labels)
        {
            var match = Regex.Match(text, $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]?\s*(?<value>.+?)\s*$");
            if (match.Success) return match.Groups["value"].Value.Trim();
        }
        return string.Empty;
    }

    private static bool TryMoney(string value, out decimal result) =>
        decimal.TryParse(value.Replace("₹", string.Empty).Replace(",", string.Empty).Trim(), NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result) && result >= 0;
}