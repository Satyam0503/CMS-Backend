using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Holidays;

public class Holidays : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string HolidayId { get; set; }
    public  string HolidayName { get; set; }
    public  DateTime Date { get; set; }
    public string Detail { get; set; }
    public EnumsHelper.HolidayTypes HolidayType { get; set; }
}
