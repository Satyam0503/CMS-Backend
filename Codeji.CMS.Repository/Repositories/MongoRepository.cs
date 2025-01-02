using Codeji.CMS.GenericRepository;
using LinqKit;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Extensions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.GenericRepository.Services;
using System.Data.Entity;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;
using Codeji.CMS.GenericRepository.Repositories;
using System.Reflection;

namespace Codeji.CMS.GenericRepository
{
    public class MongoRepository<TEntity> : IMongoDbRepository<TEntity>
    {
        private IMongoCollection<TEntity> _dbSet = null;
        private readonly IHttpContextAccessor _httpContextAccessor;
        // private IMongoDbContext _mongoDbContext;
        public MongoRepository(IMongoDbCacheService _cacheService, IHttpContextAccessor httpContextAccessor)
        {
            MongoDbContext _mongoDbContext = new MongoDbContext(_cacheService, typeof(TEntity).Name);
            _dbSet = _mongoDbContext.GetCollection<TEntity>();
            _httpContextAccessor = httpContextAccessor;
        }

        //not to be used unless it's to join collections together 
        public IMongoCollection<TEntity> GetCollection()
        {
            return _dbSet;
        }
        #region getand set comapnyId
        private string GetCompanyId()
        {
            var companyId = _httpContextAccessor.HttpContext?.Items["CompanyId"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
            {
                throw new UnauthorizedAccessException("CompanyId is not available in the current context.");
            }
            return companyId;
        }

        private void SetCompanyId(TEntity entity)
        {
            var companyId = GetCompanyId();

            // Use reflection to set the CompanyId property dynamically
            var companyIdProperty = typeof(TEntity).GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance);
            if (companyIdProperty != null && companyIdProperty.CanWrite)
            {
                companyIdProperty.SetValue(entity, companyId);
            }
        }
        #endregion

        private IQueryable<TEntity> GetQuery(Expression<Func<TEntity, bool>> filter = null, bool WithDeletedObjects = false)
        {
            filter = filter ?? (x => true);
            IQueryable<TEntity> query = _dbSet.AsQueryable().ApplyDefaultFilters(WithDeletedObjects, GetCompanyId()).Where(filter);
            return query;
        }

        public IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, bool WithDeletedObjects = false)
        {
            IQueryable<TEntity> query =  GetQuery(filter, WithDeletedObjects);
            if (orderBy != null)
                return orderBy(query);
            return query.AsQueryable();
        }

