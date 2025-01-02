using System;
namespace Codeji.CMS.Utility.Helpers
{
    public class JwtSettingModel
    {
        public JwtSettingModel()
        {
        }

        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }

}

