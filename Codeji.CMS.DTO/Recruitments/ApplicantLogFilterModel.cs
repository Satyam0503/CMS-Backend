namespace Codeji.CMS.DTO.Recruitments;

public class ApplicantLogFilterModel
{
    public int PageNo { get; set; }
    public int PageSize { get; set; }
    public string ApplicantName { get; set; }
    public string JobRole { get; set; }
    public int[] ActivityCategory { get; set; }
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
}
