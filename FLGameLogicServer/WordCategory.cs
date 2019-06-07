﻿using System;
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
        HashSet<string> answers;


        public string CategoryName { get; private set; }

        public IEnumerable<string> Answers => answers;


        public WordCategory(string categoryName, Dictionary<string, IEnumerable<string>> wordsAndCorrections)
        {
            CategoryName = categoryName;

            entries = new Dictionary<string, WordEntry>(StringComparer.InvariantCultureIgnoreCase);
            answers = new HashSet<string>();

            foreach (var w in wordsAndCorrections)
            {
                entries.Add(w.Key, new WordEntry(w.Key, null));
                answers.Add(w.Key);
                foreach (var correction in w.Value)
                    entries.Add(correction, new WordEntry(correction, w.Key));
            }
        }

        public string GetCorrectedWord(string word, Func<int, int> getMaxEditDistance)
        {
            if (entries.TryGetValue(word, out var entry))
                return entry.CorrectedWord ?? entry.Word;

            foreach (var kv in entries)
                if (EditDistance.IsLessThan(word, kv.Key, getMaxEditDistance(Math.Min(word.Length, kv.Key.Length))))
                    return kv.Value.CorrectedWord ?? kv.Value.Word;

            return null;
        }
    }
}
