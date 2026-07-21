using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public class PayrollDivisorPolicyDto
{
    public string? Id { get; set; }
    [Required, RegularExpression("CALENDAR_DAYS|WORKING_DAYS|FIXED_30_DAYS")]
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}
