using FLGrainInterfaces;
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
        public static void ConfigureServices(IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IGrainReferenceConversionProvider, BondGrainReferenceConversionProvider>();

            var ConfigProvider = new ConfigProvider();
            services.AddSingleton<IConfigWriter>(ConfigProvider);
            services.AddSingleton<IConfigReader>(ConfigProvider);

            services.AddSingletonNamedService<IControllable, ConfigUpdateControllable>(ConfigUpdateControllable.ServiceName);

            var connectionStringProvider = new ConnectionStringProvider(connectionString);
            services.AddSingleton<IConnectionStringProvider>(connectionStringProvider);

            // this is a really early init stage, we shouldn't care about blocking here
            services.AddSingleton<ISuggestionService>(SuggestionService.CreateInstance(connectionStringProvider).Result);
        }
    }
}
