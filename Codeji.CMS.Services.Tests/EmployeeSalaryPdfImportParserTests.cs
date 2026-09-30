using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryPdfImportParserTests
{
    [Fact]
    public void Parse_RejectsUnreadablePdfWithoutCreatingSalaryDraft()
    {
        using var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("not a pdf"));
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
}