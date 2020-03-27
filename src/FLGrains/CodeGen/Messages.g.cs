using System.Linq;
using FLGrainInterfaces;
using FLGrains;
#nullable enable annotations 

namespace FLGrains
{
    [LightMessage.OrleansUtils.GrainInterfaces.EndPointNameAttribute("sys"), Orleans.Concurrency.StatelessWorkerAttribute(128)]
    public abstract class SystemEndPointBase : LightMessage.OrleansUtils.Grains.EndPointGrain, ISystemEndPoint
    {
        public virtual System.Threading.Tasks.Task<bool> SendNumRoundsWonForRewardUpdated(System.Guid clientID, uint totalRoundsWon) => SendMessage(clientID, "rwu", LightMessage.Common.Messages.Param.UInt(totalRoundsWon));
        public virtual System.Threading.Tasks.Task<bool> SendStatisticUpdated(System.Guid clientID, StatisticValueDTO stat) => SendMessage(clientID, "st", stat?.ToParam() ?? LightMessage.Common.Messages.Param.Null());
        protected abstract System.Threading.Tasks.Task<(OwnPlayerInfoDTO playerInfo, ConfigValuesDTO configData, System.Collections.Generic.IEnumerable<GoldPackConfigDTO> goldPacks)> GetStartupInfo(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("st")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetStartupInfo(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetStartupInfo(input.ClientID);
            return Success(result.playerInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), result.configData?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.Array(result.goldPacks.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("trwr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_TakeRewardForWinningRounds(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).TakeRewardForWinningRounds();
            return Success(LightMessage.Common.Messages.Param.UInt(result.totalGold), LightMessage.Common.Messages.Param.TimeSpan(result.timeUntilNextReward));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("inf")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_ActivateInfinitePlay(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).ActivateInfinitePlay();
            return Success(LightMessage.Common.Messages.Param.Boolean(result.success), LightMessage.Common.Messages.Param.UInt(result.totalGold), LightMessage.Common.Messages.Param.TimeSpan(result.duration));
        }

        protected abstract System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<LeaderBoardEntryDTO>> GetLeaderBoard(System.Guid clientID, LeaderBoardSubject subject, LeaderBoardGroup group);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("lb")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetLeaderBoard(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetLeaderBoard(input.ClientID, array[0].AsUEnum<LeaderBoardSubject>().Value, array[1].AsUEnum<LeaderBoardGroup>().Value);
            return Success(LightMessage.Common.Messages.Param.Array(result.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("bgp")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_BuyGoldPack(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).ProcessGoldPackPurchase(array[0].AsString, array[1].AsString);
            return Success(LightMessage.Common.Messages.Param.UEnum(result.result), LightMessage.Common.Messages.Param.UInt(result.totalGold));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("ne")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SetNotificationsEnabled(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await GrainFactory.GetGrain<IPlayer>(input.ClientID).SetNotificationsEnabled(array[0].AsBoolean.Value);
            return NoResult();
        }

        protected abstract System.Threading.Tasks.Task<System.Guid?> Login(System.Guid clientID, string email, string password);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("lg")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_Login(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await Login(input.ClientID, array[0].AsString, array[1].AsString);
            return Success(LightMessage.Common.Messages.Param.Guid(result));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("reg")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_PerformRegistration(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).PerformRegistration(array[0].AsString, array[1].AsString, array[2].AsString);
            return Success(LightMessage.Common.Messages.Param.UEnum(result));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("unm")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SetUsername(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).SetUsername(array[0].AsString);
            return Success(LightMessage.Common.Messages.Param.Boolean(result));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("eml")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SetEmail(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).SetEmail(array[0].AsString);
            return Success(LightMessage.Common.Messages.Param.UEnum(result));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("pwd")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_UpdatePassword(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).UpdatePassword(array[0].AsString);
            return Success(LightMessage.Common.Messages.Param.UEnum(result));
        }

        protected abstract System.Threading.Tasks.Task SendPasswordRecoveryLink(System.Guid clientID, string email);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("pwdrl")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SendPasswordRecoveryLink(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await SendPasswordRecoveryLink(input.ClientID, array[0].AsString);
            return NoResult();
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("fcm")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_RegisterFcmToken(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await GrainFactory.GetGrain<IPlayer>(input.ClientID).SetFcmToken(array[0].AsString);
            return NoResult();
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("cfg")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_ClearFinishedGames(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await GrainFactory.GetGrain<IPlayer>(input.ClientID).ClearFinishedGames();
            return NoResult();
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("tutp")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_SetTutorialProgress(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await GrainFactory.GetGrain<IPlayer>(input.ClientID).SetTutorialProgress(array[0].AsUInt.Value);
            return Success();
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
        public virtual System.Threading.Tasks.Task<bool> SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfoDTO opponentInfo, System.TimeSpan? expiryTimeRemaining) => SendMessage(clientID, "opj", LightMessage.Common.Messages.Param.Guid(gameID), opponentInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.TimeSpan(expiryTimeRemaining));
        public virtual System.Threading.Tasks.Task<bool> SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO>? wordsPlayed, System.TimeSpan? expiryTimeRemaining) => SendMessage(clientID, "opr", LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.UInt(roundNumber), LightMessage.Common.Messages.Param.Array(wordsPlayed?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())), LightMessage.Common.Messages.Param.TimeSpan(expiryTimeRemaining));
        public virtual System.Threading.Tasks.Task<bool> SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold) => SendMessage(clientID, "gend", LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.UInt(myScore), LightMessage.Common.Messages.Param.UInt(theirScore), LightMessage.Common.Messages.Param.UInt(myPlayerScore), LightMessage.Common.Messages.Param.UInt(myPlayerRank), LightMessage.Common.Messages.Param.UInt(myLevel), LightMessage.Common.Messages.Param.UInt(myXP), LightMessage.Common.Messages.Param.UInt(myGold));
        public virtual System.Threading.Tasks.Task<bool> SendGameExpired(System.Guid clientID, System.Guid gameID, bool myWin, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold) => SendMessage(clientID, "gexp", LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.Boolean(myWin), LightMessage.Common.Messages.Param.UInt(myPlayerScore), LightMessage.Common.Messages.Param.UInt(myPlayerRank), LightMessage.Common.Messages.Param.UInt(myLevel), LightMessage.Common.Messages.Param.UInt(myXP), LightMessage.Common.Messages.Param.UInt(myGold));
        protected abstract System.Threading.Tasks.Task<(System.Guid gameID, PlayerInfoDTO? opponentInfo, byte numRounds, bool myTurnFirst, System.TimeSpan? expiryTimeRemaining)> NewGame(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("new")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_NewGame(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await NewGame(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.Guid(result.gameID), result.opponentInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(result.numRounds), LightMessage.Common.Messages.Param.Boolean(result.myTurnFirst), LightMessage.Common.Messages.Param.TimeSpan(result.expiryTimeRemaining));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("rnd")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_StartRound(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IGame>(array[0].AsGuid.Value).StartRound(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.String(result.category), LightMessage.Common.Messages.Param.Boolean(result.haveAnswers), LightMessage.Common.Messages.Param.TimeSpan(result.roundTime), LightMessage.Common.Messages.Param.Boolean(result.mustChooseGroup), LightMessage.Common.Messages.Param.Array(result.groups?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("cgr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_ChooseGroup(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IGame>(array[0].AsGuid.Value).ChooseGroup(input.ClientID, (ushort)array[1].AsUInt.Value);
            return Success(LightMessage.Common.Messages.Param.String(result.category), LightMessage.Common.Messages.Param.Boolean(result.haveAnswers), LightMessage.Common.Messages.Param.TimeSpan(result.roundTime));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("rgr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_RefreshGroups(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).RefreshGroups(array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.Array(result?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("word")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_PlayWord(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IGame>(array[0].AsGuid.Value).PlayWord(input.ClientID, array[1].AsString);
            return Success(LightMessage.Common.Messages.Param.UInt(result.wordScore), LightMessage.Common.Messages.Param.String(result.corrected));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("irt")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_IncreaseRoundTime(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).IncreaseRoundTime(array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.UInt(result.gold), LightMessage.Common.Messages.Param.TimeSpan(result.remainingTime));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("rvw")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_RevealWord(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).RevealWord(array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.UInt(result.gold), LightMessage.Common.Messages.Param.String(result.word), LightMessage.Common.Messages.Param.UInt(result.wordScore));
        }

        protected abstract System.Threading.Tasks.Task<(System.Collections.Generic.IEnumerable<WordScorePairDTO>? opponentWords, System.TimeSpan? expiryTimeRemaining)> EndRound(System.Guid clientID, System.Guid gameID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("endr")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_EndRound(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await EndRound(input.ClientID, array[0].AsGuid.Value);
            return Success(LightMessage.Common.Messages.Param.Array(result.opponentWords?.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())), LightMessage.Common.Messages.Param.TimeSpan(result.expiryTimeRemaining));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("info")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetGameInfo(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IGame>(array[0].AsGuid.Value).GetGameInfo(input.ClientID);
            return Success(result?.ToParam() ?? LightMessage.Common.Messages.Param.Null());
        }

        protected abstract System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<SimplifiedGameInfoDTO>> GetAllGames(System.Guid clientID);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("all")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetAllGames(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GetAllGames(input.ClientID);
            return Success(LightMessage.Common.Messages.Param.Array(result.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())));
        }

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("ans")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_GetAnswers(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            var result = await GrainFactory.GetGrain<IPlayer>(input.ClientID).GetAnswers(array[0].AsString);
            return Success(LightMessage.Common.Messages.Param.Array(result.words.Select(a => LightMessage.Common.Messages.Param.String(a))), LightMessage.Common.Messages.Param.UInt(result.totalGold));
        }

        protected abstract System.Threading.Tasks.Task Vote(System.Guid clientID, string category, bool up);

        [LightMessage.OrleansUtils.GrainInterfaces.MethodNameAttribute("vote")]
        async System.Threading.Tasks.Task<LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionResult> EndPoint_Vote(LightMessage.OrleansUtils.GrainInterfaces.EndPointFunctionParams input)
        {
            var array = input.Args;
            await Vote(input.ClientID, array[0].AsString, array[1].AsBoolean.Value);
            return NoResult();
        }
    }

    public class LightMessageHost
    {
        LightMessage.OrleansUtils.Host.LightMessageOrleansHost host = new LightMessage.OrleansUtils.Host.LightMessageOrleansHost();

        public int ConnectedClientCount => host.ConnectedClientCount;

        public delegate System.Threading.Tasks.Task<System.Guid?> ClientAuthCallbackDelegate(HandShakeMode mode, System.Guid? clientID, string? email, string? password);

        ClientAuthCallbackDelegate onClientAuthCallback;

        public System.Threading.Tasks.Task Start(Orleans.IGrainFactory grainFactory, System.Net.IPEndPoint ipEndPoint, ClientAuthCallbackDelegate onClientAuthCallback, LightMessage.Common.Util.ILogProvider logProvider = null)
        {
            this.onClientAuthCallback = onClientAuthCallback;
            return host.Start(grainFactory, ipEndPoint, OnClientAuthRequest, logProvider);
        }

        public void Stop() => host.Stop();
        System.Threading.Tasks.Task<System.Guid?> OnClientAuthRequest(LightMessage.Common.ProtocolMessages.AuthRequestMessage message) => onClientAuthCallback(message.Params[0].AsUEnum<HandShakeMode>().Value, message.Params[1].AsGuid, message.Params[2].AsString, message.Params[3].AsString);
    }
}