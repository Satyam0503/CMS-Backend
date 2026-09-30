using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public class PayrollDivisorPolicy : BaseClass
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PayrollMode { get; set; } = "Standard";
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public string LopCalculationBase { get; set; } = "FIXED_GROSS";
    public string EpfMode { get; set; } = "CAPPED_CEILING";
    public decimal EpfWageCeiling { get; set; } = 15000m;
    public string EsicMode { get; set; } = "STATUTORY";
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public string TdsMode { get; set; } = "AUTO_NEW_REGIME";
    public bool RequireAttendanceLock { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
