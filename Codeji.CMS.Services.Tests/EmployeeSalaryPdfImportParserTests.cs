using Codeji.CMS.Services.PayRoll;
using System.Text;
using UglyToad.PdfPig;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryPdfImportParserTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("Total Deductions: 500", true)]
    [InlineData("Total Deductions: 1500", false)]
    [InlineData("Total Deductions: invalid", false)]
    public void Aggregate_deduction_is_optional_and_reconciles_with_itemized_amounts(string totalLine, bool valid)
    {
        var bytes = CreateTextPdf("Employee Id: EMP-005", "Effective From: 2026-11-01", "Basic Pay: 30000",
            "HRA: 0", "LTA: 0", "Other Allowance: 0", "Bonus: 0", "Health Insurance: 500", totalLine);
        var result = EmployeeSalaryPdfImportParser.Parse(new MemoryStream(bytes));
        Assert.Equal(valid, result.Row is not null);
        if (valid)
            Assert.DoesNotContain(result.Row!.Components, x => x.ComponentCode == "TOTAL_DEDUCTIONS");
        else
            Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_ExtractsTextBasedOfferSalaryIntoPreviewRow()
    {
        var pdfBytes = CreateTextPdf(
            "Employee Id: EMP-001",
            "Effective From: 2026-11-01",
            "Basic Pay: 120000",
            "HRA: 20000",
            "LTA: 0",
            "Other Allowance: 5000",
            "Bonus: 0",
            "Health Insurance: 2000",
            "Total Deductions: 2000",
            "Annual CTC: 1800000");

        using var document = PdfDocument.Open(new MemoryStream(pdfBytes));
        var extractedText = string.Join(" | ", document.GetPages().Select(page => page.Text));
        using var stream = new MemoryStream(pdfBytes);
        var result = EmployeeSalaryPdfImportParser.Parse(stream);

        Assert.True(result.Errors.Count == 0, $"Extracted text: {extractedText}. Parser errors: {string.Join(" | ", result.Errors)}");
        Assert.NotNull(result.Row);
        Assert.Equal("EMP-001", result.Row.EmployeeId);
        Assert.Equal(new DateTime(2026, 11, 1), result.Row.EffectiveFrom);
        Assert.Equal(145000m, result.Row.MonthlyGross);
        Assert.Equal(1800000m, result.Row.AnnualCtc);
        Assert.Equal("OFFER_PDF", result.Row.AnnualCtcSource);
        Assert.Equal(6, result.Row.Components.Count);
        Assert.DoesNotContain(result.Row.Components, component => component.ComponentCode == "TOTAL_DEDUCTIONS");
        Assert.Equal(2000m, result.Row.Components.Where(component => component.Kind == "DEDUCTION").Sum(component => component.Value));
    }

    [Fact]
    public void Parse_RejectsAnInvalidExplicitAnnualCtcInsteadOfSilentlyInferringIt()
    {
        var bytes = CreateTextPdf("Employee Id: EMP-005", "Effective From: 2026-11-01", "Basic Pay: 30000",
            "HRA: 0", "LTA: 0", "Other Allowance: 0", "Bonus: 0", "Health Insurance: 0", "Annual CTC: unknown");
        var result = EmployeeSalaryPdfImportParser.Parse(new MemoryStream(bytes));
        Assert.Null(result.Row);
        Assert.Contains(result.Errors, error => error.Contains("Annual CTC is invalid", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_UsesOnboardingEmployeeIdWhenOfferDoesNotPrintAnEmployeeCode()
    {
        var pdfBytes = CreateTextPdf(
            "Effective From: 2026-11-01",
            "Basic Pay: 120000",
            "HRA: 20000",
            "LTA: 0",
            "Other Allowance: 5000",
            "Bonus: 0",
            "Health Insurance: 2000",
            "Total Deductions: 2000");

        var result = EmployeeSalaryPdfImportParser.Parse(new MemoryStream(pdfBytes), employeeIdFallback: "EMP-002");

        Assert.Empty(result.Errors);
        Assert.Equal("EMP-002", result.Row?.EmployeeId);
        Assert.Equal("ANNUALIZED_GROSS_ESTIMATE", result.Row?.AnnualCtcSource);
    }

    [Fact]
    public void Parse_UsesOnboardingIdentityAndSalaryStartWhenOfferOmitsBoth()
    {
        var pdfBytes = CreateTextPdf(
            "Basic Pay: 120000",
            "HRA: 20000",
            "LTA: 0",
            "Other Allowance: 5000",
            "Bonus: 0",
            "Health Insurance: 2000",
            "Total Deductions: 2000");

        var result = EmployeeSalaryPdfImportParser.Parse(
            new MemoryStream(pdfBytes),
            employeeIdFallback: "EMP-003",
            effectiveFromFallback: new DateTime(2026, 11, 1));

        Assert.Empty(result.Errors);
        Assert.Equal("EMP-003", result.Row?.EmployeeId);
        Assert.Equal(new DateTime(2026, 11, 1), result.Row?.EffectiveFrom);
    }

    [Fact]
    public void Parse_DoesNotReplaceAnInvalidDocumentDateWithTheOnboardingFallback()
    {
        var pdfBytes = CreateTextPdf(
            "Employee Id: EMP-004",
            "Effective From: not-a-date",
            "Basic Pay: 120000",
            "HRA: 20000",
            "LTA: 0",
            "Other Allowance: 5000",
            "Bonus: 0",
            "Health Insurance: 2000",
            "Total Deductions: 2000");

        var result = EmployeeSalaryPdfImportParser.Parse(
            new MemoryStream(pdfBytes),
            effectiveFromFallback: new DateTime(2026, 11, 1));

        Assert.Null(result.Row);
        Assert.Contains(result.Errors, error => error.Contains("missing or invalid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_RejectsUnreadablePdfWithoutCreatingSalaryDraft()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("not a pdf"));
        var result = EmployeeSalaryPdfImportParser.Parse(stream);
        Assert.Null(result.Row);
        Assert.Contains(result.Errors, error => error.Contains("could not be read", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_RejectsUnreadableStream()
    {
        var result = EmployeeSalaryPdfImportParser.Parse(Stream.Null);
        Assert.Null(result.Row);
        Assert.Contains(result.Errors, error => error.Contains("readable PDF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_SucceedsWhenOptionalComponentsLikeBonusAndLtaAreOmitted()
    {
        var pdfBytes = CreateTextPdf(
            "Employee Id: EMP-999",
            "Effective From: 15/11/2026",
            "Basic Salary: 50000",
            "House Rent Allowance: 20000",
            "Special Allowance: 10000",
            "Annual CTC: 960000");

        var result = EmployeeSalaryPdfImportParser.Parse(new MemoryStream(pdfBytes));

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Row);
        Assert.Equal("EMP-999", result.Row.EmployeeId);
        Assert.Equal(new DateTime(2026, 11, 15), result.Row.EffectiveFrom);
        Assert.Equal(80000m, result.Row.MonthlyGross);
        Assert.Equal(960000m, result.Row.AnnualCtc);

        var basic = result.Row.Components.First(c => c.ComponentCode == "BASIC");
        var hra = result.Row.Components.First(c => c.ComponentCode == "HRA");
        var other = result.Row.Components.First(c => c.ComponentCode == "OTHER_ALLOWANCE");
        var bonus = result.Row.Components.First(c => c.ComponentCode == "BONUS");
        var lta = result.Row.Components.First(c => c.ComponentCode == "LTA");
        var insurance = result.Row.Components.First(c => c.ComponentCode == "HEALTH_INSURANCE");

        Assert.Equal(50000m, basic.Value);
        Assert.Equal(20000m, hra.Value);
        Assert.Equal(10000m, other.Value);
        Assert.Equal(0m, bonus.Value);
        Assert.Equal(0m, lta.Value);
        Assert.Equal(0m, insurance.Value);
    }

    [Fact]
    public void Parse_SupportsIndianCurrencyAndFormattedAmounts()
    {
        var pdfBytes = CreateTextPdf(
            "Employee Code: EMP-888",
            "Joining Date: 01-Dec-2026",
            "Basic Pay: Rs. 65,000/-",
            "HRA: ₹ 25,000",
            "Other Allowance: INR 10,000",
            "CTC: 1200000");

        var result = EmployeeSalaryPdfImportParser.Parse(new MemoryStream(pdfBytes));

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Row);
        Assert.Equal("EMP-888", result.Row.EmployeeId);
        Assert.Equal(new DateTime(2026, 12, 1), result.Row.EffectiveFrom);
        Assert.Equal(100000m, result.Row.MonthlyGross);
        Assert.Equal(1200000m, result.Row.AnnualCtc);
    }

    private static byte[] CreateTextPdf(params string[] lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 10 Tf");
        content.AppendLine("50 750 Td");
        foreach (var (line, index) in lines.Select((line, index) => (line, index)))
        {
            if (index > 0) content.AppendLine("0 -16 Td");
            content.Append('(').Append(line).AppendLine(") Tj");
        }
        content.AppendLine("ET");

        var contentText = content.ToString();
        var contentObject = new StringBuilder()
            .Append("<< /Length ").Append(Encoding.ASCII.GetByteCount(contentText)).AppendLine(" >>")
            .AppendLine("stream")
            .Append(contentText)
            .AppendLine("endstream")
            .ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            contentObject,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.WriteLine("%PDF-1.4");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var (pdfObject, index) in objects.Select((pdfObject, index) => (pdfObject, index)))
        {
            offsets.Add(output.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(pdfObject);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        var xrefOffset = output.Position;
        writer.WriteLine($"xref");
        writer.WriteLine($"0 {offsets.Count}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1)) writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {offsets.Count} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.Write("%%EOF");
        writer.Flush();
        return output.ToArray();
    }
}
