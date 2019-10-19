using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.Configuration;
using FLGrains.ServiceInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;
using Orleans.Runtime;
using OrleansBondUtils.CassandraInterop;
using OrleansCassandraUtils.Reminders;

namespace FLGrains
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureGameServer(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IGrainReferenceConversionProvider, BondGrainReferenceConversionProvider>();

            var configProvider = new ConfigProvider();
            services.AddSingleton<IConfigWriter>(configProvider);
            services.AddSingleton<IConfigReader>(configProvider);

            services.AddSingletonNamedService<IControllable, ConfigUpdateControllable>(ConfigUpdateControllable.ServiceName);

            var connectionStringProvider = new ConnectionStringProvider(connectionString);
            services.AddSingleton<IConnectionStringProvider>(connectionStringProvider);

            services.AddSingleton<ILeaderBoardPlayerInfoCacheService, LeaderBoardPlayerInfoCacheService>();

            // this is a really early init stage, we shouldn't care about blocking here
            services.AddSingleton<ISuggestionService>(SuggestionService.CreateInstance(connectionStringProvider).Result);

            return services;
        }
    }
}
