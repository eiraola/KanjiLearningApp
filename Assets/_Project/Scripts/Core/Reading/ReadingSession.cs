using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Reading
{
    public enum KanjiAnswer { Knew, DidntKnow }

    /// <summary>Answers given while reading one story. Not persisted (GDD 4); the last answer per kanji wins.</summary>
    public sealed class ReadingSession
    {
        private readonly Story _story;
        private readonly Dictionary<KanjiKey, KanjiAnswer> _answers = new Dictionary<KanjiKey, KanjiAnswer>();

        public ReadingSession(Story story)
        {
            _story = story ?? throw new ArgumentNullException(nameof(story));
        }

        public void Record(KanjiEntry entry, KanjiAnswer answer)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _answers[entry.Key] = answer;
        }

        /// <summary>Unanswered kanji count as passed; only a red answer is a failure.</summary>
        public ReadingStats BuildStats()
        {
            var seen = new HashSet<KanjiKey>();
            var failed = new List<KanjiEntry>();
            int total = 0;
            for (int i = 0; i < _story.Entries.Count; i++)
            {
                KanjiEntry e = _story.Entries[i];
                if (!seen.Add(e.Key)) continue;
                total++;
                if (_answers.TryGetValue(e.Key, out KanjiAnswer a) && a == KanjiAnswer.DidntKnow) failed.Add(e);
            }
            return new ReadingStats(total, failed);
        }
    }
}
