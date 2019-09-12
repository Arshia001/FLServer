using Bond;
using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using OrleansIndexingGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public class PlayerInfoUtil
    {
        public static Task<PlayerInfo> GetForPlayerID(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetPlayerInfo();
    }

    [Immutable]
    public class PlayerLeaderBoardInfo
    {
        public string Name { get; }
        //?? avatar

        public PlayerLeaderBoardInfo(string name) => Name = name;
    }

    //?? split into current games and past games, keep full history of past games somewhere else or limit history to a few items
    [Schema, BondSerializationTag("#p")]
    public class PlayerState : IOnDeserializedHandler
    {
        [Id(0)]
        public List<IGame> ActiveGames { get; private set; }

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
        public List<IGame> PastGames { get; private set; }

        [Id(9)]
        public DateTime InfinitePlayEndTime { get; set; }

        [Id(10)]
        public HashSet<string> OwnedCategoryAnswers { get; private set; }

        [Id(11)]
        public Dictionary<(Statistics stat, int parameter), ulong> StatisticsValues { get; private set; }

        [Id(12)]
        public byte[] PasswordSalt { get; private set; }

        [Id(13)]
        public byte[] PasswordHash { get; set; }

        [Id(14)]
        public string Email { get; set; }

        [Id(15)]
        public string Username { get; set; }

        public void OnDeserialized()
        {
            if (ActiveGames == null)
                ActiveGames = new List<IGame>();
            if (PastGames == null)
                PastGames = new List<IGame>();
            if (OwnedCategoryAnswers == null)
                OwnedCategoryAnswers = new HashSet<string>();
            if (StatisticsValues == null)
                StatisticsValues = new Dictionary<(Statistics stat, int extra), ulong>();
            if (PasswordSalt == null)
                PasswordSalt = CryptographyHelper.GeneratePasswordSalt();
        }
    }

    [BondSerializationTag("@p")]
    public interface IPlayer : IGrainWithGuidKey
    {
        Task<OwnPlayerInfo> PerformStartupTasksAndGetInfo();

        Task<PlayerInfo> GetPlayerInfo();
        Task<OwnPlayerInfo> GetOwnPlayerInfo();
        Task<PlayerLeaderBoardInfo> GetLeaderBoardInfo();
        Task<(PlayerInfo info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories);

        Task<bool> SetUsername(string username);
        Task<bool> PerformRegistration(string email, string password);
        Task<bool> SetEmail(string email);
        Task<bool> UpdatePassword(string oldPassword, string newPassword);
        Task<bool> ValidatePassword(string password);

        Task AddStats(List<StatisticValue> values);

        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task<bool> CanEnterGame();
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task OnRoundResult(IGame game, CompetitionResult result, uint myScore, ushort groupID);
        Task<(uint score, uint rank)> OnGameResult(IGame game, CompetitionResult result, uint myScore);

        Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateInfinitePlay();

        Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid gameID);
        Task<(ulong? gold, string word, byte? wordScore)> RevealWord(Guid gameID);

        Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string category);
        Task<bool> HaveAnswersForCategory(string category);
        Task<IReadOnlyList<bool>> HaveAnswersForCategories(IReadOnlyList<string> categories);

        Task<IEnumerable<GroupInfoDTO>> RefreshGroups(Guid gameID);

        Task<(ulong totalGold, TimeSpan nextRewardTime)> TakeRewardForWinningRounds();
    }

    public static class PlayerIndex
    {
        static GrainIndexManager_Unique<string, IPlayer> byUsername =
            new GrainIndexManager_Unique<string, IPlayer>("p_un", 16384, new StringHashGenerator());

        static GrainIndexManager_Unique<string, IPlayer> byEmail =
            new GrainIndexManager_Unique<string, IPlayer>("p_e", 16384, new StringHashGenerator());

        public static Task<bool> UpdateUsernameIfUnique(IGrainFactory grainFactory, IPlayer player, string name) =>
            byUsername.UpdateIndexIfUnique(grainFactory, name, player);

        public static Task<bool> UpdateEmailIfValidAndUnique(IGrainFactory grainFactory, IPlayer player, string email) =>
            ValidationHelper.ValidateEmail(email) ?
                byEmail.UpdateIndexIfUnique(grainFactory, email, player)
                : Task.FromResult(false);

        public static Task<IPlayer> GetByEmail(IGrainFactory grainFactory, string email) => byEmail.GetGrain(grainFactory, email);
    }
}
