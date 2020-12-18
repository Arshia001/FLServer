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
        public async Task<Guid?> Authenticate(HandShakeMode mode, Guid? clientID, string? email, string? password, string? bazaarToken, string? bazaarUserName)
        {
            switch (mode)
            {
                case HandShakeMode.ClientID:
                    return clientID ?? Guid.NewGuid();

                case HandShakeMode.EmailAndPassword:
                    {
                        if (email == null || password == null)
                            return null;
                        var player = await PlayerIndex.GetByEmail(GrainFactory, email);
                        if (player == null || !await player.ValidatePassword(password))
                            return null;
                        return player.GetPrimaryKey();
                    }

                case HandShakeMode.RecoveryEmailRequest:
                    {
                        if (email == null)
                            return null;
                        var player = await PlayerIndex.GetByEmail(GrainFactory, email);
                        if (player != null)
                            await player.SendPasswordRecoveryLink();
                        return null;
                    }

                case HandShakeMode.BazaarToken:
                    {
                        if (string.IsNullOrEmpty(bazaarToken))
                            return null;

                        var player = await PlayerIndex.GetByBazaarToken(GrainFactory, bazaarToken);
                        if (player == null)
                        {
                            player = GrainFactory.GetGrain<IPlayer>(Guid.NewGuid());
                            if (!string.IsNullOrEmpty(bazaarUserName))
                                await player.SetUsername(bazaarUserName);
                            await PlayerIndex.SetBazaarToken(GrainFactory, player, bazaarToken);
                        }

                        return player.GetPrimaryKey();
                    }

                default:
                    return null;
            }
        }
    }
}
