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
        static IGame pendingGame;


        //?? Do I even need to mention that we need a matchmaking system?
        [MethodName("new")]
        public async Task<EndPointFunctionResult> NewGame(EndPointFunctionParams args)
        {
            var userProfile = GrainFactory.GetGrain<IUserProfile>(args.ClientID);

            if (pendingGame == null)
            {
                do
                    pendingGame = GrainFactory.GetGrain<IGame>(Guid.NewGuid());
                while (await pendingGame.GetState() != EGameState.New);

                var numRounds = await userProfile.JoinGameAsFirstPlayer(pendingGame);

                return Success(Param.Guid(pendingGame.GetPrimaryKey()), Param.String(null), Param.UInt(numRounds));
            }
            else
            {
                var result = await userProfile.JoinGameAsSecondPlayer(pendingGame);

                var gameID = pendingGame.GetPrimaryKey();
                pendingGame = null;

                return Success(Param.Guid(gameID), Param.String(result.opponentID.ToString()), Param.UInt(result.numRounds));
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
            var (totalScore, thisWordScore, corrected) = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).PlayWord(args.ClientID, args.Args[1].AsString);
            return Success(Param.UInt(totalScore), Param.Int(thisWordScore), Param.String(corrected));
        }

        [MethodName("endr")]
        public async Task<EndPointFunctionResult> EndRound(EndPointFunctionParams args)
        {
            var opponentWords = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).EndRound(args.ClientID);
            return Success(Param.Array(opponentWords.Select(ws => ws.ToParam())));
        }

        [MethodName("info")]
        public async Task<EndPointFunctionResult> GetGameInfo(EndPointFunctionParams args)
        {
            var result = await GrainFactory.GetGrain<IGame>(args.Args[0].AsGuid.Value).GetGameInfo(args.ClientID);
            return Success(result.ToParams(GrainFactory));
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


        public async Task SendOpponentJoined(Guid playerID, Guid gameID, Guid opponentID)
        {
            if (await IsConnected(playerID))
                await SendMessage(playerID, "opj", Param.Guid(gameID), Param.String(opponentID.ToString()));
            else
                SendPush();
        }

        public async Task SendOpponentTurnEnded(Guid playerID, Guid gameID, uint roundNumber, IEnumerable<WordScorePair> wordsPlayed)
        {
            if (await IsConnected(playerID))
                await SendMessage(playerID, "opr", Param.Guid(gameID), Param.UInt(roundNumber), Param.Array(wordsPlayed.Select(ws => ws.ToParam())));
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
