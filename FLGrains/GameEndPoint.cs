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

        //?? push updates about opponent moves and game state changes to players
    }
}
