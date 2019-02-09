using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    struct WordEntry
    {
        public string Word { get; } // The word we're considering
        public string CorrectedWord { get; } // (optional) The word it maps to


        public WordEntry(string word, string correctedWord)
        {
            Word = word;
            CorrectedWord = correctedWord;
        }
    }

    public class WordCategory
    {
        Dictionary<string, WordEntry> entries;


        public string CategoryName { get; private set; }


        public WordCategory(string categoryName, Dictionary<string, List<string>> wordsAndScores)
        {
            CategoryName = categoryName;

            entries = new Dictionary<string, WordEntry>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var w in wordsAndScores)
            {
                entries.Add(w.Key, new WordEntry(w.Key, null));
                foreach (var c in w.Value)
                    entries.Add(c, new WordEntry(c, w.Key));
            }
        }

        public string GetCorrectedWord(string word)
        {
            if (entries.TryGetValue(word, out var entry))
                return entry.CorrectedWord ?? entry.Word;

            foreach (var kv in entries)
                if (Utility.EditDistanceLessThan(word, kv.Key, 2)) //?? max distance as parameter
                    return kv.Value.CorrectedWord ?? kv.Value.Word;

            return null;
        }
    }
}
