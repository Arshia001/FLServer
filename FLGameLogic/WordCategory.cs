using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    public class WordCategory
    {
        public string CategoryName { get; set; }

        public Dictionary<string, byte> WordsAndScores { get; set; }

        public Dictionary<string, string> WordCorrections { get; set; } //?? maybe also have an automatic version that e.g. replaces characters with correct but same-sounding ones?


        public byte GetScoreWithoutCorrection(string word)
        {
            byte score;
            return WordsAndScores.TryGetValue(word, out score) ? score : (byte)0;
        }

        public byte GetScore(string word, out string corrected)
        {
            corrected = null;

            byte score;
            if (WordsAndScores.TryGetValue(word, out score))
                return score;

            if (WordCorrections.TryGetValue(word, out corrected))
                return score;

            return 0;
        }
    }
}
