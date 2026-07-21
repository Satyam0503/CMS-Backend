using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;

public class AttendancePenaltyPolicy : BaseClass
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Combined LHD + ED monthly allowance";
    public int CombinedLhdEdMonthlyLimit { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;
    public bool RequiresHrApproval { get; set; } = true;
    public string DefaultDecision { get; set; } = "REVIEW_REQUIRED";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
}
