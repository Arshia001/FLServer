using System;
using System.Collections.Generic;
using System.Text;
using Bond;

namespace FLGameLogicServer
{
    [Schema]
    public class SerializedGameData
    {
        [Schema]
        public struct WordScorePair
        {
            [Id(0)]
            public string word;
            [Id(1)]
            public byte score;

            public WordScorePair(string word, byte score)
            {
                this.word = word;
                this.score = score;
            }
        }

        [Id(0)]
        public List<string> CategoryNames { get; set; }
        [Id(1)]
        public List<List<WordScorePair>>[] PlayerAnswers { get; set; }
        [Id(2)]
        public List<uint>[] PlayerScores { get; set; }
        [Id(3)]
        public DateTime[] TurnEndTimes { get; set; }
        [Id(4)]
        public int FirstTurn { get; set; }
    }
}
