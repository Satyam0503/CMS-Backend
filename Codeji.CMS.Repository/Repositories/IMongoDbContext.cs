using MongoDB.Driver;

namespace Codeji.CMS.GenericRepository
{
    public interface IMongoDbContext
    {
        IMongoCollection<TEntity> GetCollection<TEntity>();

        IMongoCollection<TEntity> GetCollection<TEntity>(string CollectionName);

    }
}
