using System;
using System.Diagnostics;
using System.Linq;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class StoryParserErrorTests
    {
        private readonly StoryParser _parser = new StoryParser();

        private ParseResult Parse(string text) => _parser.Parse("t", text);

        private static int Count(ParseResult r, ParseWarningKind kind) => r.Report.Warnings.Count(w => w.Kind == kind);

        [TestCase("a {Reading: r; Meaning: m} b")]
        [TestCase("a {Kanji: ; Reading: r} b")]
        [TestCase("a {Kanji: 　 ; Reading: r} b")]
        public void Parse_MissingKanji_BlockBecomesText(string text)
        {
            var r = Parse(text);
            Assert.AreEqual(0, r.Story.Entries.Count);
            Assert.AreEqual(0, r.Report.BlockCount);
            Assert.AreEqual(1, Count(r, ParseWarningKind.MissingKanji));
            Assert.AreEqual(1, r.Story.Segments.Count);
            Assert.AreEqual(SegmentKind.Text, r.Story.Segments[0].Kind);
            Assert.AreEqual(text, r.Story.Segments[0].Text);
        }

        [Test]
        public void Parse_MissingKanji_EscapesNotProcessedInRawText()
        {
            var r = Parse("{Meaning: a\\;b}");
            Assert.AreEqual("{Meaning: a\\;b}", r.Story.Segments[0].Text);
            Assert.AreEqual(1, Count(r, ParseWarningKind.MissingKanji));
        }

        [Test]
        public void Parse_EmptyBlock_IsMissingKanji()
        {
            foreach (string text in new[] { "{}", "{ }" })
            {
                var r = Parse(text);
                Assert.AreEqual(1, Count(r, ParseWarningKind.MissingKanji), text);
                Assert.AreEqual(text, r.Story.Segments[0].Text);
            }
        }

        [Test]
        public void Parse_EmptyOptional_Accepted()
        {
            var r = Parse("{Kanji: A; Reading: ; Meaning:}");
            Assert.AreEqual(1, r.Story.Entries.Count);
            Assert.AreEqual(string.Empty, r.Story.Entries[0].Reading);
            Assert.AreEqual(string.Empty, r.Story.Entries[0].Meaning);
            Assert.AreEqual(0, r.Report.Warnings.Count);
        }

        [Test]
        public void Parse_DuplicateField_FirstWins_Warns()
        {
            var r = Parse("{Kanji: A; Reading: one; reading: two}");
            Assert.AreEqual("one", r.Story.Entries[0].Reading);
            Assert.AreEqual(1, r.Report.Warnings.Count);
            Assert.AreEqual(ParseWarningKind.DuplicateField, r.Report.Warnings[0].Kind);
            Assert.AreEqual("reading", r.Report.Warnings[0].Detail);
        }

        [Test]
        public void Parse_DuplicateField_EmptyFirstKanjiStillWins()
        {
            var r = Parse("{Kanji: ; Kanji: A}");
            Assert.AreEqual(0, r.Story.Entries.Count);
            Assert.AreEqual(1, Count(r, ParseWarningKind.DuplicateField));
            Assert.AreEqual(1, Count(r, ParseWarningKind.MissingKanji));
        }

        [Test]
        public void Parse_UnknownField_Ignored_Warns()
        {
            var r = Parse("{Kanji: A; Color: red}");
            Assert.AreEqual(1, r.Story.Entries.Count);
            Assert.AreEqual(1, r.Report.Warnings.Count);
            Assert.AreEqual(ParseWarningKind.UnknownField, r.Report.Warnings[0].Kind);
            Assert.AreEqual("Color", r.Report.Warnings[0].Detail);
        }

        [Test]
        public void Parse_FieldWithoutSeparator_Warns()
        {
            var r = Parse("{Kanji: A; foo; ;}");
            Assert.AreEqual(1, r.Story.Entries.Count);
            Assert.AreEqual(1, r.Report.Warnings.Count);
            Assert.AreEqual(ParseWarningKind.UnknownField, r.Report.Warnings[0].Kind);
            Assert.AreEqual("foo", r.Report.Warnings[0].Detail);
        }

        [Test]
        public void Parse_UnclosedBrace_DoesNotConsumeRest()
        {
            var r = Parse("a { b {Kanji: A} c");
            Assert.AreEqual(1, r.Story.Entries.Count);
            Assert.AreEqual(1, Count(r, ParseWarningKind.UnclosedBrace));
            Assert.AreEqual("a { b ", r.Story.Segments[0].Text);
            Assert.AreEqual(SegmentKind.Kanji, r.Story.Segments[1].Kind);
            Assert.AreEqual(" c", r.Story.Segments[2].Text);
        }

        [Test]
        public void Parse_UnclosedBrace_AtEndOfText()
        {
            var r = Parse("hola {Kanji: A");
            Assert.AreEqual(0, r.Story.Entries.Count);
            Assert.AreEqual(1, Count(r, ParseWarningKind.UnclosedBrace));
            Assert.AreEqual("hola {Kanji: A", r.Story.Segments[0].Text);
        }

        [Test]
        public void Parse_StrayClosingBrace_IsText_Warns()
        {
            var r = Parse("a } b");
            Assert.AreEqual("a } b", r.Story.Segments[0].Text);
            Assert.AreEqual(1, Count(r, ParseWarningKind.StrayClosingBrace));
        }

        [Test]
        public void Parse_Nested_InnerWins()
        {
            var r = Parse("{ a {Kanji: x} b }");
            Assert.AreEqual(1, r.Story.Entries.Count);
            Assert.AreEqual("x", r.Story.Entries[0].Kanji);
            Assert.AreEqual(3, r.Story.Segments.Count);
            Assert.AreEqual("{ a ", r.Story.Segments[0].Text);
            Assert.AreEqual(" b }", r.Story.Segments[2].Text);
            Assert.AreEqual(1, Count(r, ParseWarningKind.UnclosedBrace));
            Assert.AreEqual(1, Count(r, ParseWarningKind.StrayClosingBrace));
        }

        [Test]
        public void Parse_RepeatedKanji_NSegments_OneKey()
        {
            var r = Parse("{Kanji: A} y {Kanji: A} y {Kanji: A}");
            Assert.AreEqual(3, r.Story.Segments.Count(s => s.Kind == SegmentKind.Kanji));
            Assert.AreEqual(3, r.Story.Entries.Count);
            Assert.AreEqual(3, r.Report.BlockCount);
            Assert.AreEqual(1, r.Report.DistinctKanjiCount);
            Assert.AreEqual(1, r.Story.DistinctKanjiCount);
        }

        [Test]
        public void Parse_NoBlocks_StoryWithoutKanjis_Warns()
        {
            var r = Parse("solo texto");
            Assert.AreEqual(1, r.Story.Segments.Count);
            Assert.AreEqual("solo texto", r.Story.Segments[0].Text);
            Assert.AreEqual(0, r.Story.Entries.Count);
            Assert.AreEqual(1, Count(r, ParseWarningKind.NoKanjiFound));
        }

        [Test]
        public void Parse_EmptyText()
        {
            var r = Parse(string.Empty);
            Assert.AreEqual(0, r.Story.Segments.Count);
            Assert.AreEqual(1, Count(r, ParseWarningKind.NoKanjiFound));
        }

        [Test]
        public void Parse_WarningLine_IsOneBased()
        {
            var r = Parse("uno\ndos\n{Reading: r}");
            var w = r.Report.Warnings.First(x => x.Kind == ParseWarningKind.MissingKanji);
            Assert.AreEqual(3, w.Line);
        }

        [Test]
        public void Parse_ManyUnclosedBraces_StaysLinear()
        {
            string text = new string('{', 10000);
            var sw = Stopwatch.StartNew();
            var r = Parse(text);
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 1000);
            Assert.AreEqual(10000, Count(r, ParseWarningKind.UnclosedBrace));
            Assert.AreEqual(text, r.Story.Segments[0].Text);
        }

        [Test]
        public void Parse_NullText_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _parser.Parse("t", null));
        }

        [Test]
        public void SentenceWithDot_KeepsColonInValue()
        {
            var r = Parse("{Kanji: 時; Sentence. 会議は10:30です}");
            Assert.AreEqual("会議は10:30です", r.Story.Entries[0].Sentence);
            Assert.AreEqual(0, r.Report.Warnings.Count);
        }

        [Test]
        public void SentenceWithColon_KeepsTimeInValue()
        {
            var r = Parse("{Kanji: 時; Sentence: 会議は10:30です}");
            Assert.AreEqual("会議は10:30です", r.Story.Entries[0].Sentence);
        }

        [Test]
        public void ColonBeforeDot_UsesColon()
        {
            var r = Parse("{Kanji: A; Meaning: Mr. Smith}");
            Assert.AreEqual("Mr. Smith", r.Story.Entries[0].Meaning);
        }

        [Test]
        public void UnknownNameBeforeDot_StillUsesColon()
        {
            var r = Parse("{Kanji: A; Foo. a:b}");
            Assert.AreEqual(1, Count(r, ParseWarningKind.UnknownField));
            Assert.AreEqual(1, r.Story.Entries.Count);
        }
    }
}
