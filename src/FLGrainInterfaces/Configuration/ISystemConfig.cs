using Newtonsoft.Json.Converters;
using Orleans;
using Orleans.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces.Configuration
{
    public interface ISystemConfig : IGrainWithIntegerKey
    {
        Task UpdateConfigFromDatabase();

        Task<Immutable<ConfigData>> GetConfig();

        Task UploadConfig(string jsonConfig);
    }
}
