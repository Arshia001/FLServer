﻿using System;

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

        public uint RevealWordPrice { get; private set; }

        public uint GetAnswersPrice { get; private set; }

        public uint MaxActiveGames { get; private set; }
        public uint InfinitePlayPrice { get; private set; }
        public TimeSpan InfinitePlayTime { get; private set; }

        public float WordScoreThreshold2 { get; private set; }
        public float WordScoreThreshold3 { get; private set; }

        public byte NumRoundsToWinToGetReward { get; private set; }
        public TimeSpan RoundWinRewardInterval { get; private set; }
        public uint NumGoldRewardForWinningRounds { get; private set; }

        public int WinDeltaScore { get; private set; }
        public int LossDeltaScore { get; private set; }
        public int DrawDeltaScore { get; private set; }
    }
}