using FLGrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class Player : SaveStateOnDeactivateGrain<PlayerState>, IPlayer
    {
        IConfigReader configReader;

        public Player(IConfigReader configReader) => this.configReader = configReader;

        public Task<Immutable<OwnPlayerInfo>> PerformStartupTasksAndGetInfo()
        {
            if (State.Level == 0)
            {
                //?? init stuff

                State.Level = 1;
            }

            return GetOwnPlayerInfo();
        }

        public Task<Immutable<IReadOnlyList<IGame>>> GetGames() => Task.FromResult(State.MyGames.AsImmutable<IReadOnlyList<IGame>>());

        public Task<Immutable<PlayerInfo>> GetPlayerInfo() => Task.FromResult(GetPlayerInfoImpl().AsImmutable());

        string GetName() => State.Name ?? $"Guest{this.GetPrimaryKey().ToString().Substring(0, 8)}";

        public Task<Immutable<OwnPlayerInfo>> GetOwnPlayerInfo() =>
            Task.FromResult(new OwnPlayerInfo
            {
                Name = GetName(),
                Level = State.Level,
                XP = State.XP,
                NextLevelXPThreshold = GetNextLevelRequiredXP(configReader.Config),
                CurrentNumRoundsWonForReward = State.NumRoundsWonForReward
            }.AsImmutable());

        PlayerInfo GetPlayerInfoImpl() => new PlayerInfo { ID = this.GetPrimaryKey(), Name = GetName() }; //?? other info

        LevelConfig GetLevelConfig(ReadOnlyConfigData config)
        {
            if (!config.PlayerLevels.TryGetValue(State.Level, out var result))
                return config.PlayerLevels[0];
            return result;
        }

        uint GetNextLevelRequiredXP(ReadOnlyConfigData config) => GetLevelConfig(config).GetRequiredXP(State);

        void AddXP(uint amount) //?? notify client here or otherwise
        {
            var reqXP = GetNextLevelRequiredXP(configReader.Config);

            State.XP += amount;
            if (State.XP >= reqXP)
            {
                ++State.Level;
                State.XP -= reqXP;
            }
        }

        public async Task<byte> JoinGameAsFirstPlayer(IGame game)
        {
            var result = await game.StartNew(this.GetPrimaryKey());
            State.MyGames.Add(game);
            return result;
        }

        public async Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game)
        {
            var result = await game.AddSecondPlayer(GetPlayerInfoImpl());
            State.MyGames.Add(game);
            return result;
        }

        public Task OnRoundWon()
        {
            ++State.NumRoundsWonForReward;
            return GrainFactory.GetGrain<ISystemEndPoint>(0).SendNumRoundsWonForRewardUpdated(this.GetPrimaryKey(), State.NumRoundsWonForReward);
        }

        public Task<string> TakeRewardForWinningRounds()
        {
            if (State.NumRoundsWonForReward < configReader.Config.ConfigValues.NumRoundsToWinToGetReward)
                throw new VerbatimException("Reward not ready to take yet");

            State.NumRoundsWonForReward = 0;
            return Task.FromResult("Congrats, you win -.-");
        }
    }
}
