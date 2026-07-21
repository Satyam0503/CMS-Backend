using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class PayrollDivisorPolicy : BaseClass
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
