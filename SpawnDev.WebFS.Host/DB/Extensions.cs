using Microsoft.Extensions.DependencyInjection;

namespace SpawnDev.DB
{
    /// <summary>
    /// Adds extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Add AppDB service and set the default handlers for DateTime and DateTimeOffset
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAppDB(this IServiceCollection services)
        {
            DateTimeHandler.AddDateTimeHandler();
            DateTimeNullableHandler.AddDateTimeNullableHandler();
            DateTimeOffsetHandler.AddDateTimeOffsetHandler();
            DateTimeOffsetNullableHandler.AddDateTimeOffsetNullableHandler();
            services.AddSingleton<AppDB>();
            return services;
        }
    }
}
