using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Parsing
{
    public enum ParseWarningKind { MissingKanji, DuplicateField, UnknownField, UnclosedBrace, StrayClosingBrace, NoKanjiFound }

    public sealed class ParseWarning
    {
        public ParseWarning(ParseWarningKind kind, int line, string detail)
        {
            Kind = kind;
            Line = line;
            Detail = detail ?? string.Empty;
        }

        public ParseWarningKind Kind { get; }
        public int Line { get; }
        public string Detail { get; }
    }

    public sealed class ParseReport
    {
        public ParseReport(IReadOnlyList<ParseWarning> warnings, int blockCount, int distinctKanjiCount)
        {
            Warnings = warnings ?? Array.Empty<ParseWarning>();
            BlockCount = blockCount;
            DistinctKanjiCount = distinctKanjiCount;
        }

        public IReadOnlyList<ParseWarning> Warnings { get; }
        public int BlockCount { get; }
        public int DistinctKanjiCount { get; }
    }

    public sealed class ParseResult
    {
        public ParseResult(Story story, ParseReport report)
        {
            Story = story;
            Report = report;
        }

        public Story Story { get; }
        public ParseReport Report { get; }
    }
}
