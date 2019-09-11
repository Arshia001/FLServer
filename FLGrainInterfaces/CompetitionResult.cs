namespace FLGrainInterfaces
{
    public enum CompetitionResult
    {
        Loss,
        Draw,
        Win
    }

    public static class CompetitionResultHelper
    {
        public static CompetitionResult Get(uint myScore, uint opponentScore) =>
            myScore < opponentScore ? CompetitionResult.Loss :
            myScore > opponentScore ? CompetitionResult.Win :
            CompetitionResult.Draw;

        public static CompetitionResult Flip(this CompetitionResult r) =>
            r == CompetitionResult.Win ? CompetitionResult.Loss :
            r == CompetitionResult.Loss ? CompetitionResult.Win :
            CompetitionResult.Draw;
    }
}
