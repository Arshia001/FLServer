using Bond;
using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public class PlayerInfoUtil
    {
        public static Task<PlayerInfo> GetForPlayerID(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetPlayerInfo().UnwrapImmutable();

        public static Task<OwnPlayerInfo> GetOwnForPlayerID(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetOwnPlayerInfo().UnwrapImmutable();
    }

    //?? split into current games and past games, keep full history of past games somewhere else or limit history to a few items
    [Schema, BondSerializationTag("#p")]
    public class PlayerState : IOnDeserializedHandler
    {
        [Id(0)]
        public List<IGame> ActiveGames { get; set; }

        [Id(1)]
        public string Name { get; set; }

        [Id(2)]
        public uint Level { get; set; }

        [Id(3)]
        public uint XP { get; set; }

        [Id(4)]
        public uint NumRoundsWonForReward { get; set; }

        [Id(5)]
        public DateTime LastRoundWinRewardTakeTime { get; set; }

        [Id(6)]
        public uint Score { get; set; }

        [Id(7)]
        public ulong Gold { get; set; }

        [Id(8)]
        public List<IGame> PastGames { get; set; }

        [Id(9)]
        public DateTime InfinitePlayEndTime { get; set; }

        public void OnDeserialized()
        {
            if (ActiveGames == null)
                ActiveGames = new List<IGame>();
            if (PastGames == null)
                PastGames = new List<IGame>();
        }
    }

    [BondSerializationTag("@p")]
    public interface IPlayer : IGrainWithGuidKey
    {
        Task<Immutable<OwnPlayerInfo>> PerformStartupTasksAndGetInfo();

        Task<Immutable<PlayerInfo>> GetPlayerInfo();
        Task<Immutable<OwnPlayerInfo>> GetOwnPlayerInfo();

        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task<bool> CanEnterGame();
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task OnRoundWon(IGame game);
        Task<(uint score, uint rank)> OnGameResult(IGame game, Guid? winnerID);

        Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateInfinitePlay();

        Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid gameID);
        Task<(ulong? gold, string word, byte? wordScore)> RevealWord(Guid gameID);

        Task<IEnumerable<GroupInfoDTO>> RefreshGroups(Guid gameID);

        Task<(ulong totalGold, TimeSpan nextRewardTime)> TakeRewardForWinningRounds();
    }
}
