
using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Repository.Entities;

public class EmpEmailLogs : BaseClass
{
  [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
  public string EmailLogId { get; set; }
  public string UserTo { get; set; }
  public string Email { get; set; }
  public string Subject { get; set; }
  public string Body { get; set; }
  public MailType EmailLogType { get; set; }
  public string UserFrom { get; set; }
  public bool Status { get; set; }
  public string ErrorMessage { get; set; }

}
