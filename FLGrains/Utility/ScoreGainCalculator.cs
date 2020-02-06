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
        public static uint CalculateGain(uint playerScore1, uint playerScore2, CompetitionResult gameResult1To2, ReadOnlyConfigData config)
        {
            uint winnerScore, loserScore;

            switch (gameResult1To2)
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
                        FLMath.InverseLerp(-config.ConfigValues.MatchmakingScoreDifference,
                            config.ConfigValues.MatchmakingScoreDifference, (int)loserScore - (int)winnerScore)
                    )
                )
            );
        }
    }
}
