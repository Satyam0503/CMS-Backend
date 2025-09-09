using DinkToPdf;
using DinkToPdf.Contracts;

namespace Codeji.CMS.Services;

public class PdfService
{
    readonly IConverter _converter;

    public PdfService(IConverter converter)
    {
        _converter = converter;
    }

    public byte[] GeneratePdfFormHtml(string htmlContent)
    {
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings() { Top = 10 },

                // path where pdf will be saved
                // Out = @"C:\DinkToPdf\src\DinkToPdf.TestThreadSafe\test.pdf",
            },
            Objects ={
                    new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        return _converter.Convert(doc);
    }

}
