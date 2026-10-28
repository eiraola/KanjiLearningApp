using System;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class StoryParserTests
    {
        private const string FwColon = "\uFF1A";
        private const string FwSemi = "\uFF1B";
        private const string FwSpace = "\u3000";

        private readonly StoryParser _parser = new StoryParser();

        private ParseResult Parse(string text) => _parser.Parse("t", text);

        private static KanjiEntry Single(ParseResult r)
        {
            Assert.AreEqual(1, r.Story.Entries.Count);
            return r.Story.Entries[0];
        }

        [Test]
        public void Parse_Example_GddSection51()
        {
            var r = Parse("Hoy sale el {Kanji: \u65E5; Reading: \u3072; Meaning: sol; Sentence: \u65E5\u304C\u6607\u308B; SentenceMeaning: el sol sale;} y luego se pone.");
            var e = Single(r);
            Assert.AreEqual("\u65E5", e.Kanji);
            Assert.AreEqual("\u3072", e.Reading);
            Assert.AreEqual("sol", e.Meaning);
            Assert.AreEqual("\u65E5\u304C\u6607\u308B", e.Sentence);
            Assert.AreEqual("el sol sale", e.SentenceMeaning);
            Assert.AreEqual(3, r.Story.Segments.Count);
            Assert.AreEqual(SegmentKind.Text, r.Story.Segments[0].Kind);
            Assert.AreEqual("Hoy sale el ", r.Story.Segments[0].Text);
            Assert.AreEqual(SegmentKind.Kanji, r.Story.Segments[1].Kind);
            Assert.AreEqual("\u65E5", r.Story.Segments[1].Text);
            Assert.AreEqual(0, r.Story.Segments[1].EntryIndex);
            Assert.AreEqual(" y luego se pone.", r.Story.Segments[2].Text);
            Assert.AreEqual(0, r.Report.Warnings.Count);
            Assert.AreEqual(1, r.Report.BlockCount);
            Assert.AreEqual(1, r.Report.DistinctKanjiCount);
            Assert.AreEqual(string.Empty, r.Story.Id);
            Assert.AreEqual("t", r.Story.Name);
        }

        [Test]
        public void Parse_TrailingSemicolon_IsOptional()
        {
            var a = Single(Parse("{Kanji: A; Reading: b;}"));
            var b = Single(Parse("{Kanji: A; Reading: b}"));
            Assert.AreEqual(a.Kanji, b.Kanji);
            Assert.AreEqual(a.Reading, b.Reading);
            Assert.AreEqual("b", b.Reading);
        }

        [Test]
        public void Parse_SentenceWithDot_EqualsColon()
        {
            var e = Single(Parse("{Kanji: A; Sentence. hola mundo; Meaning. sol}"));
            Assert.AreEqual("hola mundo", e.Sentence);
            Assert.AreEqual("sol", e.Meaning);
        }

        [Test]
        public void Parse_FieldNames_AreCaseInsensitive()
        {
            var e = Single(Parse("{kanji: A; READING: r; sEnTeNcE: s; sentencemeaning: sm; MEANING: m}"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual("r", e.Reading);
            Assert.AreEqual("s", e.Sentence);
            Assert.AreEqual("sm", e.SentenceMeaning);
            Assert.AreEqual("m", e.Meaning);
        }

        [Test]
        public void Parse_FullWidthSeparators_Work()
        {
            var e = Single(Parse("{Kanji" + FwColon + "A" + FwSemi + "Reading" + FwColon + "r" + FwSemi + "}"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual("r", e.Reading);
        }

        [Test]
        public void Parse_FullWidthSpaceAndTabs_AreTrimmed()
        {
            var e = Single(Parse("\t{" + FwSpace + "Kanji" + FwSpace + ":\t" + FwSpace + "A" + FwSpace + ";\tReading\t:\tr\t" + FwSpace + ";}\t"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual("r", e.Reading);
            Assert.AreEqual("\t", Parse("\t{Kanji:A}\t").Story.Segments[0].Text);
        }

        [Test]
        public void Parse_FieldOrder_IsFree()
        {
            var e = Single(Parse("{SentenceMeaning: sm; Meaning: m; Reading: r; Kanji: A; Sentence: s}"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual("r", e.Reading);
            Assert.AreEqual("m", e.Meaning);
            Assert.AreEqual("s", e.Sentence);
            Assert.AreEqual("sm", e.SentenceMeaning);
        }

        [Test]
        public void Parse_ColonInValue_Preserved()
        {
            var e = Single(Parse("{Kanji: A; Meaning: 10:30}"));
            Assert.AreEqual("10:30", e.Meaning);
        }

        [Test]
        public void Parse_DotsInValue_NotSeparator_WhenColonPresent()
        {
            var e = Single(Parse("{Kanji: A; Meaning: Mr. Tanaka}"));
            Assert.AreEqual("Mr. Tanaka", e.Meaning);
        }

        [Test]
        public void Parse_NewlineInsideValue_BecomesSpace()
        {
            Assert.AreEqual("uno dos", Single(Parse("{Kanji: A; Meaning: uno\ndos}")).Meaning);
            Assert.AreEqual("uno dos", Single(Parse("{Kanji: A; Meaning: uno\r\ndos}")).Meaning);
            Assert.AreEqual("uno dos", Single(Parse("{Kanji: A; Meaning: uno\rdos}")).Meaning);
        }

        [Test]
        public void Parse_NewlineBetweenFields_Ok()
        {
            var e = Single(Parse("{Kanji: A;\r\n Reading: r;\n Meaning: m\n}"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual("r", e.Reading);
            Assert.AreEqual("m", e.Meaning);
        }

        [Test]
        public void Parse_InnerSpaces_Preserved()
        {
            var e = Single(Parse("{Kanji: A; Meaning:   a  b   c  }"));
            Assert.AreEqual("a  b   c", e.Meaning);
        }

        [Test]
        public void Parse_Escapes_Semicolon_Colon_Brace()
        {
            var e = Single(Parse(@"{Kanji: A; Meaning: a\;b\:c\}d; Reading: r}"));
            Assert.AreEqual("a;b:c}d", e.Meaning);
            Assert.AreEqual("r", e.Reading);
            Assert.AreEqual(1, Parse(@"{Kanji: A\}B}").Story.Entries.Count);
            Assert.AreEqual("A}B", Single(Parse(@"{Kanji: A\}B}")).Kanji);
        }

        [Test]
        public void Parse_Backslash_BeforeOtherChar_IsLiteral()
        {
            var e = Single(Parse(@"{Kanji: A; Meaning: a\b\\c}"));
            Assert.AreEqual(@"a\b\\c", e.Meaning);
        }

        [Test]
        public void Parse_Escape_OutsideBlock_NotProcessed()
        {
            var r = Parse(@"antes \; y \: fin {Kanji: A} \}");
            Assert.AreEqual(@"antes \; y \: fin ", r.Story.Segments[0].Text);
            Assert.AreEqual(@" \", r.Story.Segments[2].Text.Substring(0, 2));
        }

        [Test]
        public void Parse_OptionalFieldsAbsent_Accepted()
        {
            var e = Single(Parse("{Kanji: A}"));
            Assert.AreEqual("A", e.Kanji);
            Assert.AreEqual(string.Empty, e.Reading);
            Assert.AreEqual(string.Empty, e.Meaning);
            Assert.AreEqual(string.Empty, e.Sentence);
            Assert.AreEqual(string.Empty, e.SentenceMeaning);
        }

        [Test]
        public void Parse_TextOutsideBlocks_Unchanged()
        {
            const string raw = "linea 1 <b>\r\nlinea 2\n\n  \tlinea 3 ";
            var r = Parse(raw + "{Kanji: A}" + raw);
            Assert.AreEqual(3, r.Story.Segments.Count);
            Assert.AreEqual(raw, r.Story.Segments[0].Text);
            Assert.AreEqual(raw, r.Story.Segments[2].Text);
        }

        [Test]
        public void Parse_MultipleBlocks_OrderAndEntryIndex()
        {
            var r = Parse("a{Kanji: X}b{Kanji: Y}c{Kanji: X}");
            Assert.AreEqual(6, r.Story.Segments.Count);
            Assert.AreEqual(3, r.Story.Entries.Count);
            Assert.AreEqual(0, r.Story.Segments[1].EntryIndex);
            Assert.AreEqual(1, r.Story.Segments[3].EntryIndex);
            Assert.AreEqual(2, r.Story.Segments[5].EntryIndex);
            Assert.AreEqual("Y", r.Story.Entries[r.Story.Segments[3].EntryIndex].Kanji);
            Assert.AreEqual(3, r.Report.BlockCount);
            Assert.AreEqual(2, r.Report.DistinctKanjiCount);
            Assert.AreEqual(2, r.Story.DistinctKanjiCount);
        }

        [Test]
        public void Parse_NoBlocks_ReportsNoKanjiFound()
        {
            var r = Parse("solo texto");
            Assert.AreEqual(1, r.Story.Segments.Count);
            Assert.AreEqual(ParseWarningKind.NoKanjiFound, r.Report.Warnings[0].Kind);
        }

        [Test]
        public void Parse_NullText_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Parse(null));
        }

        [Test]
        public void Parse_KanjiKey_TrimmedAndOrdinal()
        {
            Assert.AreEqual(new KanjiKey(" A "), new KanjiKey("A"));
            Assert.IsTrue(new KanjiKey("A") == new KanjiKey("A"));
            Assert.IsTrue(new KanjiKey("a") != new KanjiKey("A"));
            Assert.AreEqual(new KanjiKey("A").GetHashCode(), new KanjiKey("A ").GetHashCode());
            Assert.AreEqual("A", new KanjiKey(" A").ToString());
            // No Unicode normalization: precomposed vs combining differ.
            Assert.AreNotEqual(new KanjiKey("\u00E9"), new KanjiKey("e\u0301"));
            Assert.Throws<ArgumentException>(() => new KanjiKey("  "));
            Assert.Throws<ArgumentException>(() => new KanjiKey(null));
        }

        [Test]
        public void Parse_StoryName_FromFileName()
        {
            Assert.AreEqual("a", StoryName.FromFileName("a.txt"));
            Assert.AreEqual("a.b", StoryName.FromFileName("a.b.txt"));
            Assert.AreEqual("x", StoryName.FromFileName(@"ruta\x.TXT"));
            Assert.AreEqual("x", StoryName.FromFileName("a/b/x.txt"));
            Assert.AreEqual("abc", StoryName.FromFileName("abc"));
            Assert.AreEqual(StoryName.DefaultName, StoryName.FromFileName(".txt"));
            Assert.AreEqual(StoryName.DefaultName, StoryName.FromFileName(""));
            Assert.AreEqual(StoryName.DefaultName, StoryName.FromFileName(null));
        }

        [Test]
        public void Parse_StoryName_AreSame_IgnoresCase()
        {
            Assert.IsTrue(StoryName.AreSame("Hola", " hOLA "));
            Assert.IsFalse(StoryName.AreSame("Hola", "Hola2"));
        }

        [Test]
        public void Parse_StoryWith_SharesContent()
        {
            var s = Parse("a{Kanji: X}").Story;
            var c = s.With("id1", "nombre", new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
            Assert.AreEqual("id1", c.Id);
            Assert.AreEqual("nombre", c.Name);
            Assert.AreSame(s.Segments, c.Segments);
            Assert.AreSame(s.Entries, c.Entries);
        }
    }
}
