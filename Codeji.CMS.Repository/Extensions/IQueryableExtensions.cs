using Codeji.CMS.GenericRepository.Interfaces;
using LinqKit;
using MongoDB.Driver.Linq;

namespace Codeji.CMS.GenericRepository.Extensions
{
    public static class IQueryableExtensions
    {
        public static IQueryable<TEntity> ApplyDefaultFilters<TEntity>(this IQueryable<TEntity> query, bool WithDeletedObjects, string companyId)
        {
            ExpressionStarter<TEntity> defaultFilters = GetDefaultFilters<TEntity>(WithDeletedObjects, companyId);
            return query.Where(defaultFilters);
        }
        //insert all default filters here.
        private static ExpressionStarter<TEntity> GetDefaultFilters<TEntity>(bool WithDeletedObjects, string companyId)
        {
            ExpressionStarter<TEntity> predicate = PredicateBuilder.New<TEntity>(x => true);
            if (typeof(ISupportAuditing).IsAssignableFrom(typeof(TEntity)) && !string.IsNullOrEmpty(companyId))
            {
                predicate.And(x => ((ISupportAuditing)x).CompanyId == companyId);
            }
            if (typeof(ISupportSoftDelete).IsAssignableFrom(typeof(TEntity)) && !WithDeletedObjects)
            {
                predicate.And(x => !((ISupportSoftDelete)x).IsDeleted);
            }
            return predicate;
        }
    }
}
