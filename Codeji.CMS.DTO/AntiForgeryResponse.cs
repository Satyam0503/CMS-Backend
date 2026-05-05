namespace Codeji.CMS.DTO;

public class AntiForgeryResponse
{
    public required string RequestToken { get; set; }
    public required string CookieToken { get; set; }
}