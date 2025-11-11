using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpawnDev.WebFS.DokanAsync;

namespace SpawnDev.WebFS
{
    /// <summary>
    /// Adds extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Registers WebFSClient, TService as IAsyncDokanOperations and TService.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWebFS<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService, IAsyncDokanOperations
        {

            // Register our custom WebFS filesystem provider WebFSProvider, which implements IAsyncDokanOperations
            // WebFSProvider is a demo  WebFS provider that allows read and write access to the browser's Origin private file system
            services.TryAddSingleton<TService, TImplementation>();

            // WebFSClient connects to the WebFS tray app running on the user's PC
            // It will use the registered IAsyncDokanOperations service to handle requests from WebFS tray
            services.TryAddSingleton<WebFSClient>();

            // Register WebFSProvider as IAsyncDokanOperations also so WebFSClient can use it
            services.TryAddSingleton(sp => (IAsyncDokanOperations)sp.GetRequiredService<TService>());
            return services;
        }
        /// <summary>
        /// Registers WebFSClient, TService as IAsyncDokanOperations and TService.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWebFS<TService>(this IServiceCollection services)
            where TService : class, IAsyncDokanOperations
        {

            // Register our custom WebFS filesystem provider WebFSProvider, which implements IAsyncDokanOperations
            // WebFSProvider is a demo  WebFS provider that allows read and write access to the browser's Origin private file system
            services.TryAddSingleton<TService>();

            // WebFSClient connects to the WebFS tray app running on the user's PC
            // It will use the registered IAsyncDokanOperations service to handle requests from WebFS tray
            services.TryAddSingleton<WebFSClient>();

            // Register WebFSProvider as IAsyncDokanOperations also so WebFSClient can use it
            services.TryAddSingleton(sp => (IAsyncDokanOperations)sp.GetRequiredService<TService>());
            return services;
        }
    }
}
