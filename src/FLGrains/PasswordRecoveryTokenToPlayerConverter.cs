using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FLGrainInterfaces;
using Orleans;
using Orleans.Concurrency;

namespace FLGrains
{
    [StatelessWorker]
    class PasswordRecoveryTokenToPlayerConverter : Grain, IPasswordRecoveryTokenToPlayerConverter
    {
        public Task<IPlayer?> GetPlayer(string token) => PlayerIndex.GetByRecoveryToken(GrainFactory, token);
    }
}
