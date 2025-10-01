
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Codeji.CMS.Services;

public class PdfService
{
    public PdfService()
    {
    }

    public async Task<byte[]> GeneratePdfFormHtml(string htmlContent)
    {
        await new BrowserFetcher().DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlContent);
        // generate pdf 
        byte[] pdfByte = await page.PdfDataAsync(new PdfOptions()
        {
            DisplayHeaderFooter = true,
            HeaderTemplate = $"<div style='font-size:10px; text-align:center; width:100%;'>CodeJi</div>",
            FooterTemplate = $"<div style='font-size:10px; text-align:center; width:100%;'>{DateTime.UtcNow.ToString("dddd, dd MMMM yyyy")}</div>",
            PrintBackground = true,
            Landscape = false,
            Format = PaperFormat.A4,
            MarginOptions = new MarginOptions
            {
                Bottom = "50px",
                Left = "10px",
                Right = "10px"
            }
        });
        return pdfByte;
    }

}
