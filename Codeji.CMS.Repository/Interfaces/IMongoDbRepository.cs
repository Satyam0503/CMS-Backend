using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using MongoDB.Driver;

namespace Codeji.CMS.GenericRepository.Interfaces
{
    public interface IMongoDbRepository<TEntity>
    {
        /// <summary>
        /// Get Raw Collection from context without any filters and checks, 
        /// </summary>
        /// <returns>It will return collection of TEntity</returns>
        IMongoCollection<TEntity> GetCollection();
        /// <summary>
        /// Get Collection As Queryable  within filters, Can set order by in collection.
        /// can get Select column or property from collection
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <returns>It will return only query as queryable. here you can perform any operation at your method or bussiness logic, </returns>
        IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, bool WithDeletedObjects = false);
        /// <summary>
        /// Get Data Enumerable List with filter. No support for  Single property get 
        /// </summary>
        /// <param name="whereCondition"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <returns>Return the list of data as Enumerable </returns>
        Task<IEnumerable<TEntity>> GetAll(Expression<Func<TEntity, bool>> whereCondition = null, bool WithDeletedObjects = false);
        /// <summary>
        /// Get Single records from collection within filters
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <returns></returns>
        Task<TEntity?> FirstOrDefault(Expression<Func<TEntity, bool>> filter, bool WithDeletedObjects = false);
        /// <summary>
        /// Check data exist in colletion or not.
        /// </summary>
        /// <param name="whereCondition"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <returns>Return true or false.</returns>
        Task<bool> Exist(Expression<Func<TEntity, bool>> whereCondition, bool WithDeletedObjects = false);
        /// <summary>
        /// Get count of records from collection within filters
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <returns> Returns int value of records</returns>
        Task<int> Count(Expression<Func<TEntity, bool>> filter = null, bool WithDeletedObjects = false);
        /// <summary>
        /// Insert single records in collection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<Result> AddOne(TEntity entity);
        /// <summary>
        /// Insert many records in collection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<Result> AddMany(IEnumerable<TEntity> entity);
        /// <summary>
        /// Delete single records from collection with filter(Hard deleted)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Result> Delete(FilterDefinition<TEntity> filter);
        /// <summary>
        /// Delete many records  from collection with filter.(Hard Deleted)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Result> DeleteAll(FilterDefinition<TEntity> filter);
        #region Update
        /// <summary>
        /// Update only one records with filter and data entity . No needs to pass update definitions.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<Result> Update(FilterDefinition<TEntity> filter, TEntity entity);
        /// <summary>
        /// Update one or many data in collection without Data entity. just pass filter and update defination.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="updateDefinition"></param>
        /// <param name="IsUpsert"></param>
        /// <returns>Returns result model</returns>
        Task<Result> UpdateMany(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, bool IsUpsert = false);
        #endregion Update

        //IEnumerable<TEntity> GetAggregateData(Expression<Func<TEntity, bool>> filter, ProjectionDefinition<TEntity> projection = null, bool WithDeletedObjects = false, bool? isAscending = null, string orderedKey = null);


        #region New methods
        Task<IEnumerable<TResult>> GetAggregateDataAsync<TResult>(
            Expression<Func<TEntity, bool>> filter = null, ProjectionDefinition<TEntity, TResult>? projection = null, bool WithDeletedObjects = false, bool? isAscending = null, string? orderedKey = null, int? pageNo = null, int? pageSize = null, string? hint = null);


        #endregion
    }
}
