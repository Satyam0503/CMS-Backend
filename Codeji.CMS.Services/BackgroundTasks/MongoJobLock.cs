using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Codeji.CMS.Services.BackgroundTasks;

/// <summary>Cross-process mutex backed by a unique _id in the job_locks collection; expired locks are taken over.</summary>
public sealed class MongoJobLock(IMongoDatabase database)
{
    private sealed class LockDocument
    {
        [BsonId] public string Id { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private IMongoCollection<LockDocument> Locks => database.GetCollection<LockDocument>("job_locks");

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        var owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        var filter = Builders<LockDocument>.Filter.Eq(x => x.Id, key) &
                     Builders<LockDocument>.Filter.Lt(x => x.ExpiresAt, now);
        var update = Builders<LockDocument>.Update
            .Set(x => x.Owner, owner)
            .Set(x => x.ExpiresAt, now.Add(ttl));

        try
        {
            // A live lock fails the filter, so the upsert inserts a duplicate _id and throws.
            await Locks.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<LockDocument> { IsUpsert = true });
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return null;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }

        return new Releaser(Locks, key, owner);
    }

    private sealed class Releaser(IMongoCollection<LockDocument> locks, string key, string owner) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() =>
            await locks.DeleteOneAsync(x => x.Id == key && x.Owner == owner);
    }
}
