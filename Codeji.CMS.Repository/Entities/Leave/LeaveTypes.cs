using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class LeaveTypes : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string LeaveTypeId { get; set; }
    public int MaxLeaveDays { get; set; }
    public bool IsHalfDay { get; set; } = false;
    public int? MinAdvanceNoticeDate { get; set; }
    public bool IsActive { get; set; } = false;
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    
}
