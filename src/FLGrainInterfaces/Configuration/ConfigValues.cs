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

        IReadOnlyList<TimeFrame>? notificationTimeFrames;
        public IReadOnlyList<TimeFrame>? NotificationTimeFrames
        {
            get => notificationTimeFrames;
            private set => notificationTimeFrames = value.OrderBy(x => x).ToList();
        }

        public uint InviterReward { get; private set; }
        public uint InviteeReward { get; private set; }

        public uint MaxMatchResultHistoryEntries { get; private set; }

        public static void Validate(ConfigValues data)
        {
            Validation.CheckNotEqual(data.ClientTimePerRound, TimeSpan.Zero, "client time per round");
            Validation.CheckNotEqual(data.DrawGoldGain, 0u, "draw gold gain");
            Validation.CheckNotEqual(data.DrawXPGain, 0u, "draw XP gain");
            Validation.CheckNotEqual(data.ExtraTimePerRound, TimeSpan.Zero, "extra time per round");
            Validation.CheckNotEqual(data.GameInactivityTimeout, TimeSpan.Zero, "game inactivity timeout");
            Validation.CheckNotEqual(data.GameExpiryGoldPenalty, null, "game expiry gold penalty");
            Validation.CheckNotEqual(data.GameExpiryScorePenalty, null, "game expiry score penalty");
            Validation.CheckNotEqual(data.UpgradedActiveGameLimitPrice, 0u, "max active games when upgraded");
            Validation.CheckNotEqual(data.MaxActiveGamesWhenUpgraded, 0u, "upgraded active game limit price");
            Validation.CheckNotEqual(data.UpgradedActiveGameLimitTime, TimeSpan.Zero, "upgraded active game limit time");
            Validation.CheckNotEqual(data.LoserScoreLossRatio, 0, "loser score loss ratio");
            Validation.CheckNotEqual(data.MatchmakingLevelDifference, 0u, "matchmaking level difference");
            Validation.CheckNotEqual(data.MatchmakingScoreDifference, 0u, "matchmaking score difference");
            Validation.CheckNotEqual(data.MaxActiveGames, 0u, "max active games");
            Validation.CheckNotEqual(data.MaxScoreGain, 0u, "max score gain");
            Validation.CheckNotEqual(data.NumGoldRewardForWinningRounds, 0u, "gold reward for winning rounds");
            Validation.CheckNotEqual(data.NumGroupChoices, 0u, "group choices");
            Validation.CheckNotEqual(data.NumRoundsPerGame, 0u, "rounds per game");
            Validation.CheckNotEqual(data.NumRoundsToWinToGetReward, 0u, "rounds to win to get reward");
            Validation.CheckNotEqual(data.NumTimeExtensionsPerRound, 0u, "time extensions per round");
            Validation.CheckNotEqual(data.PriceToRefreshGroups, 0u, "refresh group price");
            Validation.CheckNotEqual(data.RoundTimeExtension, TimeSpan.Zero, "round time extension amount");
            Validation.CheckNotEqual(data.RoundWinRewardInterval, TimeSpan.Zero, "win reward interval");
            Validation.CheckNotEqual(data.WinnerGoldGain, 0u, "winner gold gain");
            Validation.CheckNotEqual(data.WinnerXPGain, 0u, "winner XP gain");
            Validation.CheckNotEqual(data.WordScoreThreshold2, 0, "word score threshold 2");
            Validation.CheckNotEqual(data.WordScoreThreshold3, 0, "word score threshold 3");
            Validation.CheckNotEqual(data.VideoAdGold, 0u, "video ad gold");
            Validation.CheckNotEqual(data.GetAnswersPrice, 0u, "answers prices");

            Validation.CheckList(data.RevealWordPrices, "reveal word prices");
            Validation.CheckList(data.RoundTimeExtensionPrices, "round time extension price");
            Validation.CheckList(data.NotificationTimeFrames, "notification time frame");

            if (data.NotificationTimeFrames != null)
                foreach (var (index, frame) in data.NotificationTimeFrames.Select((f, i) => (i, f)))
                    frame.Validate($"notification time frame #{index}");

            Validation.CheckNotEqual(data.InviterReward, 0u, "inviter reward");
            Validation.CheckNotEqual(data.InviteeReward, 0u, "invitee reward");

            data.CoinRewardVideo.Validate("coin reward video");
            data.GetCategoryAnswersVideo.Validate("get category answers video");
        }
    }
}
