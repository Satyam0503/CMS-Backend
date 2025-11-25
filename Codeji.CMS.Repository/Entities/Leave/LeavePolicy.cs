using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Repository.Entities.Leave;

public class LeavePolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public bool Paid { get; set; } = true;

    public LeaveAccrualPeriod AccrualPeriod { get; set; }
    public decimal AccrualAmount { get; set; }
    public decimal? MaxBalance { get; set; } = 0;

    public bool CarryOverAllowed { get; set; } = false;
    public decimal? CarryOverLimit { get; set; }

    public int MinNoticeDays { get; set; } = 0;
    public bool HalfDayAllowed { get; set; } = false;
    public bool WeekendInclusive { get; set; } = false;
    public bool HolidayInclusive { get; set; } = false;
}
