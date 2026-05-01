using Codeji.CMS.GenericRepository.Settings;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Codeji.CMS.GenericRepository.Repositories
{

    public interface IMongoDbCacheService
    {
        MongoDbSettings GetDatabaseSettings(string entityName);
        MongoDbSettings GetDefaultDatabaseSettings();
    }

    public class MongoDbCacheService : IMongoDbCacheService
    {
        private readonly MongoDbSettings _settings;

        public MongoDbCacheService(IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString("mongodb")
                ?? throw new InvalidOperationException("ConnectionStrings:mongodb is not configured.");

            _settings = new MongoDbSettings { Connection = connection };

            var client = new MongoClient(_settings.Connection);
            var database = client.GetDatabase(_settings.DatabaseName);
            _settings.Collections = database.ListCollectionNamesAsync().Result.ToList();
        }

        public MongoDbSettings GetDatabaseSettings(string entityName) => _settings;

        public MongoDbSettings GetDefaultDatabaseSettings() => _settings;
    }

}

