using System;
using System.Collections.Generic;

namespace Kanji.Core.Words
{
    public enum WordColumn { Word, Sentence, Reading, Meaning, SentenceMeaning, SentenceReading }

    public enum WordParseError { None, Empty, NoTabs }

    public sealed class WordParseResult
    {
        public WordParseResult(WordParseError error, IReadOnlyList<WordEntry> entries, int invalidLines, bool hasHeader)
        {
            Error = error;
            Entries = entries ?? Array.Empty<WordEntry>();
            InvalidLines = invalidLines;
            HasHeader = hasHeader;
        }

        public WordParseError Error { get; }

        /// <summary>Parsed words in text order. May repeat a Word (duplicates are resolved by the import).</summary>
        public IReadOnlyList<WordEntry> Entries { get; }

        /// <summary>Non-empty lines that produced no word (no Word cell, or no tab while the header has several columns).</summary>
        public int InvalidLines { get; }

        public bool HasHeader { get; }
    }

    /// <summary>
    /// Parses pasted tab-separated text. The first non-empty line is a header when one of its cells is "Word";
    /// columns are then matched by name (case-insensitive, any order, unknown ones ignored). Without a header
    /// the order is Word, Sentence, Reading, Meaning, SentenceMeaning, SentenceReading.
    /// </summary>
    public static class WordTsvParser
    {
        public static readonly WordColumn[] DefaultOrder =
        {
            WordColumn.Word, WordColumn.Sentence, WordColumn.Reading, WordColumn.Meaning,
            WordColumn.SentenceMeaning, WordColumn.SentenceReading,
        };

        public static WordParseResult Parse(string text)
        {
            var entries = new List<WordEntry>();
            if (string.IsNullOrWhiteSpace(text)) return new WordParseResult(WordParseError.Empty, entries, 0, false);

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int first = 0;
            while (first < lines.Length && IsBlank(lines[first])) first++;

            bool anyTab = text.IndexOf('\t') >= 0;
            // A single word per line is fine without tabs; several cells that lost their tabs are not.
            if (!anyTab && CountNonBlank(lines) > 1 && LooksLikeHeaderWithoutTabs(lines[first]))
                return new WordParseResult(WordParseError.NoTabs, entries, 0, false);

            int?[] map; // map[cellIndex] = column index into WordColumn, null = ignored
            bool hasHeader = TryReadHeader(lines[first], out map);
            int start = hasHeader ? first + 1 : first;
            if (!hasHeader)
            {
                map = new int?[DefaultOrder.Length];
                for (int i = 0; i < DefaultOrder.Length; i++) map[i] = (int)DefaultOrder[i];
            }

            int mappedColumns = 0;
            foreach (int? m in map) if (m.HasValue) mappedColumns++;

            int invalid = 0;
            for (int li = start; li < lines.Length; li++)
            {
                string line = lines[li];
                if (IsBlank(line)) continue;
                string[] cells = line.Split('\t');
                if (hasHeader && mappedColumns > 1 && cells.Length < 2)
                {
                    invalid++;
                    continue;
                }

                var values = new string[DefaultOrder.Length];
                for (int c = 0; c < cells.Length && c < map.Length; c++)
                    if (map[c].HasValue) values[map[c].Value] = Clean(cells[c]);

                string word = values[(int)WordColumn.Word];
                if (string.IsNullOrEmpty(word))
                {
                    invalid++;
                    continue;
                }

                entries.Add(new WordEntry(word,
                    values[(int)WordColumn.Reading],
                    values[(int)WordColumn.Meaning],
                    values[(int)WordColumn.Sentence],
                    values[(int)WordColumn.SentenceReading],
                    values[(int)WordColumn.SentenceMeaning]));
            }

            return new WordParseResult(WordParseError.None, entries, invalid, hasHeader);
        }

        private static bool TryReadHeader(string line, out int?[] map)
        {
            string[] cells = line.Split('\t');
            map = new int?[cells.Length];
            bool hasWord = false;
            var used = new HashSet<WordColumn>();
            for (int i = 0; i < cells.Length; i++)
            {
                if (!TryColumn(cells[i], out WordColumn col) || !used.Add(col)) continue;
                map[i] = (int)col;
                if (col == WordColumn.Word) hasWord = true;
            }
            return hasWord;
        }

        public static bool TryColumn(string header, out WordColumn column)
        {
            string n = Normalize(header);
            foreach (WordColumn c in DefaultOrder)
            {
                if (n == c.ToString().ToLowerInvariant())
                {
                    column = c;
                    return true;
                }
            }
            column = WordColumn.Word;
            return false;
        }

        // "Sentence Meaning", "sentence_meaning" and "SENTENCEMEANING" all map to SentenceMeaning.
        private static string Normalize(string s)
        {
            var chars = new List<char>(s.Length);
            foreach (char ch in Clean(s))
                if (ch != ' ' && ch != '_' && ch != '-') chars.Add(char.ToLowerInvariant(ch));
            return new string(chars.ToArray());
        }

        private static bool LooksLikeHeaderWithoutTabs(string line)
        {
            string[] parts = Clean(line).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;
            int known = 0;
            foreach (string p in parts) if (TryColumn(p, out _)) known++;
            return known >= 2;
        }

        private static int CountNonBlank(string[] lines)
        {
            int n = 0;
            foreach (string l in lines) if (!IsBlank(l)) n++;
            return n;
        }

        private static bool IsBlank(string line) => Clean(line).Length == 0;

        // Trims whitespace plus BOM / zero-width characters that sneak in from spreadsheets and clipboards.
        private static string Clean(string s) => (s ?? string.Empty).Trim().Trim('﻿', '​').Trim();
    }
}
