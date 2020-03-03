using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    public struct WordScorePair
    {
        public string word;
        public byte score;

        public WordScorePair(string word, byte score)
        {
            this.word = word;
            this.score = score;
        }
    }
}
