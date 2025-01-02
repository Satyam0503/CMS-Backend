using System;
using Codeji.CMS.GenericRepository.Settings;
using Microsoft.Extensions.Options;
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
        private readonly Dictionary<string, MongoDbSettings> _cache = new();
        private readonly List<MongoDbSettings> _dbSettings;

        public MongoDbCacheService(IOptions<List<MongoDbSettings>> options)
        {
            _dbSettings = options.Value ?? throw new ArgumentException("MongoDbSettings must be provided.");

            foreach (var setting in _dbSettings)
            {
                var client = new MongoClient(setting.Connection);
                var database = client.GetDatabase(setting.DatabaseName);
                // Fetch and cache collection names
                setting.Collections =  database.ListCollectionNamesAsync().Result.ToList();
                _cache[setting.DatabaseName] = setting;
            }
        }

        public MongoDbSettings GetDatabaseSettings(string entityName)
        {
            // Find database that contains the collection
            return _dbSettings.FirstOrDefault(x => x.Collections.Contains(entityName))
                   ?? GetDefaultDatabaseSettings();
        }

        public MongoDbSettings GetDefaultDatabaseSettings()
        {
            return _dbSettings.FirstOrDefault()
                   ?? throw new InvalidOperationException("No valid MongoDbSettings configuration found.");
        }
    }

}

