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


        public byte GetWordScoreWithoutCorrection(string word)
        {
            return WordsAndScores.TryGetValue(word, out var score) ? score : (byte)0;
        }
    }
}
