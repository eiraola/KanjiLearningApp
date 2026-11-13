using System;
using System.Collections.Generic;

namespace Kanji.Core.Words
{
    /// <summary>What a paste would do to a list, computed before anything is saved.</summary>
    public sealed class WordImportPlan
    {
        public WordImportPlan(WordParseResult parse, int newCount, IReadOnlyList<WordEntry> duplicates)
        {
            Parse = parse ?? throw new ArgumentNullException(nameof(parse));
            NewCount = newCount;
            Duplicates = duplicates ?? Array.Empty<WordEntry>();
        }

        public WordParseResult Parse { get; }
        public WordParseError Error => Parse.Error;
        public IReadOnlyList<WordEntry> Incoming => Parse.Entries;
        public int InvalidLines => Parse.InvalidLines;

        /// <summary>Distinct words that are not in the list yet.</summary>
        public int NewCount { get; }

        /// <summary>
        /// One entry per repeated Word (already in the list, or repeated inside the paste), in paste order,
        /// holding the incoming data that "Update" would write.
        /// </summary>
        public IReadOnlyList<WordEntry> Duplicates { get; }

        public bool HasDuplicates => Duplicates.Count > 0;
    }

    public readonly struct WordImportOutcome
    {
        public WordImportOutcome(int added, int updated, int ignored, int invalidLines)
        {
            Added = added;
            Updated = updated;
            Ignored = ignored;
            InvalidLines = invalidLines;
        }

        public int Added { get; }
        public int Updated { get; }
        public int Ignored { get; }
        public int InvalidLines { get; }
    }

    /// <summary>Pure merge rules for pasting words into a list.</summary>
    public static class WordImport
    {
        public static WordImportPlan Plan(IReadOnlyList<WordEntry> existing, WordParseResult parse)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            if (existing != null) foreach (WordEntry w in existing) known.Add(w.Key);

            var seenNew = new HashSet<string>(StringComparer.Ordinal);
            var dupIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            var duplicates = new List<WordEntry>();
            foreach (WordEntry w in parse.Entries)
            {
                if (!known.Contains(w.Key) && seenNew.Add(w.Key)) continue;
                // Repeated: keep the last incoming data for that word (what Update writes).
                if (dupIndex.TryGetValue(w.Key, out int i)) duplicates[i] = w;
                else
                {
                    dupIndex[w.Key] = duplicates.Count;
                    duplicates.Add(w);
                }
            }
            return new WordImportPlan(parse, seenNew.Count, duplicates);
        }

        /// <summary>
        /// Appends new words in paste order. Repeated words: <paramref name="update"/> overwrites them in place
        /// with the last incoming data; otherwise the first version (existing or first in the paste) is kept.
        /// </summary>
        public static IReadOnlyList<WordEntry> Merge(IReadOnlyList<WordEntry> existing, IReadOnlyList<WordEntry> incoming,
            bool update, out WordImportOutcome outcome, int invalidLines = 0)
        {
            var result = new List<WordEntry>(existing ?? Array.Empty<WordEntry>());
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < result.Count; i++) index[result[i].Key] = i;

            int added = 0;
            var repeated = new HashSet<string>(StringComparer.Ordinal);
            foreach (WordEntry w in incoming ?? Array.Empty<WordEntry>())
            {
                if (index.TryGetValue(w.Key, out int at))
                {
                    repeated.Add(w.Key);
                    if (update) result[at] = w.WithState(result[at].Progress, result[at].ReverseProgress, result[at].Difficult); // keep study state
                    continue;
                }
                index[w.Key] = result.Count;
                result.Add(w);
                added++;
            }

            outcome = new WordImportOutcome(added, update ? repeated.Count : 0, update ? 0 : repeated.Count, invalidLines);
            return result;
        }
    }
}
