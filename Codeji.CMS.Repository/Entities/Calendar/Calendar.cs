using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Repository.Entities.Calendar;


public class Calendar
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string OccasionId { get; set; }
    public string OccasionName { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public Occasions OccasionType { get; set; }
    public string? OccasionImageUrl { get; set; }
}