        public async Task<IEnumerable<TEntity>> GetAll(Expression<Func<TEntity, bool>> whereCondition, bool WithDeletedObjects = false)
        {
            whereCondition = whereCondition ?? (x => true);
            IQueryable<TEntity> query = _dbSet.AsQueryable().ApplyDefaultFilters(WithDeletedObjects, GetCompanyId()).Where(whereCondition);
            return await Task.Run( ()=>query.AsEnumerable());

        }
        public async Task<int> Count(Expression<Func<TEntity, bool>> filter , bool WithDeletedObjects = false)
        {
            int res =  await GetQuery(filter, WithDeletedObjects).CountAsync();
            return res;
        }
        public async Task<bool> Exist(Expression<Func<TEntity, bool>> filter, bool WithDeletedObjects = false)
        {
            bool res = await GetQuery(filter, WithDeletedObjects).AnyAsync();
            return res;
        }
        public async Task<TEntity?> FirstOrDefault(Expression<Func<TEntity, bool>> filter, bool WithDeletedObjects = false)
        {
            TEntity? res = await GetQuery(filter, WithDeletedObjects).FirstOrDefaultAsync();
            return res;
        }
        #region Create
        public async Task<Result> AddOne(TEntity item)
        {
            Result res = new Result();
            try
            {
                SetCompanyId(item);
                IMongoCollection<TEntity> collection = _dbSet;
                await collection.InsertOneAsync(item);
                res.Success = true;
                res.Message = "OK";
                res.StatusCode = StatusCodes.Status200OK;
                return res;
            }

            catch (Exception ex)
            {
                res.StatusCode = StatusCodes.Status500InternalServerError;
                res.Message = HelperService.NotifyException("AddOne", "Exception adding one " + typeof(TEntity).Name, ex);
                return res;
            }
        }
        public async Task<Result> AddMany(IEnumerable<TEntity> item)
        {
            Result res = new Result();
            try
            {
                IMongoCollection<TEntity> collection = _dbSet;
                if (item.Any())
                {
                    item.ForEach(SetCompanyId);
                    await collection.InsertManyAsync(item);
                    res.Success = true;
                    res.Message = "OK";
                    res.StatusCode = StatusCodes.Status200OK;
                }
                return res;
            }
            catch (Exception ex)
            {
                res.StatusCode = StatusCodes.Status500InternalServerError;
                res.Message = HelperService.NotifyException("AddOne", "Exception adding many " + typeof(TEntity).Name, ex);
                return res;
            }
        }
        #endregion Create
        #region Delete
        public async Task<Result> Delete(FilterDefinition<TEntity> filter)
        {
            Result result = new Result();
            try
            {
                DeleteResult deleteRes = await _dbSet.DeleteOneAsync(filter);
                result.Success = true;
                result.Message = "OK";
                result.StatusCode = StatusCodes.Status200OK;
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = StatusCodes.Status500InternalServerError;
                result.Message = HelperService.NotifyException("DeleteOne", "Exception deleting one " + typeof(TEntity).Name, ex);
                return result;
            }
        }
        public async Task<Result> DeleteAll(FilterDefinition<TEntity> filter)
        {
            Result result = new Result();
            try
            {
                DeleteResult deleteRes = await _dbSet.DeleteManyAsync(filter);
                result.Success = true;
                result.Message = "OK";
                result.StatusCode = StatusCodes.Status200OK;
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = StatusCodes.Status500InternalServerError;
                result.Message = HelperService.NotifyException("DeleteMany", "Exception deleting many " + typeof(TEntity).Name, ex);
                return result;
            }
        }
        #endregion Delete
        #region Update
        public async Task<Result> Update(FilterDefinition<TEntity> filter, TEntity model)
        {
            Result result = new Result();
            try
            {
                SetCompanyId(model);
                // check filters with company ID.
                //to DO
                ReplaceOneResult updateRes = await _dbSet.ReplaceOneAsync(filter, model, new ReplaceOptions { IsUpsert = false });
                if (updateRes.ModifiedCount < 1)
                {
                    Exception ex = new Exception();
                    result.Message = HelperService.NotifyException("UpdateOne", "ERROR: updateRes.ModifiedCount < 1 for entity: " + typeof(TEntity).Name, ex);
                    result.StatusCode = StatusCodes.Status400BadRequest;
                    return result;
                }
                result.Success = true;
                result.Message = "OK";
                result.StatusCode = StatusCodes.Status200OK;
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = StatusCodes.Status500InternalServerError;
                result.Message = HelperService.NotifyException("UpdateOne", "Exception updating entity: " + typeof(TEntity).Name, ex);
                return result;
            }
        }
        public async Task<Result> UpdateMany(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> updateDefinition, bool IsUpsert = false)
        {
            Result result = new Result();
            try
            {
                UpdateResult updateRes = await _dbSet.UpdateManyAsync(filter, updateDefinition,
                    new UpdateOptions
                    {
                        IsUpsert = IsUpsert
                    });
                if (updateRes.ModifiedCount < 1)
                {
                    result.StatusCode = StatusCodes.Status400BadRequest;
                    return result;
                }
                result.StatusCode = StatusCodes.Status200OK;
                result.Success = true;
                result.Message = "OK";
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = StatusCodes.Status500InternalServerError;
                result.Message = HelperService.NotifyException("UpdateOne", "Exception updating entity: " + typeof(TEntity).Name, ex);
                return result;
            }
        }
        #endregion Update

