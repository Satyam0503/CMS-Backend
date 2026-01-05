using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Interface;

public interface IMigration
{
    string Id { get; }
    Task ExecuteAsync(IMongoDatabase db);
}

