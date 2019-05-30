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

        //?? Do I even need to mention that we need a matchmaking system?
        protected override async Task<(Guid gameID, PlayerInfo opponentInfo, byte numRounds, bool myTurnFirst)> NewGame(Guid clientID)
        {
            var userProfile = GrainFactory.GetGrain<IPlayer>(clientID);

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

        protected override Task<(string category, TimeSpan roundTime)> StartRound(Guid clientID, Guid gameID) =>
            GrainFactory.GetGrain<IGame>(gameID).StartRound(clientID);

        protected override Task<(byte wordScore, string corrected)> PlayWord(Guid clientID, Guid gameID, string word) =>
            GrainFactory.GetGrain<IGame>(gameID).PlayWord(clientID, word);

        protected override async Task<IEnumerable<WordScorePairDTO>> EndRound(Guid clientID, Guid gameID)
        {
            var result = await GrainFactory.GetGrain<IGame>(gameID).EndRound(clientID);
            return result.Value.Select(w => (WordScorePairDTO)w);
        }

        protected override async Task<GameInfo> GetGameInfo(Guid clientID, Guid gameID)
        {
            var result = await GrainFactory.GetGrain<IGame>(gameID).GetGameInfo(clientID);
            return result.Value;
        }

        protected override async Task<IEnumerable<SimplifiedGameInfo>> GetAllGames(Guid clientID)
        {
            var games = (await GrainFactory.GetGrain<IPlayer>(clientID).GetGames()).Value;
            return await Task.WhenAll(games.Reverse().Select(g => g.GetSimplifiedGameInfo(clientID).UnwrapImmutable()));
        }

        protected override async Task<IEnumerable<WordScorePairDTO>> GetAnswers(Guid clientID, string category) =>
            await Task.WhenAll(configReader.Config.CategoriesByName[category].Answers.Select(
                async s => new WordScorePairDTO
                {
                    Word = s,
                    Score = await GrainFactory.GetGrain<IWordUsageAggregatorCache>(category).GetScore(s)
                }
            ));


        public override async Task SendGameEnded(Guid clientID, Guid gameID, uint myScore, uint theirScore)
        {
            if (await IsConnected(clientID))
                await base.SendGameEnded(clientID, gameID, myScore, theirScore);
            else
                SendPush();
        }

        public override async Task SendOpponentJoined(Guid clientID, Guid gameID, PlayerInfo opponentInfo)
        {
            if (await IsConnected(clientID))
                await base.SendOpponentJoined(clientID, gameID, opponentInfo);
            else
                SendPush();
        }

        public override async Task SendOpponentTurnEnded(Guid clientID, Guid gameID, byte roundNumber, IEnumerable<WordScorePairDTO> wordsPlayed)
        {
            if (await IsConnected(clientID))
                await base.SendOpponentTurnEnded(clientID, gameID, roundNumber, wordsPlayed);
            else
                SendPush();
        }

        void SendPush() { } //?? stub
    }
}
