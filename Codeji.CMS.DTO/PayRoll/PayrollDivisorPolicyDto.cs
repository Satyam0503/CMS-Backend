using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public class PayrollDivisorPolicyDto
{
    public string? Id { get; set; }
    [RegularExpression("Startup|Standard|Enterprise")]
    public string PayrollMode { get; set; } = "Standard";
    [Required, RegularExpression("CALENDAR_DAYS|WORKING_DAYS|FIXED_30_DAYS")]
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    [RegularExpression("FIXED_GROSS|BASIC_ONLY")]
    public string LopCalculationBase { get; set; } = "FIXED_GROSS";
    [RegularExpression("EXEMPT|CAPPED_CEILING|ACTUAL_BASIC_12")]
    public string EpfMode { get; set; } = "CAPPED_CEILING";
    public decimal EpfWageCeiling { get; set; } = 15000m;
    public decimal EpfEmployeeRatePercent { get; set; } = 12m;
    public decimal EpfEmployerRatePercent { get; set; } = 12m;
    [RegularExpression("EXEMPT|STATUTORY")]
    public string EsicMode { get; set; } = "STATUTORY";
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public decimal EsicEmployeeRatePercent { get; set; } = 0.75m;
    public decimal EsicEmployerRatePercent { get; set; } = 3.25m;
    [RegularExpression("AUTO_NEW_REGIME|AUTO_OLD_REGIME|MANUAL_ONLY")]
    public string TdsMode { get; set; } = "AUTO_NEW_REGIME";
    public bool RequireAttendanceLock { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}
