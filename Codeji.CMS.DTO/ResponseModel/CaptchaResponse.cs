using Newtonsoft.Json;

namespace Codeji.CMS.DTO.ResponseModel
{
    public class CaptchaResponse
    {
        [JsonProperty("sucess")]
        public bool Success { get; set; }
    }
}
