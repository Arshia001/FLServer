using System.Collections.Generic;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class CategoryConfig
    {
        public class Entry
        {
            public string Word { get; }
            public IReadOnlyList<string> Corrections { get; }

            public Entry(string word, IEnumerable<string> corrections)
            {
                Word = word;
                Corrections = corrections.ToList();
            }
        }

        public string Name { get; }
        public IReadOnlyList<Entry> Words { get; }
        public GroupConfig Group { get; }

        public CategoryConfig(string name, IEnumerable<Entry> words, GroupConfig group)
        {
            Name = name;
            Words = words.ToList();
            Group = group;
        }
    }
}
