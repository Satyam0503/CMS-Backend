using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Repository.Entities.Calendar;


public class CalendarEntity : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public bool Recurring { get; set; }
    public string Description { get; set; }
    public CalendarItem Type { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? TargetUserIds { get; set; }
    public bool RestrictToTargetUsersAndHrAdmin { get; set; }
}

