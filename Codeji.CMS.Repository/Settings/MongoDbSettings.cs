using MongoDB.Driver;

namespace Codeji.CMS.GenericRepository.Settings
{

    public class MongoDbSettings
    {

        public string Connection { get; set; }
        public IEnumerable<string> Collections { get; set; }
        public MongoClientSettings ConnectionString { get; set; }

        // Derived from the path component of the Connection URI, e.g.
        // mongodb+srv://user:pass@cluster.mongodb.net/MyDb?... -> "MyDb"
        public string DatabaseName =>
            string.IsNullOrWhiteSpace(Connection) ? null : MongoUrl.Create(Connection).DatabaseName;
    }
}
