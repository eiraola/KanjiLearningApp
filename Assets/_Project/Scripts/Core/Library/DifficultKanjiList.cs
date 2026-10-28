using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    public enum DifficultAddResult { Added, AlreadyPresent, SaveFailed }

    /// <summary>Difficult kanji of ONE story. Deduplicated by Kanji + Reading (GDD 2.5), insertion order.</summary>
    public sealed class DifficultKanjiList
    {
        private readonly IDifficultKanjiRepository _repo;
        private readonly List<KanjiEntry> _items = new List<KanjiEntry>();
        private readonly HashSet<DifficultKanjiKey> _keys = new HashSet<DifficultKanjiKey>();
        private readonly List<int> _progress = new List<int>(); // study successes, parallel to _items

        /// <summary>Successes needed to fill the progress circle.</summary>
        public const int MaxProgress = 5;

        public DifficultKanjiList(IDifficultKanjiRepository repo, string storyId)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            StoryId = storyId ?? throw new ArgumentNullException(nameof(storyId));
            IReadOnlyList<KanjiEntry> loaded = _repo.Load(storyId);
            if (loaded == null) return;
            IReadOnlyList<int> progress = _repo.LoadProgress(storyId);
            if (progress != null && progress.Count != loaded.Count) progress = null;
            for (int i = 0; i < loaded.Count; i++)
            {
                KanjiEntry e = loaded[i];
                if (e == null || !_keys.Add(KeyOf(e))) continue;
                _items.Add(Copy(e));
                _progress.Add(progress == null ? 0 : Clamp(progress[i]));
            }
        }

        public string StoryId { get; }

        public IReadOnlyList<KanjiEntry> Items => _items;

        /// <summary>Incremented on every successful change.</summary>
        public int Revision { get; private set; }

        public bool Contains(KanjiEntry entry) => entry != null && _keys.Contains(KeyOf(entry));

        public DifficultAddResult Add(KanjiEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (_keys.Contains(KeyOf(entry))) return DifficultAddResult.AlreadyPresent;

            _items.Add(Copy(entry));
            _progress.Add(0);
            if (!TrySave())
            {
                _items.RemoveAt(_items.Count - 1);
                _progress.RemoveAt(_progress.Count - 1);
                return DifficultAddResult.SaveFailed;
            }
            _keys.Add(KeyOf(entry));
            Revision++;
            return DifficultAddResult.Added;
        }

        public bool Remove(KanjiEntry entry)
        {
            if (entry == null) return false;
            DifficultKanjiKey key = KeyOf(entry);
            int index = IndexOf(key);
            if (index < 0) return false;

            KanjiEntry removed = _items[index];
            int removedProgress = _progress[index];
            _items.RemoveAt(index);
            _progress.RemoveAt(index);
            if (!TrySave())
            {
                _items.Insert(index, removed);
                _progress.Insert(index, removedProgress);
                return false;
            }
            _keys.Remove(key);
            Revision++;
            return true;
        }

        /// <summary>Study successes of the entry (0..<see cref="MaxProgress"/>); 0 if not in the list.</summary>
        public int ProgressOf(KanjiEntry entry)
        {
            if (entry == null) return 0;
            int index = IndexOf(KeyOf(entry));
            return index < 0 ? 0 : _progress[index];
        }

        /// <summary>Green adds 1 (max 5); red leaves it unchanged (P9). Saved at once; false if not in the list or the save failed.</summary>
        public bool RecordStudy(KanjiEntry entry, bool knew)
        {
            if (entry == null) return false;
            int index = IndexOf(KeyOf(entry));
            if (index < 0) return false;

            int before = _progress[index];
            int after = Clamp(before + (knew ? 1 : 0));
            if (after == before) return true;
            _progress[index] = after;
            if (!TrySave())
            {
                _progress[index] = before;
                return false;
            }
            Revision++;
            return true;
        }

        private static int Clamp(int value) => value < 0 ? 0 : value > MaxProgress ? MaxProgress : value;

        private int IndexOf(DifficultKanjiKey key)
        {
            for (int i = 0; i < _items.Count; i++)
                if (KeyOf(_items[i]).Equals(key)) return i;
            return -1;
        }

        private static DifficultKanjiKey KeyOf(KanjiEntry e) => new DifficultKanjiKey(e.Key.Value, e.Reading ?? string.Empty);

        private readonly struct DifficultKanjiKey : IEquatable<DifficultKanjiKey>
        {
            private readonly string _kanji;
            private readonly string _reading;

            public DifficultKanjiKey(string kanji, string reading)
            {
                _kanji = kanji;
                _reading = reading.Trim();
            }

            public bool Equals(DifficultKanjiKey o) =>
                string.Equals(_kanji, o._kanji, StringComparison.Ordinal) &&
                string.Equals(_reading, o._reading, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is DifficultKanjiKey k && Equals(k);

            public override int GetHashCode() => unchecked(_kanji.GetHashCode() * 31 + _reading.GetHashCode());
        }

        private bool TrySave() => _repo.Save(StoryId, _items, _progress).IsSuccess;

        private static KanjiEntry Copy(KanjiEntry e) =>
            new KanjiEntry(e.Kanji, e.Reading, e.Meaning, e.Sentence, e.SentenceMeaning);
    }
}
