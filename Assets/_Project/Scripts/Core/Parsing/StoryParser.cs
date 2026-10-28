using System;
using System.Collections.Generic;
using System.Text;
using Kanji.Core.Model;

namespace Kanji.Core.Parsing
{
    /// <summary>Single-pass, stateless parser of {Kanji: ...; Reading: ...} blocks.</summary>
    public sealed class StoryParser
    {
        public ParseResult Parse(string name, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return new Run(text).Execute(name);
        }

        // Per-call state, so StoryParser itself stays stateless and reusable.
        private sealed class Run
        {
            private readonly string _text;
            private readonly StringBuilder _plain = new StringBuilder();
            private readonly StringBuilder _buf = new StringBuilder();
            private readonly List<Segment> _segs = new List<Segment>();
            private readonly List<KanjiEntry> _entries = new List<KanjiEntry>();
            private readonly List<ParseWarning> _warnings = new List<ParseWarning>();
            private readonly HashSet<KanjiKey> _keys = new HashSet<KanjiKey>();
            private int _line = 1;

            private string _kanji, _reading, _meaning, _sentence, _sentenceMeaning;

            public Run(string text) { _text = text; }

            public ParseResult Execute(string name)
            {
                int n = _text.Length;
                int i = 0;
                while (i < n)
                {
                    char c = _text[i];
                    if (c == '{')
                    {
                        int j = FindClose(i);
                        if (j < 0)
                        {
                            _plain.Append('{');
                            _warnings.Add(new ParseWarning(ParseWarningKind.UnclosedBrace, _line, "{"));
                            i++;
                            continue;
                        }

                        int blockLine = _line;
                        if (ParseBlock(i + 1, j, blockLine, out KanjiEntry entry))
                        {
                            Flush();
                            _entries.Add(entry);
                            _keys.Add(entry.Key);
                            _segs.Add(Segment.ForKanji(entry.Kanji, _entries.Count - 1));
                        }
                        else
                        {
                            _warnings.Add(new ParseWarning(ParseWarningKind.MissingKanji, blockLine, string.Empty));
                            _plain.Append(_text, i, j - i + 1);
                        }
                        CountLines(i, j + 1);
                        i = j + 1;
                    }
                    else if (c == '}')
                    {
                        _plain.Append('}');
                        _warnings.Add(new ParseWarning(ParseWarningKind.StrayClosingBrace, _line, "}"));
                        i++;
                    }
                    else
                    {
                        int start = i;
                        while (i < n && _text[i] != '{' && _text[i] != '}') i++;
                        _plain.Append(_text, start, i - start);
                        CountLines(start, i);
                    }
                }

                Flush();
                if (_entries.Count == 0)
                    _warnings.Add(new ParseWarning(ParseWarningKind.NoKanjiFound, 1, string.Empty));

                var story = new Story(string.Empty, name, _segs, _entries);
                var report = new ParseReport(_warnings, _entries.Count, _keys.Count);
                return new ParseResult(story, report);
            }

            private void CountLines(int from, int to)
            {
                for (int k = from; k < to; k++)
                    if (_text[k] == '\n') _line++;
            }

            private void Flush()
            {
                if (_plain.Length == 0) return;
                _segs.Add(Segment.Plain(_plain.ToString()));
                _plain.Clear();
            }

            private static bool IsEscapable(char c) => c == ';' || c == ':' || c == '}';

            /// <summary>Index of the closing '}' of the block opened at <paramref name="open"/>, or -1.</summary>
            private int FindClose(int open)
            {
                int n = _text.Length;
                for (int k = open + 1; k < n; k++)
                {
                    char c = _text[k];
                    if (c == '\\' && k + 1 < n && IsEscapable(_text[k + 1])) { k++; continue; }
                    if (c == '}') return k;
                    if (c == '{') return -1;
                }
                return -1;
            }

