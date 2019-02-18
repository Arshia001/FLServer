using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    struct WordEntry
    {
        public string Word { get; } // The word we're considering
        public string CorrectedWord { get; } // (optional) The word it maps to
        public byte Score { get; }


        public WordEntry(string word, string correctedWord, byte score)
        {
            Word = word;
            CorrectedWord = correctedWord;
            Score = score;
        }
    }

    public class WordCategory
    {
        Dictionary<string, WordEntry> entries;


        public string CategoryName { get; private set; }


        public WordCategory(string categoryName, Dictionary<string, (byte score, List<string> corrections)> wordsAndScores)
        {
            CategoryName = categoryName;

            entries = new Dictionary<string, WordEntry>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var w in wordsAndScores)
            {
                entries.Add(w.Key, new WordEntry(w.Key, null, w.Value.score));
                foreach (var correction in w.Value.corrections)
                    entries.Add(correction, new WordEntry(correction, w.Key, w.Value.score));
            }
        }

        public (byte score, string corrected) GetScore(string word)
        {
            if (entries.TryGetValue(word, out var entry))
                return (entry.Score, entry.CorrectedWord ?? entry.Word);

            foreach (var kv in entries)
                if (Utility.EditDistanceLessThan(word, kv.Key, 2)) //?? max distance as parameter
                    return (kv.Value.Score, kv.Value.CorrectedWord ?? kv.Value.Word);

            return (0, null);
        }
    }
}
