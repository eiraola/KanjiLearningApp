using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kanji.Core.Model;

namespace Kanji.Core.Reading
{
    /// <summary>A piece of TMP rich text ready to assign to one text component.</summary>
    public readonly struct RichTextChunk
    {
        public RichTextChunk(string markup, int visibleLength, int linkCount, bool breakAfter)
        {
            Markup = markup ?? string.Empty;
            VisibleLength = visibleLength;
            LinkCount = linkCount;
            BreakAfter = breakAfter;
        }

        /// <summary>TMP rich text.</summary>
        public string Markup { get; }
        /// <summary>UTF-16 characters shown (no tags).</summary>
        public int VisibleLength { get; }
        public int LinkCount { get; }
        /// <summary>True when the cut consumed a '\n' (the chunk change provides the line break).</summary>
        public bool BreakAfter { get; }
    }

    /// <summary>
    /// Turns a story into TMP rich text chunks: kanji blocks become coloured links, everything else is literal
    /// (wrapped in noparse, with '&lt;' escaped), and the text is split into chunks of bounded size.
    /// </summary>
    public sealed class StoryRichTextBuilder
    {
        public const int DefaultMaxChunkChars = 8000;
        public const int MinMaxChunkChars = 256;

        private const string NoParseOpen = "<noparse>";
        private const string NoParseClose = "</noparse>";
        // A '<' is emitted right before a noparse close so TMP cannot form a tag with it.
        private const string EscapedLess = "<" + NoParseClose + NoParseOpen;

        private readonly string _linkColorHex;
        private readonly int _maxChunkChars;

        public StoryRichTextBuilder(string linkColorHex, int maxChunkChars = DefaultMaxChunkChars)
        {
            if (!IsValidColor(linkColorHex))
                throw new ArgumentException("Color must look like #RRGGBB.", nameof(linkColorHex));
            if (maxChunkChars < MinMaxChunkChars)
                throw new ArgumentOutOfRangeException(nameof(maxChunkChars), "Must be at least " + MinMaxChunkChars + ".");
            _linkColorHex = linkColorHex;
            _maxChunkChars = maxChunkChars;
        }

        public IReadOnlyList<RichTextChunk> Build(Story story)
        {
            if (story == null) throw new ArgumentNullException(nameof(story));
            return new Run(_linkColorHex, _maxChunkChars).Execute(story);
        }

        public static string LinkIdFor(int entryIndex) => "k" + entryIndex.ToString(CultureInfo.InvariantCulture);

        public static bool TryParseLinkId(string linkId, out int entryIndex)
        {
            entryIndex = -1;
            if (string.IsNullOrEmpty(linkId) || linkId.Length < 2 || linkId[0] != 'k') return false;
            for (int i = 1; i < linkId.Length; i++)
                if (linkId[i] < '0' || linkId[i] > '9') return false;
            if (int.TryParse(linkId.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                entryIndex = value;
                return true;
            }
            return false;
        }

        private static bool IsValidColor(string hex)
        {
            if (hex == null || hex.Length != 7 || hex[0] != '#') return false;
            for (int i = 1; i < 7; i++)
            {
                char c = hex[i];
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!ok) return false;
            }
            return true;
        }

        private static bool IsSoftBreak(char c) => c == '。' || c == '！' || c == '？' || c == '、' || c == ' ';

        private struct Piece
        {
            public string Src;
            public int Start;
            public int Len;
            public int EntryIndex; // -1 = literal text, otherwise link
            public bool Soft;      // a cut after this piece is allowed (oversize paragraphs only)
        }

        private sealed class Run
        {
            private readonly string _color;
            private readonly int _max;
            private readonly List<RichTextChunk> _chunks = new List<RichTextChunk>();
            private readonly List<Piece> _para = new List<Piece>();
            private readonly StringBuilder _sb;

            private bool _inNoParse;
            private int _vis;
            private int _links;
            private bool _hasContent;
            private bool _pendingNewline;
            private bool _prevBackslash;

            public Run(string color, int max)
            {
                _color = color;
                _max = max;
                _sb = new StringBuilder((int)(max * 1.2) + 64);
            }

            public IReadOnlyList<RichTextChunk> Execute(Story story)
            {
                bool lastWasCr = false;
                var segs = story.Segments;
                for (int s = 0; s < segs.Count; s++)
                {
                    Segment seg = segs[s];
                    if (seg.Kind == SegmentKind.Kanji)
                    {
                        lastWasCr = false;
                        if (seg.Text.Length > 0)
                            _para.Add(new Piece { Src = seg.Text, Start = 0, Len = seg.Text.Length, EntryIndex = seg.EntryIndex });
                        continue;
                    }

                    string t = seg.Text;
                    int runStart = 0;
                    for (int i = 0; i < t.Length; i++)
                    {
                        char c = t[i];
                        if (c == '\n' && lastWasCr) { lastWasCr = false; runStart = i + 1; continue; }
                        lastWasCr = c == '\r';
                        if (c != '\n' && c != '\r') continue;
                        AddText(t, runStart, i - runStart);
                        EndParagraph(true);
                        runStart = i + 1;
                    }
                    AddText(t, runStart, t.Length - runStart);
                }

                if (_para.Count > 0) EndParagraph(false);
                if (_pendingNewline) { WriteNewline(); _pendingNewline = false; _hasContent = true; }
                Flush(false);
                return _chunks;
            }

