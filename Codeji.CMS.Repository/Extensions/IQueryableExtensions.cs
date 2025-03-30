using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using LinqKit;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Codeji.CMS.GenericRepository.Extensions
{
    public static class IQueryableCustomExtensions
    {
        // public static IQueryable<TEntity> ApplyDefaultFilters<TEntity>(this IQueryable<TEntity> query, bool WithDeletedObjects, string companyId)
        // {
        //     ExpressionStarter<TEntity> defaultFilters = GetDefaultFilters<TEntity>(WithDeletedObjects, companyId);
        //     return query.Where(defaultFilters);
        // }
        public static FilterDefinition<TEntity> ApplyDefaultFilters<TEntity>(Expression<Func<TEntity, bool>> filter, bool withDeletedObjects, string _companyId)
        {
            var builder = Builders<TEntity>.Filter;
            var combinedFilter = builder.Where(filter ?? (x => true));

            if (typeof(ISupportAuditing).IsAssignableFrom(typeof(TEntity)) && !string.IsNullOrEmpty(_companyId))
            {
                var companyFilter = builder.Eq("CompanyId", _companyId);
                combinedFilter = builder.And(combinedFilter, companyFilter);
            }

            if (typeof(ISupportSoftDelete).IsAssignableFrom(typeof(TEntity)) && !withDeletedObjects)
            {
                var notDeletedFilter = builder.Eq("IsDeleted", false);
                combinedFilter = builder.And(combinedFilter, notDeletedFilter);
            }

            return combinedFilter;
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
