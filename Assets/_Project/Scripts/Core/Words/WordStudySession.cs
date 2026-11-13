using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Core.Reading;

namespace Kanji.Core.Words
{
    /// <summary>One pass over every word of a list in random order. Each answer updates the word's progress at once.</summary>
    public sealed class WordStudySession
    {
        private readonly WordListLibrary _library;
        private readonly string _listId;
        private readonly List<WordEntry> _order;
        private readonly List<WordEntry> _failed = new List<WordEntry>();
        private int _index;

        /// <param name="reverse">S16 reverse mode: answers update <see cref="WordEntry.ReverseProgress"/>.</param>
        public WordStudySession(WordListLibrary library, WordList list, Random random = null, bool reverse = false)
        {
            Reverse = reverse;
            _library = library ?? throw new ArgumentNullException(nameof(library));
            if (list == null) throw new ArgumentNullException(nameof(list));
            _listId = list.Id;
            _order = new List<WordEntry>(list.Words);
            random = random ?? new Random();
            for (int i = _order.Count - 1; i > 0; i--) // Fisher-Yates
            {
                int j = random.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }

        public bool Reverse { get; }
        public string ListId => _listId;
        public IReadOnlyList<WordEntry> Order => _order;
        public int Answered => _index;
        public bool IsFinished => _index >= _order.Count;
        public WordEntry Current => IsFinished ? null : _order[_index];

        /// <summary>Records the answer for <see cref="Current"/> and moves on. False if the progress could not be saved (the answer still counts).</summary>
        public bool Record(bool knew)
        {
            if (IsFinished) throw new InvalidOperationException("Study session already finished.");
            WordEntry entry = _order[_index++];
            if (!knew) _failed.Add(entry);
            return _library.RecordStudy(_listId, entry.Key, knew, Reverse).IsSuccess;
        }

        /// <summary>Same results screen as the stories: total answered, failed words (word + reading).</summary>
        public ReadingStats BuildStats()
        {
            var failed = new List<KanjiEntry>(_failed.Count);
            foreach (WordEntry w in _failed)
                failed.Add(new KanjiEntry(w.Word, w.Reading, w.Meaning, w.Sentence, w.SentenceMeaning));
            return new ReadingStats(_index, failed);
        }
    }
}
