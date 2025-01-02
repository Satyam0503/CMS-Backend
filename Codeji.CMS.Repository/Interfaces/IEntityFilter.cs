namespace Codeji.CMS.GenericRepository.Interfaces
{
    public interface IEntityFilter<TOutput>
    {
        IQueryable<TOutput> ApplyFilter(IQueryable<TOutput> queryable, string serializedValue);
    }
}
