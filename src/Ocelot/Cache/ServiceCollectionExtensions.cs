using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ocelot.Cache
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOcelotCacheProvider<T>(this IServiceCollection services)
            where T : class, IOcelotCacheProvider
        {
            // Remove any existing cache providers
            services.RemoveAll(typeof(IOcelotCacheProvider));

            // Add the new cache provider, 
            // You should be careful and make sure you are only adding one cache provider
            services.AddSingleton<IOcelotCacheProvider, T>();
        }
    }
}
