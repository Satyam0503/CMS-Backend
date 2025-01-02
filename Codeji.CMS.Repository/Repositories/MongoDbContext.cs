using Codeji.CMS.GenericRepository.Repositories;
using MongoDB.Driver;
namespace Codeji.CMS.GenericRepository
{
    public class MongoDbContext
    {
        //http://www.layerworks.com/blog/2014/11/11/mongodb-shell-csharp-driver-comparison-cheat-cheet
        private readonly IMongoClient _appClient;
        private readonly IMongoDatabase _appDatabase;
        //IMongoDbCacheService cacheService;
        public MongoDbContext(IMongoDbCacheService _cacheService, string entityName = "")
        {
            //one know issue is there when first time table created in db it will not identify the right db connetion string.
            //thing alternative solutions.
            var dbSettings = _cacheService.GetDatabaseSettings(entityName);
            // Initialize MongoClient and Database
            _appClient = new MongoClient(dbSettings.Connection);
            _appDatabase = _appClient.GetDatabase(dbSettings.DatabaseName);
        }


        /// <summary>   
        /// The private GetCollection method
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IMongoCollection<TEntity> GetCollection<TEntity>()
        {

            return _appDatabase.GetCollection<TEntity>(typeof(TEntity).Name);

        }
        public IMongoCollection<TEntity> GetCollection<TEntity>(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name must be provided.");
            return _appDatabase.GetCollection<TEntity>(collectionName);
        }
    }
}
