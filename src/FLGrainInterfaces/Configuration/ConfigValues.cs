using System;
using System.Collections.Generic;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class ConfigValues
    {
        public byte NumRoundsPerGame { get; private set; }
        public byte NumGroupChoices { get; private set; }
        public uint PriceToRefreshGroups { get; private set; }
        public TimeSpan ClientTimePerRound { get; private set; }
        public TimeSpan ExtraTimePerRound { get; private set; }

        public TimeSpan RoundTimeExtension { get; private set; }
        public IReadOnlyList<uint>? RoundTimeExtensionPrices { get; private set; }
        public uint NumTimeExtensionsPerRound { get; private set; }

        public IReadOnlyList<uint>? RevealWordPrices { get; private set; }

        public uint GetAnswersPrice { get; private set; }

        public TimeSpan GameInactivityTimeout { get; private set; }
        public uint? GameExpiryGoldPenalty { get; private set; }
        public uint? GameExpiryScorePenalty { get; private set; }

        public uint MaxActiveGames { get; private set; }
        public uint UpgradedActiveGameLimitPrice { get; private set; }
        public TimeSpan UpgradedActiveGameLimitTime { get; private set; }
        public uint MaxActiveGamesWhenUpgraded { get; private set; }

        public float WordScoreThreshold2 { get; private set; }
        public float WordScoreThreshold3 { get; private set; }

        public byte NumRoundsToWinToGetReward { get; private set; }
        public TimeSpan RoundWinRewardInterval { get; private set; }
        public uint NumGoldRewardForWinningRounds { get; private set; }

        public uint MatchmakingScoreDifference { get; private set; }
        public uint MatchmakingLevelDifference { get; private set; }
        public TimeSpan MatchMakingWaitBeforeBotMatch { get; private set; }
        public TimeSpan MatchMakingWindowExpansionInterval { get; private set; }
        public uint MatchMakingScorePerExpansion { get; private set; }
        public uint MatchMakingLevelPerExpansion { get; private set; }

        public uint MaxScoreGain { get; private set; }
        public uint MinScoreGain { get; private set; }
        public float LoserScoreLossRatio { get; private set; }

        public uint WinnerXPGain { get; private set; }
        public uint LoserXPGain { get; private set; }
        public uint DrawXPGain { get; private set; }

        public uint WinnerGoldGain { get; private set; }
        public uint LoserGoldGain { get; private set; }
        public uint DrawGoldGain { get; private set; }

        public uint MaxGameHistoryEntries { get; private set; }

        public byte RefreshGroupsAllowedPerRound { get; private set; }

        public uint InitialGold { get; private set; }

        public uint LeaderBoardTopScoreCount { get; private set; }
        public uint LeaderBoardAroundScoreCount { get; private set; }

        public uint VideoAdGold { get; private set; }
        public VideoAdLimitConfig CoinRewardVideo { get; private set; }
        public VideoAdLimitConfig GetCategoryAnswersVideo { get; private set; }

        IReadOnlyList<TimeFrame> notificationTimeFrames = Array.Empty<TimeFrame>();
        public IReadOnlyList<TimeFrame> NotificationTimeFrames
        {
            get => notificationTimeFrames;
            private set => notificationTimeFrames = value.OrderBy(x => x).ToList();
        }

        public IReadOnlyList<uint>? InactivityNotificationIntervalsInDays { get; private set; }

        public uint InviterReward { get; private set; }
        public uint InviteeReward { get; private set; }

        public uint MaxMatchResultHistoryEntries { get; private set; }

        public uint BotMatchOutcomeNumMatches { get; private set; }
        public int BotMatchOutcomeWinThreshold { get; private set; }
        public int BotMatchOutcomeLossThreshold { get; private set; }


        public double BotPlayMinWaitMinutes { get; private set; }
        public double BotPlayMaxWaitMinutes { get; private set; }

        public uint TutorialGamesCount { get; private set; }

        public static void Validate(ConfigValues data)
        {
            Validation.CheckNotDefaultStruct(data.ClientTimePerRound, "client time per round");
            Validation.CheckNotDefaultStruct(data.DrawGoldGain, "draw gold gain");
            Validation.CheckNotDefaultStruct(data.DrawXPGain, "draw XP gain");
            Validation.CheckNotDefaultStruct(data.ExtraTimePerRound, "extra time per round");
            Validation.CheckNotDefaultStruct(data.GameInactivityTimeout, "game inactivity timeout");
            Validation.CheckNotDefaultStruct(data.GameExpiryGoldPenalty, "game expiry gold penalty");
            Validation.CheckNotDefaultStruct(data.GameExpiryScorePenalty, "game expiry score penalty");
            Validation.CheckNotDefaultStruct(data.UpgradedActiveGameLimitPrice, "max active games when upgraded");
            Validation.CheckNotDefaultStruct(data.MaxActiveGamesWhenUpgraded, "upgraded active game limit price");
            Validation.CheckNotDefaultStruct(data.UpgradedActiveGameLimitTime, "upgraded active game limit time");
            Validation.CheckNotDefaultStruct(data.LoserScoreLossRatio, "loser score loss ratio");
            Validation.CheckNotDefaultStruct(data.MaxActiveGames, "max active games");
            Validation.CheckNotDefaultStruct(data.MaxScoreGain, "max score gain");
            Validation.CheckNotDefaultStruct(data.MinScoreGain, "min score gain");
            Validation.CheckNotDefaultStruct(data.NumGoldRewardForWinningRounds, "gold reward for winning rounds");
            Validation.CheckNotDefaultStruct(data.NumGroupChoices, "group choices");
            Validation.CheckNotDefaultStruct(data.NumRoundsPerGame, "rounds per game");
            Validation.CheckNotDefaultStruct(data.NumRoundsToWinToGetReward, "rounds to win to get reward");
            Validation.CheckNotDefaultStruct(data.NumTimeExtensionsPerRound, "time extensions per round");
            Validation.CheckNotDefaultStruct(data.PriceToRefreshGroups, "refresh group price");
            Validation.CheckNotDefaultStruct(data.RoundTimeExtension, "round time extension amount");
            Validation.CheckNotDefaultStruct(data.RoundWinRewardInterval, "win reward interval");
            Validation.CheckNotDefaultStruct(data.WinnerGoldGain, "winner gold gain");
            Validation.CheckNotDefaultStruct(data.WinnerXPGain, "winner XP gain");
            Validation.CheckNotDefaultStruct(data.LoserXPGain, "loser XP gain");
            Validation.CheckNotDefaultStruct(data.WordScoreThreshold2, "word score threshold 2");
            Validation.CheckNotDefaultStruct(data.WordScoreThreshold3, "word score threshold 3");
            Validation.CheckNotDefaultStruct(data.VideoAdGold, "video ad gold");
            Validation.CheckNotDefaultStruct(data.GetAnswersPrice, "answers prices");
            Validation.CheckNotDefaultStruct(data.MaxGameHistoryEntries, "maximum game history entries");
            Validation.CheckNotDefaultStruct(data.RefreshGroupsAllowedPerRound, "refresh groups allowed per round");
            Validation.CheckNotDefaultStruct(data.InitialGold, "initial gold");
            Validation.CheckNotDefaultStruct(data.LeaderBoardTopScoreCount, "leader board top score count");
            Validation.CheckNotDefaultStruct(data.LeaderBoardAroundScoreCount, "leader board around score count");

            Validation.CheckNotDefaultStruct(data.MatchmakingLevelDifference, "matchmaking level difference");
            Validation.CheckNotDefaultStruct(data.MatchmakingScoreDifference, "matchmaking score difference");
            Validation.CheckNotDefaultStruct(data.MatchMakingWaitBeforeBotMatch, "match-making wait before bot match");
            Validation.CheckNotDefaultStruct(data.MatchMakingWindowExpansionInterval, "match-making window expansion interval");
            Validation.CheckNotDefaultStruct(data.MatchMakingScorePerExpansion, "match-making score per expansion");
            Validation.CheckNotDefaultStruct(data.MatchMakingLevelPerExpansion, "match-making level per expansion");

            Validation.CheckList(data.RevealWordPrices, "reveal word prices");
            Validation.CheckList(data.RoundTimeExtensionPrices, "round time extension price");
            Validation.CheckList(data.NotificationTimeFrames, "notification time frame");

            if (data.NotificationTimeFrames != null)
                foreach (var (index, frame) in data.NotificationTimeFrames.Select((f, i) => (i, f)))
                    frame.Validate($"notification time frame #{index}");

            Validation.CheckList(data.InactivityNotificationIntervalsInDays, "inactivity notification intervals");

            Validation.CheckNotDefaultStruct(data.InviterReward, "inviter reward");
            Validation.CheckNotDefaultStruct(data.InviteeReward, "invitee reward");

            Validation.CheckNotDefaultStruct(data.MaxMatchResultHistoryEntries, "max match result history entries");

            Validation.CheckNotDefaultStruct(data.BotMatchOutcomeNumMatches, "bot match outcome num matches");
            Validation.CheckNotDefaultStruct(data.BotMatchOutcomeWinThreshold, "bot match outcome win threshold");
            Validation.CheckNotDefaultStruct(data.BotMatchOutcomeLossThreshold, "bot match outcome loss threshold");

            if (data.BotPlayMinWaitMinutes < 1.0f)
                Validation.FailWith("Bot play min time must be at least 1 minute");
            if (data.BotPlayMaxWaitMinutes < 1.0f)
                Validation.FailWith("Bot play max time must be at least 1 minute");
            if (data.BotPlayMaxWaitMinutes <= data.BotPlayMinWaitMinutes)
                Validation.FailWith("Bot play max time must be more than bot play min time");

            Validation.CheckNotDefaultStruct(data.TutorialGamesCount, "Tutorial games count");

            data.CoinRewardVideo.Validate("coin reward video");
            data.GetCategoryAnswersVideo.Validate("get category answers video");
        }
    }
}
