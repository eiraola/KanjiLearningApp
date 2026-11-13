using System;
using System.Collections.Generic;

namespace Kanji.Core.Words
{
    /// <summary>One word of a word list. Identity is <see cref="Key"/>: the trimmed Word, ordinal.</summary>
    public sealed class WordEntry
    {
        public const int MaxProgress = 5;

        public WordEntry(string word, string reading, string meaning, string sentence, string sentenceReading,
            string sentenceMeaning, int progress = 0, bool difficult = false, int reverseProgress = 0)
        {
            Progress = Clamp(progress);
            ReverseProgress = Clamp(reverseProgress);
            Difficult = difficult;
            Word = (word ?? string.Empty).Trim();
            if (Word.Length == 0) throw new ArgumentException("Word must not be empty.", nameof(word));
            Reading = (reading ?? string.Empty).Trim();
            Meaning = (meaning ?? string.Empty).Trim();
            Sentence = (sentence ?? string.Empty).Trim();
            SentenceReading = (sentenceReading ?? string.Empty).Trim();
            SentenceMeaning = (sentenceMeaning ?? string.Empty).Trim();
        }

        public string Word { get; }
        public string Reading { get; }
        public string Meaning { get; }
        public string Sentence { get; }
        public string SentenceReading { get; }
        public string SentenceMeaning { get; }

        public string Key => Word;

        /// <summary>Study successes (0..<see cref="MaxProgress"/>): green adds 1, red leaves it as is.</summary>
        public int Progress { get; }

        /// <summary>Reverse study successes (S16: reading shown, guess word + meaning), 0..<see cref="MaxProgress"/>.</summary>
        public int ReverseProgress { get; }

        /// <summary>In this list's difficult sub-list.</summary>
        public bool Difficult { get; }

        /// <summary>Same data with another study state.</summary>
        public WordEntry WithState(int progress, int reverseProgress, bool difficult) =>
            new WordEntry(Word, Reading, Meaning, Sentence, SentenceReading, SentenceMeaning, progress, difficult, reverseProgress);

        public WordEntry WithDifficult(bool difficult) => WithState(Progress, ReverseProgress, difficult);

        /// <summary>Progress of the given study mode.</summary>
        public int ProgressOf(bool reverse) => reverse ? ReverseProgress : Progress;

        private static int Clamp(int value) => value < 0 ? 0 : value > MaxProgress ? MaxProgress : value;

        public static bool SameKey(WordEntry a, WordEntry b) =>
            a != null && b != null && string.Equals(a.Key, b.Key, StringComparison.Ordinal);
    }

    public sealed class WordList
    {
        public WordList(string id, string name, DateTime createdAtUtc, IReadOnlyList<WordEntry> words)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            Words = words ?? Array.Empty<WordEntry>();
        }

        public string Id { get; }
        public string Name { get; }
        public DateTime CreatedAtUtc { get; }
        public IReadOnlyList<WordEntry> Words { get; }

        public WordList WithWords(IReadOnlyList<WordEntry> words) => new WordList(Id, Name, CreatedAtUtc, words);
    }

    public sealed class WordListInfo
    {
        public WordListInfo(string id, string name, int wordCount, DateTime createdAtUtc)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            WordCount = wordCount;
            CreatedAtUtc = createdAtUtc;
        }

        public string Id { get; }
        public string Name { get; }
        public int WordCount { get; }
        public DateTime CreatedAtUtc { get; }
    }

    public sealed class WordListListing
    {
        public WordListListing(IReadOnlyList<WordListInfo> items, int unreadableCount)
        {
            Items = items ?? Array.Empty<WordListInfo>();
            UnreadableCount = unreadableCount;
        }

        public IReadOnlyList<WordListInfo> Items { get; }

        /// <summary>Number of list files skipped (corrupt, unsupported version, I/O error).</summary>
        public int UnreadableCount { get; }
    }

    public interface IWordListRepository
    {
        WordListListing List();
        Library.RepoResult<WordList> Load(string id);
        Library.RepoResult Save(WordList list);
    }
}
