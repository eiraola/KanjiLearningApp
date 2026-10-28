using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Reading
{
    /// <summary>Result of a reading session, counted over distinct kanji (by KanjiKey).</summary>
    public sealed class ReadingStats
    {
        public ReadingStats(int total, IReadOnlyList<KanjiEntry> failedEntries)
        {
            FailedEntries = failedEntries ?? Array.Empty<KanjiEntry>();
            Total = total;
            Failed = FailedEntries.Count;
        }

        public int Total { get; }
        public int Failed { get; }
        public int Passed => Total - Failed;
        public IReadOnlyList<KanjiEntry> FailedEntries { get; }

        /// <summary>Rounded to the nearest integer; 0 when the story has no kanji.</summary>
        public int Percent => Total == 0 ? 0 : (int)Math.Round(Passed * 100.0 / Total, MidpointRounding.AwayFromZero);
    }
}