            private void AddText(string src, int start, int len)
            {
                if (len > 0) _para.Add(new Piece { Src = src, Start = start, Len = len, EntryIndex = -1 });
            }

            private void EndParagraph(bool endsWithNewline)
            {
                int length = 0;
                for (int i = 0; i < _para.Count; i++) length += _para[i].Len;

                if (length > _max) AddOversize();
                else AddNormal(length);

                _para.Clear();
                _pendingNewline = endsWithNewline;
            }

            private void AddNormal(int length)
            {
                if (_hasContent)
                {
                    int needed = _vis + (_pendingNewline ? 1 : 0) + length;
                    if (needed > _max) Flush(_pendingNewline);
                    else if (_pendingNewline) WriteNewline();
                }
                _pendingNewline = false;
                for (int i = 0; i < _para.Count; i++) WritePiece(_para[i]);
                _hasContent = true;
            }

            private void AddOversize()
            {
                if (_hasContent) Flush(_pendingNewline);
                _pendingNewline = false;

                List<Piece> units = ExpandUnits();
                int start = 0, vis = 0, lastSoft = -1, i = 0;
                while (i < units.Count)
                {
                    Piece u = units[i];
                    if (vis + u.Len > _max && vis > 0)
                    {
                        int cut = lastSoft > start ? lastSoft : i;
                        for (int k = start; k < cut; k++) WritePiece(units[k]);
                        _hasContent = true;
                        Flush(false);
                        start = cut; i = cut; vis = 0; lastSoft = -1;
                        continue;
                    }
                    vis += u.Len;
                    i++;
                    if (u.Soft) lastSoft = i;
                }
                for (int k = start; k < units.Count; k++) WritePiece(units[k]);
                _hasContent = true;
            }

            // Splits text into single chars (or surrogate pairs) so a cut can never fall inside a pair or a link.
            private List<Piece> ExpandUnits()
            {
                var units = new List<Piece>(_para.Count * 4);
                for (int p = 0; p < _para.Count; p++)
                {
                    Piece piece = _para[p];
                    if (piece.EntryIndex >= 0)
                    {
                        piece.Soft = true;
                        units.Add(piece);
                        continue;
                    }
                    int end = piece.Start + piece.Len;
                    for (int i = piece.Start; i < end;)
                    {
                        bool pair = char.IsHighSurrogate(piece.Src[i]) && i + 1 < end && char.IsLowSurrogate(piece.Src[i + 1]);
                        int n = pair ? 2 : 1;
                        units.Add(new Piece { Src = piece.Src, Start = i, Len = n, EntryIndex = -1, Soft = n == 1 && IsSoftBreak(piece.Src[i]) });
                        i += n;
                    }
                }
                return units;
            }

            private void WriteNewline()
            {
                _sb.Append('\n');
                _prevBackslash = false;
                _vis++;
            }

            private void WritePiece(Piece p)
            {
                if (p.EntryIndex >= 0)
                {
                    CloseNoParse();
                    _sb.Append("<link=\"").Append(LinkIdFor(p.EntryIndex)).Append("\"><color=").Append(_color).Append('>');
                    _sb.Append(NoParseOpen);
                    AppendEscaped(p.Src, p.Start, p.Len);
                    _sb.Append(NoParseClose).Append("</color></link>");
                    _prevBackslash = false;
                    _links++;
                }
                else
                {
                    if (!_inNoParse) { _sb.Append(NoParseOpen); _inNoParse = true; }
                    AppendEscaped(p.Src, p.Start, p.Len);
                }
                _vis += p.Len;
            }

            private void CloseNoParse()
            {
                if (!_inNoParse) return;
                _sb.Append(NoParseClose);
                _inNoParse = false;
            }

            // '<' is escaped (see EscapedLess). TMP also turns a literal backslash-u0041 into "A" even with
            // parseCtrlCharacters off, so a noparse boundary is placed between the backslash and 'u'/'U'.
            private void AppendEscaped(string src, int start, int len)
            {
                int end = start + len;
                int runStart = start;
                for (int i = start; i < end; i++)
                {
                    char c = src[i];
                    if (c == '<')
                    {
                        _sb.Append(src, runStart, i - runStart).Append(EscapedLess);
                        runStart = i + 1;
                    }
                    else if ((c == 'u' || c == 'U') && (i > start ? src[i - 1] == '\\' : _prevBackslash))
                    {
                        _sb.Append(src, runStart, i - runStart).Append(NoParseClose).Append(NoParseOpen);
                        runStart = i;
                    }
                }
                _sb.Append(src, runStart, end - runStart);
                _prevBackslash = len > 0 && src[end - 1] == '\\';
            }

            private void Flush(bool breakAfter)
            {
                if (!_hasContent) return;
                CloseNoParse();
                _chunks.Add(new RichTextChunk(_sb.ToString(), _vis, _links, breakAfter));
                _sb.Clear();
                _vis = 0;
                _links = 0;
                _hasContent = false;
            }
        }
    }
}
