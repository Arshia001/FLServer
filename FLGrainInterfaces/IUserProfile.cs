using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    [Immutable]
    public class PlayerInfo
    {
        public static Task<PlayerInfo> GetForPlayerID(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IUserProfile>(playerID).GetPlayerInfo();

        public static Task<Param> GetAsParamForPlayerID(IGrainFactory grainFactory, Guid playerID) => GetForPlayerID(grainFactory, playerID).ContinueWith(t => t.Result.ToParam());


        public string Name { get; set; }


        public Param ToParam()
        {
            return Param.Array(
                Param.String(Name)
                );
        }
    }


    public interface IUserProfile : IGrainWithGuidKey
    {
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task<PlayerInfo> GetPlayerInfo();
    }
}
