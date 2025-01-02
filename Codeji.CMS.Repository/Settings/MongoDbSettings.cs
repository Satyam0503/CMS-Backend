namespace Codeji.CMS.GenericRepository.Settings
{

    public class MongoDbSettings
    {

        public string Service { get; set; }
        public string Connection { get; set; }
        public string DatabaseName { get; set; }
        public IEnumerable<string> Collections { get; set; }

    }
}