        #region Async and New Methods
        /// <summary>
        /// Get Data with paging , Projection. Sorting
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="filter"></param>
        /// <param name="projection"></param>
        /// <param name="WithDeletedObjects"></param>
        /// <param name="isAscending"></param>
        /// <param name="orderedKey"></param>
        /// <param name="skip"></param>
        /// <param name="limit"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TResult>> GetAggregateDataAsync<TResult>(
            Expression<Func<TEntity, bool>> filter = null, ProjectionDefinition<TEntity, TResult>? projection = null, bool WithDeletedObjects = false, bool? isAscending = null, string? orderedKey = null, int? skip = null, int? limit = null, string? hint = null)
        {
            IAsyncCursor<TResult> cursor = await GetCursoryAsync(filter, projection, WithDeletedObjects, isAscending, orderedKey, skip, limit, hint);
            return cursor.ToEnumerable();
        }

        private async Task<IAsyncCursor<TResult>> GetCursoryAsync<TResult>(Expression<Func<TEntity, bool>>? filter = null, ProjectionDefinition<TEntity, TResult>? projection = null, bool WithDeletedObjects = false, bool? isAscending = null, string? orderedKey = null, int? pageNo = null, int? pageSize = null, string? hint = null)
        {
            try
            {
                Expression<Func<TEntity, bool>> defaultFilter = x => true;

                if (typeof(TEntity).GetProperties().Any(x => x.Name == "IsDeleted") && !WithDeletedObjects)
                {
                    defaultFilter = x => !((ISupportSoftDelete)x).IsDeleted;
                }
                if (typeof(TEntity).GetProperties().Any(x => x.Name == "CompanyId"))
                {
                    defaultFilter = defaultFilter.And(x => ((ISupportAuditing)x).CompanyId == GetCompanyId());
                }
                if (filter != null)
                {
                    defaultFilter = defaultFilter.And(filter);
                }
                

                List<IPipelineStageDefinition> pipeline = new List<IPipelineStageDefinition>        {
                    PipelineStageDefinitionBuilder.Match(Builders<TEntity>.Filter.Where(defaultFilter))
                };

                if (projection != null)
                {
                    pipeline.Add(PipelineStageDefinitionBuilder.Project(projection));
                }

                if (isAscending.HasValue && !string.IsNullOrEmpty(orderedKey))
                {
                    SortDefinition<TEntity> sortDefinition = isAscending.Value
                        ? Builders<TEntity>.Sort.Ascending(orderedKey)
                        : Builders<TEntity>.Sort.Descending(orderedKey);
                    pipeline.Add(PipelineStageDefinitionBuilder.Sort(sortDefinition));

                }
                if (pageNo.HasValue && pageSize.HasValue)
                {
                    //int total = _dbSet.AsQueryable().Where(defaultFilter).Count();
                    int skip = (pageNo.Value - 1) * pageSize.Value;
                    int limit = pageSize.Value;
                    pipeline.Add(PipelineStageDefinitionBuilder.Skip<int>(skip));
                    pipeline.Add(PipelineStageDefinitionBuilder.Limit<int>(limit));

                }

                AggregateOptions options = new AggregateOptions();
                if (!string.IsNullOrEmpty(hint))
                {
                    options.Hint = hint;
                }

                return await _dbSet.AggregateAsync<TResult>(pipeline, options);
            }
            catch (Exception)
            {
                // Log exception here
                return null;
            }
        }

        #endregion
    }
    //public static class MongoExtensions
    //{o
    //    public static Expression<Func<TEntity, bool>> GetDefaultFilters<TEntity>(bool withDeletedObjects)
    //    {
    //        ExpressionStarter<TEntity> predicate = PredicateBuilder.New<TEntity>(x => true);
    //        if (typeof(ISupportSoftDelete).IsAssignableFrom(typeof(TEntity)) && !withDeletedObjects)
    //        {
    //            predicate = predicate.And(x => ((ISupportSoftDelete)x).IsDeleted != true);
    //        }
    //        if (typeof(ISupportAuditing).IsAssignableFrom(typeof(TEntity)))
    //        {
    //            predicate = predicate.And(x => ((ISupportAuditing)x).CompanyId == "sdhgshjgd");
    //        }
    //        return predicate;
    //    }
    //}
}
