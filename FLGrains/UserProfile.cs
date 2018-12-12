using FLGrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    //?? split into current games and past games, keep full history of past games somewhere else or limit history to a few items
    class UserProfileState
    {
        public List<IGame> MyGames { get; set; }

        public UserProfileState()
        {
            MyGames = new List<IGame>();
        }
    }

    class UserProfile : SaveStateOnDeactivateGrain<UserProfileState>, IUserProfile
    {
        public Task<Immutable<IReadOnlyList<IGame>>> GetGames()
        {
            return Task.FromResult(State.MyGames.AsImmutable<IReadOnlyList<IGame>>());
        }

        public Task<PlayerInfo> GetPlayerInfo()
        {
            return Task.FromResult(new PlayerInfo { Name = this.GetPrimaryKey().ToString().Substring(8) });
        }

        public async Task<byte> JoinGameAsFirstPlayer(IGame game)
        {
            var result = await game.StartNew(this.GetPrimaryKey());
            State.MyGames.Add(game);
            return result;
        }

        public async Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game)
        {
            var result = await game.AddSecondPlayer(this.GetPrimaryKey());
            State.MyGames.Add(game);
            return result;
        }
    }
}
