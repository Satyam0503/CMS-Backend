using Microsoft.Extensions.DependencyInjection;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Repositories; 

using Codeji.CMS.GenericRepository.Repositories;

namespace Codeji.CMS.GenericRepository.Registration
{
    public static class RepositoryServicesRegistration
    {
        public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
        {
            //services.AddSingleton<IMongoDbContext, MongoDbContext>();
            // Register the cache service as a singleton
            services.AddSingleton<IMongoDbCacheService, MongoDbCacheService>();
            services.AddScoped(typeof(IMongoDbRepository<>), typeof(MongoRepository<>));

            return services;
        }
    }
}
