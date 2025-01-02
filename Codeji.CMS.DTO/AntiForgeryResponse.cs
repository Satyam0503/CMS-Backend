namespace Codeji.CMS.DTO
{
    public class AntiForgeryResponse
    {
        public string RequestToken { get; set; }
        public string CookieToken { get; set; }
    }
}