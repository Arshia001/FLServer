using System;

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
        public uint RoundTimeExtensionPrice { get; private set; }
        public uint NumTimeExtensionsPerRound { get; private set; }

        public uint RevealWordPrice { get; private set; }

        public uint GetAnswersPrice { get; private set; }

        public TimeSpan GameInactivityTimeout { get; private set; }

        public uint MaxActiveGames { get; private set; }
        public uint InfinitePlayPrice { get; private set; }
        public TimeSpan InfinitePlayTime { get; private set; }

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

        public static void FailWith(string error) => throw new ArgumentException(error);

        public static void Validate(ConfigValues data)
        {
            static void CheckNotEqual<T>(T t, T test, string name) where T : notnull
            {
                if (t.Equals(test))
                    FailWith($"{name} shouldn't be {test}");
            }

            CheckNotEqual(data.ClientTimePerRound, TimeSpan.Zero, "client time per round");
            CheckNotEqual(data.DrawGoldGain, 0u, "draw gold gain");
            CheckNotEqual(data.DrawXPGain, 0u, "draw XP gain");
            CheckNotEqual(data.ExtraTimePerRound, TimeSpan.Zero, "extra time per round");
            CheckNotEqual(data.GameInactivityTimeout, TimeSpan.Zero, "game inactivity timeout");
            CheckNotEqual(data.GetAnswersPrice, 0u, "answers price");
            CheckNotEqual(data.InfinitePlayPrice, 0u, "infinite play price");
            CheckNotEqual(data.InfinitePlayTime, TimeSpan.Zero, "infinite play time");
            CheckNotEqual(data.LoserScoreLossRatio, 0, "loser score loss ratio");
            CheckNotEqual(data.MatchmakingLevelDifference, 0u, "matchmaking level difference");
            CheckNotEqual(data.MatchmakingScoreDifference, 0u, "matchmaking score difference");
            CheckNotEqual(data.MaxActiveGames, 0u, "max active games");
            CheckNotEqual(data.MaxScoreGain, 0u, "max score gain");
            CheckNotEqual(data.NumGoldRewardForWinningRounds, 0u, "gold reward for winning rounds");
            CheckNotEqual(data.NumGroupChoices, 0u, "group choices");
            CheckNotEqual(data.NumRoundsPerGame, 0u, "rounds per game");
            CheckNotEqual(data.NumRoundsToWinToGetReward, 0u, "rounds to win to get reward");
            CheckNotEqual(data.NumTimeExtensionsPerRound, 0u, "time extensions per round");
            CheckNotEqual(data.PriceToRefreshGroups, 0u, "refresh group price");
            CheckNotEqual(data.RevealWordPrice, 0u, "reveal word price");
            CheckNotEqual(data.RoundTimeExtension, TimeSpan.Zero, "round time extension amount");
            CheckNotEqual(data.RoundTimeExtensionPrice, 0u, "round time extension price");
            CheckNotEqual(data.RoundWinRewardInterval, TimeSpan.Zero, "win reward interval");
            CheckNotEqual(data.WinnerGoldGain, 0u, "winner gold gain");
            CheckNotEqual(data.WinnerXPGain, 0u, "winner XP gain");
            CheckNotEqual(data.WordScoreThreshold2, 0, "word score threshold 2");
            CheckNotEqual(data.WordScoreThreshold3, 0, "word score threshold 3");
            CheckNotEqual(data.VideoAdGold, 0u, "video ad gold");

            data.CoinRewardVideo.Validate("coin reward video");
            data.GetCategoryAnswersVideo.Validate("get category answers video");
        }
    }
}
