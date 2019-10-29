using FLGrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    [StatelessWorker]
    public class ClientAuthenticator : Grain, IClientAuthenticator
    {
        public async Task<Guid?> Authenticate(HandShakeMode mode, Guid? clientID, string email, string password)
        {
            switch (mode)
            {
                case HandShakeMode.ClientID:
                    return clientID ?? Guid.NewGuid();

                case HandShakeMode.EmailAndPassword:
                    {
                        var player = await PlayerIndex.GetByEmail(GrainFactory, email);
                        if (player == null || !await player.ValidatePassword(password))
                            return null;
                        return player.GetPrimaryKey();
                    }

                case HandShakeMode.RecoveryEmailRequest:
                    {
                        var player = await PlayerIndex.GetByEmail(GrainFactory, email);
                        if (player != null)
                            await player.SendPasswordRecoveryLink();
                        return null;
                    }

                default:
                    return null;
            }
        }
    }
}
