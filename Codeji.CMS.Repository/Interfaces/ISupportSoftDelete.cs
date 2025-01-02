namespace Codeji.CMS.GenericRepository.Interfaces
{
    public interface ISupportSoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
