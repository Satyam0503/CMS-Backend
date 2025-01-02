namespace Codeji.CMS.GenericRepository.Interfaces
{
    public interface ISupportAuditing
    {
        string CompanyId { get; set; }
        string CreatedBy { get; set; }
        DateTime? CreatedDate { get; set; }
        string UpdatedBy { get; set; }
        DateTime? UpdatedDate { get; set; }
    }
}
