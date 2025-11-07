using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.Repository.Entities.Company;

public class LeaveSettings : BaseClass
{
    public string Id { get; set; }
    [Range(1, 12)]
    public int FinancialYearStartMonth { get; set; }
}
