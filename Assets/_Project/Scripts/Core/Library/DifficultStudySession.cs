using System;
using System.Collections.Generic;
using Kanji.Core.Model;
using Kanji.Core.Reading;

namespace Kanji.Core.Library
{
    /// <summary>One pass over a story's difficult list in random order. Each answer updates the kanji's progress at once.</summary>
    public sealed class DifficultStudySession
    {
        private readonly DifficultKanjiList _list;
        private readonly List<KanjiEntry> _order;
        private readonly List<KanjiEntry> _failed = new List<KanjiEntry>();
        private int _index;

        public DifficultStudySession(DifficultKanjiList list, Random random = null)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _order = new List<KanjiEntry>(list.Items);
            random = random ?? new Random();
            for (int i = _order.Count - 1; i > 0; i--) // Fisher-Yates
            {
                int j = random.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }

        public IReadOnlyList<KanjiEntry> Order => _order;

        public int Answered => _index;

        public bool IsFinished => _index >= _order.Count;

        public KanjiEntry Current => IsFinished ? null : _order[_index];

        /// <summary>Records the answer for <see cref="Current"/> and moves on. False if the progress could not be saved (the answer still counts).</summary>
        public bool Record(bool knew)
        {
            if (IsFinished) throw new InvalidOperationException("Study session already finished.");
            KanjiEntry entry = _order[_index++];
            if (!knew) _failed.Add(entry);
            return _list.RecordStudy(entry, knew);
        }

        /// <summary>Total = answered kanji; failed = red answers.</summary>
        public ReadingStats BuildStats() => new ReadingStats(_index, new List<KanjiEntry>(_failed));
    }
}
