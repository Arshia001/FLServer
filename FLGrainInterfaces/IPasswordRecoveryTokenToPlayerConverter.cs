using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace FLGrainInterfaces
{
    public interface IPasswordRecoveryTokenToPlayerConverter: IGrainWithIntegerKey
    {
        Task<IPlayer?> GetPlayer(string token);
    }
}
