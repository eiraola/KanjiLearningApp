using System;
using System.Collections.Generic;

namespace Kanji.Core.Model
{
    public enum SegmentKind { Text = 0, Kanji = 1 }

    /// <summary>Piece of a story: plain text or a reference to a kanji entry.</summary>
    public readonly struct Segment
    {
        public SegmentKind Kind { get; }
        public string Text { get; }
        public int EntryIndex { get; }

        private Segment(SegmentKind kind, string text, int entryIndex)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            EntryIndex = entryIndex;
        }

        public static Segment Plain(string text) => new Segment(SegmentKind.Text, text, -1);
        public static Segment ForKanji(string kanji, int entryIndex) => new Segment(SegmentKind.Kanji, kanji, entryIndex);
    }

    public sealed class KanjiEntry
    {
        public KanjiEntry(string kanji, string reading, string meaning, string sentence, string sentenceMeaning)
        {
            Kanji = kanji ?? string.Empty;
            Reading = reading ?? string.Empty;
            Meaning = meaning ?? string.Empty;
            Sentence = sentence ?? string.Empty;
            SentenceMeaning = sentenceMeaning ?? string.Empty;
            Key = new KanjiKey(Kanji);
        }

        public string Kanji { get; }
        public string Reading { get; }
        public string Meaning { get; }
        public string Sentence { get; }
        public string SentenceMeaning { get; }
        public KanjiKey Key { get; }
    }

    /// <summary>Identity of a kanji: only the Kanji field, trimmed, ordinal comparison.</summary>
    public readonly struct KanjiKey : IEquatable<KanjiKey>
    {
        public KanjiKey(string kanji)
        {
            string trimmed = kanji?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new ArgumentException("Kanji must not be null or empty.", nameof(kanji));
            Value = trimmed;
        }

        public string Value { get; }

        public bool Equals(KanjiKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is KanjiKey other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(KanjiKey a, KanjiKey b) => a.Equals(b);
        public static bool operator !=(KanjiKey a, KanjiKey b) => !a.Equals(b);
    }

    public sealed class Story
    {
        public Story(string id, string name, IReadOnlyList<Segment> segments, IReadOnlyList<KanjiEntry> entries,
            DateTime importedAtUtc = default)
        {
            ImportedAtUtc = importedAtUtc == default ? DateTime.MinValue : importedAtUtc;
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Segments = segments ?? Array.Empty<Segment>();
            Entries = entries ?? Array.Empty<KanjiEntry>();

            var keys = new HashSet<KanjiKey>();
            for (int i = 0; i < Entries.Count; i++) keys.Add(Entries[i].Key);
            DistinctKanjiCount = keys.Count;
        }

        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<Segment> Segments { get; }
        public IReadOnlyList<KanjiEntry> Entries { get; }
        public int DistinctKanjiCount { get; }

        /// <summary>UTC import time; DateTime.MinValue when unknown.</summary>
        public DateTime ImportedAtUtc { get; }

        public Story With(string id, string name, DateTime importedAtUtc) =>
            new Story(id, name, Segments, Entries, importedAtUtc);
    }

    public static class StoryName
    {
        public const string DefaultName = "Historia";

        public static string FromFileName(string fileName)
        {
            string s = fileName ?? string.Empty;
            int slash = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
            if (slash >= 0) s = s.Substring(slash + 1);
            int dot = s.LastIndexOf('.');
            if (dot >= 0) s = s.Substring(0, dot);
            s = s.Trim();
            return s.Length == 0 ? DefaultName : s;
        }

        public static bool AreSame(string a, string b) =>
            string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
