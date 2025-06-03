
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class MailTemplate
    {
        public string _id { get; set; }
        public EnumsHelper.MailType mailType { get; set; }
        public string subject { get; set; }
        public string body { get; set; }

    }
}
