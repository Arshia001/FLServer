using System.Linq;
using FLGrainInterfaces;
using FLGrains;

namespace FLGrains
{
    [LightMessage.OrleansUtils.GrainInterfaces.EndPointNameAttribute("sys"), Orleans.Concurrency.StatelessWorkerAttribute(128)]
    public abstract class SystemEndPointBase : LightMessage.OrleansUtils.Grains.EndPointGrain, ISystemEndPoint
    {
        public virtual System.Threading.Tasks.Task SendNumRoundsWonForRewardUpdated(System.Guid clientID, uint totalRoundsWon) => SendMessage(clientID, "rwu", LightMessage.Common.Messages.Param.UInt(totalRoundsWon));
        protected abstract System.Threading.Tasks.Task<(OwnPlayerInfo playerInfo, byte numRoundsToWinToGetReward)> GetStartupInfo(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("st")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetStartupInfo(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetStartupInfo(input.ClientID);
            return Success(result.playerInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(result.numRoundsToWinToGetReward));
        }

        protected abstract System.Threading.Tasks.Task<(ulong totalGold, System.TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("trwr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_TakeRewardForWinningRounds(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await TakeRewardForWinningRounds(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.UInt(result.totalGold), LightMessage.Common.Messages.Param.TimeSpan(result.timeUntilNextReward));
        }
    }

    [LightMessage.OrleansUtils.GrainInterfaces.EndPointNameAttribute("sg"), Orleans.Concurrency.StatelessWorkerAttribute(128)]
    public abstract class SuggestionEndPointBase : LightMessage.OrleansUtils.Grains.EndPointGrain, ISuggestionEndPoint
    {
        protected abstract System.Threading.Tasks.Task SuggestCategory(System.Guid clientID, string name, System.Collections.Generic.IReadOnlyList<string> words);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("csug")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SuggestCategory(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await SuggestCategory(input.ClientID, array[0].AsString, array[1].AsArray.Select(a => a.AsString).ToList());
            return Success();
        }

        protected abstract System.Threading.Tasks.Task SuggestWord(System.Guid clientID, string categoryName, System.Collections.Generic.IReadOnlyList<string> words);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("wsug")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SuggestWord(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await SuggestWord(input.ClientID, array[0].AsString, array[1].AsArray.Select(a => a.AsString).ToList());
            return Success();
        }
    }

    [LightMessage.OrleansUtils.GrainInterfaces.EndPointNameAttribute("gm"), Orleans.Concurrency.StatelessWorkerAttribute(128)]
    public abstract class GameEndPointBase : LightMessage.OrleansUtils.Grains.EndPointGrain, IGameEndPoint
    {
        public virtual System.Threading.Tasks.Task SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfo opponentInfo) => SendMessage(clientID, "opj", LightMessage.Common.Messages.Param.Guid(gameID), opponentInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null());
        public virtual System.Threading.Tasks.Task SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO> wordsPlayed) => SendMessage(clientID, "opr", LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.UInt(roundNumber), LightMessage.Common.Messages.Param.Array(wordsPlayed?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        public virtual System.Threading.Tasks.Task SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank) => SendMessage(clientID, "gend", LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.UInt(myScore), LightMessage.Common.Messages.Param.UInt(theirScore), LightMessage.Common.Messages.Param.UInt(myPlayerScore), LightMessage.Common.Messages.Param.UInt(myPlayerRank));
        protected abstract System.Threading.Tasks.Task<(System.Guid gameID, PlayerInfo opponentInfo, byte numRounds, bool myTurnFirst)> NewGame(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("new")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_NewGame(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await NewGame(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.Guid(result.gameID), result.opponentInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(result.numRounds), LightMessage.Common.Messages.Param.Boolean(result.myTurnFirst));
        }

        protected abstract System.Threading.Tasks.Task<(string category, System.TimeSpan roundTime)> StartRound(System.Guid clientID, System.Guid gameID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("round")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_StartRound(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await StartRound(input.ClientID, array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.String(result.category), LightMessage.Common.Messages.Param.TimeSpan(result.roundTime));
        }

        protected abstract System.Threading.Tasks.Task<(byte wordScore, string corrected)> PlayWord(System.Guid clientID, System.Guid gameID, string word);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("word")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_PlayWord(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await PlayWord(input.ClientID, array[0].AsGuid.Value, array[1].AsString);
            return Success(LightMessage.Common.Messages.Param.UInt(result.wordScore), LightMessage.Common.Messages.Param.String(result.corrected));
        }

        protected abstract System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<WordScorePairDTO>> EndRound(System.Guid clientID, System.Guid gameID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("endr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_EndRound(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await EndRound(input.ClientID, array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.Array(result?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        protected abstract System.Threading.Tasks.Task<GameInfo> GetGameInfo(System.Guid clientID, System.Guid gameID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("info")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetGameInfo(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetGameInfo(input.ClientID, array[0].AsGuid.Value);
            return Success(result?.ToParam() ?? LightMessage.Common.Messages.Param.Null());
        }

        protected abstract System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<SimplifiedGameInfo>> GetAllGames(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("all")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetAllGames(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetAllGames(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.Array(result.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        protected abstract System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<WordScorePairDTO>> GetAnswers(System.Guid clientID, string category);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("ans")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetAnswers(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetAnswers(input.ClientID, array[0].AsString);
            return Success(LightMessage.Common.Messages.Param.Array(result.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }
    }

    public class LightMessageHost
    {
        LightMessage.OrleansUtils.Host.LightMessageOrleansHost host = new LightMessage.OrleansUtils.Host.LightMessageOrleansHost();

        public delegate System.Threading.Tasks.Task<System.Guid?> ClientAuthCallbackDelegate(System.Guid? clientID);

        ClientAuthCallbackDelegate onClientAuthCallback;

        public System.Threading.Tasks.Task Start(Orleans.IGrainFactory grainFactory, System.Net.IPEndPoint ipEndPoint, ClientAuthCallbackDelegate onClientAuthCallback, LightMessage.Common.Util.ILogProvider logProvider = null)
        {
            this.onClientAuthCallback = onClientAuthCallback;
            return host.Start(grainFactory, ipEndPoint, OnClientAuthRequest, logProvider);
        }

        public void Stop() => host.Stop();
        System.Threading.Tasks.Task<System.Guid?> OnClientAuthRequest(LightMessage.Common.ProtocolMessages.AuthRequestMessage message) => onClientAuthCallback(message.Params[0].AsGuid);
    }
}