            private bool ParseBlock(int start, int end, int blockLine, out KanjiEntry entry)
            {
                _kanji = _reading = _meaning = _sentence = _sentenceMeaning = null;
                _buf.Clear();
                int colonPos = -1, dotPos = -1;

                for (int k = start; k < end; k++)
                {
                    char c = _text[k];
                    if (c == '\\' && k + 1 < end && IsEscapable(_text[k + 1]))
                    {
                        _buf.Append(_text[k + 1]);
                        k++;
                    }
                    else if (c == ';' || c == '；')
                    {
                        EndField(colonPos, dotPos, blockLine);
                        colonPos = dotPos = -1;
                    }
                    else
                    {
                        if ((c == ':' || c == '：') && colonPos < 0) colonPos = _buf.Length;
                        else if (c == '.' && dotPos < 0) dotPos = _buf.Length;
                        _buf.Append(c);
                    }
                }
                EndField(colonPos, dotPos, blockLine);

                string kanji = _kanji == null ? string.Empty : _kanji.Trim();
                if (kanji.Length == 0)
                {
                    entry = null;
                    return false;
                }
                entry = new KanjiEntry(kanji, _reading, _meaning, _sentence, _sentenceMeaning);
                return true;
            }

            private void EndField(int colonPos, int dotPos, int blockLine)
            {
                int sep = ChooseSeparator(colonPos, dotPos);
                if (sep >= 0)
                {
                    string fieldName = _buf.ToString(0, sep).Trim();
                    Assign(fieldName, NormalizeValue(sep + 1), blockLine);
                }
                else
                {
                    string fragment = _buf.ToString().Trim();
                    if (fragment.Length > 0)
                        _warnings.Add(new ParseWarning(ParseWarningKind.UnknownField, blockLine, fragment));
                }
                _buf.Clear();
            }

            // A dot before the first colon wins only when the text before it is a known field name
            // ("Sentence. 10:30" keeps the colon in the value); otherwise colon first, then dot.
            private int ChooseSeparator(int colonPos, int dotPos)
            {
                if (dotPos >= 0 && (colonPos < 0 || dotPos < colonPos) && IsKnownField(_buf.ToString(0, dotPos).Trim()))
                    return dotPos;
                return colonPos >= 0 ? colonPos : dotPos;
            }

            private static bool IsKnownField(string name) =>
                Eq(name, "Kanji") || Eq(name, "Reading") || Eq(name, "Meaning")
                || Eq(name, "Sentence") || Eq(name, "SentenceMeaning");

            // Each newline sequence (\r\n, \n, lone \r) becomes one space; then trim.
            private string NormalizeValue(int from)
            {
                var sb = new StringBuilder(Math.Max(0, _buf.Length - from));
                for (int k = from; k < _buf.Length; k++)
                {
                    char c = _buf[k];
                    if (c == '\r')
                    {
                        sb.Append(' ');
                        if (k + 1 < _buf.Length && _buf[k + 1] == '\n') k++;
                    }
                    else if (c == '\n') sb.Append(' ');
                    else sb.Append(c);
                }
                return sb.ToString().Trim();
            }

            private void Assign(string fieldName, string value, int blockLine)
            {
                if (!IsKnownField(fieldName))
                    _warnings.Add(new ParseWarning(ParseWarningKind.UnknownField, blockLine, fieldName));
                else if (Eq(fieldName, "Kanji")) Set(ref _kanji, fieldName, value, blockLine);
                else if (Eq(fieldName, "Reading")) Set(ref _reading, fieldName, value, blockLine);
                else if (Eq(fieldName, "Meaning")) Set(ref _meaning, fieldName, value, blockLine);
                else if (Eq(fieldName, "Sentence")) Set(ref _sentence, fieldName, value, blockLine);
                else Set(ref _sentenceMeaning, fieldName, value, blockLine);
            }

            // First occurrence wins, even when it is empty.
            private void Set(ref string field, string fieldName, string value, int blockLine)
            {
                if (field == null) field = value;
                else _warnings.Add(new ParseWarning(ParseWarningKind.DuplicateField, blockLine, fieldName));
            }

            private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
