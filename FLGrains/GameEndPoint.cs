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
    [StatelessWorker(128), EndPointName("game")]
    class GameEndPoint : EndPointGrain, IGameEndPoint
    {
        IConfigReader configReader;

        static System.Threading.SemaphoreSlim sem = new System.Threading.SemaphoreSlim(1);
        static HashSet<IGame> pendingGames = new HashSet<IGame>();


        public GameEndPoint(IConfigReader configReader) => this.configReader = configReader;

        //?? Do I even need to mention that we need a matchmaking system?
        [MethodName("new")]
        public async Task<EndPointFunctionResult> NewGame(EndPointFunctionParams args)
        {
            var userProfile = GrainFactory.GetGrain<IUserProfile>(args.ClientID);

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
                    return Success(Param.Guid(gameID), await PlayerInfo.GetAsParamForPlayerID(GrainFactory, opponentID), Param.UInt(numRounds), Param.Boolean(false));
                }

                do
                    gameToEnter = GrainFactory.GetGrain<IGame>(Guid.NewGuid());
                while (await gameToEnter.GetState() != GameState.New);

                numRounds = await userProfile.JoinGameAsFirstPlayer(gameToEnter);

                pendingGames.Add(gameToEnter);

                return Success(Param.Guid(gameToEnter.GetPrimaryKey()), Param.Null(), Param.UInt(numRounds), Param.Boolean(true));

            }
            finally
            {
                sem.Release();
            }
        }

        [MethodName("round")]
        public async Task<EndPointFunctionResult> StartRound(EndPointFunctionParams args)
        {
            var (category, endTime) = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).StartRound(args.ClientID);
            return Success(Param.String(category), Param.TimeSpan(endTime));
        }

        [MethodName("word")]
        public async Task<EndPointFunctionResult> PlayWord(EndPointFunctionParams args)
        {
            var (wordScore, corrected) = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).PlayWord(args.ClientID, args.Args[1].AsString);
            return Success(Param.UInt(wordScore), Param.String(corrected));
        }

        [MethodName("endr")]
        public async Task<EndPointFunctionResult> EndRound(EndPointFunctionParams args)
        {
            var opponentWords = (await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).EndRound(args.ClientID)).Value;
            return Success(opponentWords == null ? Param.Null() : Param.Array(opponentWords.Select(ws => ws.ToParam())));
        }

        [MethodName("info")]
        public async Task<EndPointFunctionResult> GetGameInfo(EndPointFunctionParams args)
        {
            var result = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).GetGameInfo(args.ClientID);
            return Success(await result.ToParam(GrainFactory));
        }

        [MethodName("all")]
        public async Task<EndPointFunctionResult> GetAllGames(EndPointFunctionParams args)
        {
            var games = (await GrainFactory.GetGrain<IUserProfile>(args.ClientID).GetGames()).Value;
            var gameInfos = new List<SimplifiedGameInfo>();
            for (int i = games.Count - 1; i >= 0; --i)
                gameInfos.Add(await games[i].GetSimplifiedGameInfo(args.ClientID));

            return Success(gameInfos.Select(gi => Param.Array(gi.ToParams(GrainFactory))));
        }

        [MethodName("ans")]
        public async Task<EndPointFunctionResult> GetAllAnswers(EndPointFunctionParams args)
            => Success(Param.Array(
                await Task.WhenAll(configReader.Config.CategoriesByName[args.Args[0].AsString].Answers.Select(
                    async s => Param.Array(
                        Param.String(s),
                        Param.UInt(await GrainFactory.GetGrain<IWordUsageAggregatorCache>(args.Args[0].AsString).GetScore(s))
                        )
                    ))
                ));


        public async Task SendOpponentJoined(Guid playerID, Guid gameID, PlayerInfo opponent)
        {
            if (await IsConnected(playerID))
                await SendMessage(playerID, "opj", Param.Guid(gameID), opponent.ToParam());
            else
                SendPush();
        }

        public async Task SendOpponentTurnEnded(Guid playerID, Guid gameID, uint roundNumber, IEnumerable<WordScorePair> wordsPlayed)
        {
            if (await IsConnected(playerID))
                await SendMessage(playerID, "opr", Param.Guid(gameID), Param.UInt(roundNumber), wordsPlayed == null ? Param.Null() : Param.Array(wordsPlayed.Select(ws => ws.ToParam())));
            else
                SendPush();
        }

        public async Task SendGameEnded(Guid playerID, Guid gameID, uint myScore, uint theirScore)
        {
            if (await IsConnected(playerID))
                await SendMessage(playerID, "gend", Param.Guid(gameID), Param.UInt(myScore), Param.UInt(theirScore));
            else
                SendPush();
        }

        void SendPush() { } //?? stub
    }
}
