using System;
using System.Collections.Generic;
using System.Text;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrainInterfaces.Utility;

namespace FLGrains.Utility
{
    static class ScoreGainCalculator
    {
        public static uint CalculateGain(uint playerScore1, uint gameScore1, uint playerScore2, uint gameScore2, ReadOnlyConfigData config)
        {
            uint winnerScore, loserScore;

            switch (CompetitionResultHelper.Get(gameScore1, gameScore2))
            {
                case CompetitionResult.Win:
                    winnerScore = playerScore1;
                    loserScore = playerScore2;
                    break;

                case CompetitionResult.Loss:
                    winnerScore = playerScore2;
                    loserScore = playerScore1;
                    break;

                case CompetitionResult.Draw:
                default:
                    return 0;
            }

            return (uint)Math.Round(
                FLMath.Lerp(config.ConfigValues.MinScoreGain, config.ConfigValues.MaxScoreGain,
                    FLMath.Clamp01(
                        FLMath.InverseLerp(-config.ConfigValues.MatchmakingScoreDifference, config.ConfigValues.MatchmakingScoreDifference, loserScore - winnerScore)
                    )
                )
            );
        }
    }
}
