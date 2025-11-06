using Microsoft.Extensions.DependencyInjection;

namespace SpawnDev.DB
{
    public static class Extensions
    {
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
