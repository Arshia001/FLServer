using FLGameLogic;
using FLGrainInterfaces;
using LightMessage.Common.Messages;
using LightMessage.OrleansUtils.GrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class GameEndPoint : GameEndPointBase
    {
        IConfigReader configReader;

        static System.Threading.SemaphoreSlim sem = new System.Threading.SemaphoreSlim(1);
        static HashSet<IGame> pendingGames = new HashSet<IGame>();


        public GameEndPoint(IConfigReader configReader) => this.configReader = configReader;

        protected override async Task<(Guid gameID, PlayerInfo opponentInfo, byte numRounds, bool myTurnFirst)> NewGame(Guid clientID)
        {
            var userProfile = GrainFactory.GetGrain<IPlayer>(clientID);

            if (!await userProfile.CanEnterGame())
                throw new VerbatimException("Cannot enter game at this time");

            await sem.WaitAsync();
            try
            {
                var gameToEnter = default(IGame);
                var opponentID = Guid.Empty;
                var numRounds = default(byte);
                var gameID = Guid.Empty;

                foreach (var game in pendingGames)
                {
                    if (!await game.WasFirstTurnPlayed())
                        continue;
                    try
                    {
                        (opponentID, numRounds) = await userProfile.JoinGameAsSecondPlayer(game);
                        gameID = game.GetPrimaryKey();
                        gameToEnter = game;
                        break;
                    }
                    catch { }
                }

                if (gameToEnter != null)
                {
                    pendingGames.Remove(gameToEnter);
                    return (gameID, await PlayerInfoUtil.GetForPlayerID(GrainFactory, opponentID), numRounds, false);
                }

                do
                    gameToEnter = GrainFactory.GetGrain<IGame>(Guid.NewGuid());
                while (await gameToEnter.GetState() != GameState.New);

                numRounds = await userProfile.JoinGameAsFirstPlayer(gameToEnter);

                pendingGames.Add(gameToEnter);

                return (gameToEnter.GetPrimaryKey(), null, numRounds, true);

            }
            finally
            {
                sem.Release();
            }
        }

        protected override async Task<IEnumerable<WordScorePairDTO>> EndRound(Guid clientID, Guid gameID)
        {
            var result = await GrainFactory.GetGrain<IGame>(gameID).EndRound(clientID);
            return result.Value?.Select(w => (WordScorePairDTO)w);
        }

        protected override async Task<IEnumerable<SimplifiedGameInfo>> GetAllGames(Guid clientID)
        {
            var games = (await GrainFactory.GetGrain<IPlayer>(clientID).GetGames()).Value;
            return await Task.WhenAll(games.Reverse().Select(g => g.GetSimplifiedGameInfo(clientID)));
        }

        public override async Task<bool> SendGameEnded(Guid clientID, Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myRank)
        {
            if (!await base.SendGameEnded(clientID, gameID, myScore, theirScore, myPlayerScore, myRank))
                SendPush();

            return true;
        }

        public override async Task<bool> SendOpponentJoined(Guid clientID, Guid gameID, PlayerInfo opponentInfo)
        {
            if (!await base.SendOpponentJoined(clientID, gameID, opponentInfo))
                SendPush();

            return true;
        }

        public override async Task<bool> SendOpponentTurnEnded(Guid clientID, Guid gameID, byte roundNumber, IEnumerable<WordScorePairDTO> wordsPlayed)
        {
            if (!await base.SendOpponentTurnEnded(clientID, gameID, roundNumber, wordsPlayed))
                SendPush();

            return true;
        }

        protected override Task Vote(Guid clientID, string category, bool up) =>
            GrainFactory.GetGrain<ICategoryStatisticsAggregationWorker>(category)
                .AddDelta(up ? new CategoryStatisticsDelta.UpVote() : (CategoryStatisticsDelta)new CategoryStatisticsDelta.DownVote());

        void SendPush() { } //?? stub
    }
}
