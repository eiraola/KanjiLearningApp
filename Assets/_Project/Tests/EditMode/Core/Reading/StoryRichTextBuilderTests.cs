using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using Kanji.Core.Reading;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class StoryRichTextBuilderTests
    {
        private const string Blue = "#1E6FFF";
        private const string Cat = "猫";

        private static StoryRichTextBuilder Builder(int max = StoryRichTextBuilder.DefaultMaxChunkChars) =>
            new StoryRichTextBuilder(Blue, max);

        // Builds a story from raw parts: plain strings are text, "@X" is a kanji X (entries numbered in order).
        private static Story MakeStory(params string[] parts)
        {
            var segments = new List<Segment>();
            var entries = new List<KanjiEntry>();
            foreach (string p in parts)
            {
                if (p.StartsWith("@", StringComparison.Ordinal))
                {
                    string k = p.Substring(1);
                    segments.Add(Segment.ForKanji(k, entries.Count));
                    entries.Add(new KanjiEntry(k, "r", "m", "s", "sm"));
                }
                else segments.Add(Segment.Plain(p));
            }
            return new Story("id", "name", segments, entries);
        }

        private static string Normalize(string s) => s.Replace("\r\n", "\n").Replace('\r', '\n');

        private static string Visible(Story story)
        {
            var sb = new StringBuilder();
            foreach (Segment s in story.Segments) sb.Append(s.Text);
            return Normalize(sb.ToString());
        }

        // Reconstructs the visible text: chunks joined by "\n" when BreakAfter, directly otherwise.
        private static string Reassemble(IReadOnlyList<RichTextChunk> chunks)
        {
            var sb = new StringBuilder();
            foreach (RichTextChunk c in chunks)
            {
                sb.Append(TmpVisibleText.Strip(c.Markup));
                if (c.BreakAfter) sb.Append('\n');
            }
            return sb.ToString();
        }

        private static int TotalLinks(IReadOnlyList<RichTextChunk> chunks)
        {
            int t = 0;
            foreach (RichTextChunk c in chunks) t += c.LinkCount;
            return t;
        }

        [Test]
        public void PlainText_PreservesLineBreaks()
        {
            var chunks = Builder().Build(MakeStory("a\nb\n\nc"));
            Assert.AreEqual(1, chunks.Count);
            Assert.AreEqual("a\nb\n\nc", TmpVisibleText.Strip(chunks[0].Markup));
            Assert.AreEqual(6, chunks[0].VisibleLength);
            Assert.AreEqual(0, chunks[0].LinkCount);
        }

        [Test]
        public void CrLf_NormalizedToLf()
        {
            var chunks = Builder().Build(MakeStory("a\r\nb\rc", "\r", "\nd"));
            Assert.AreEqual("a\nb\nc\nd", Reassemble(chunks));
        }

        [Test]
        public void KanjiSegment_EmitsLinkWithColorAndEntryIndex()
        {
            var chunks = Builder().Build(MakeStory("x", "@" + Cat, "y"));
            Assert.AreEqual(1, chunks.Count);
            Assert.AreEqual(
                "<noparse>x</noparse><link=\"k0\"><color=" + Blue + "><noparse>" + Cat + "</noparse></color></link><noparse>y</noparse>",
                chunks[0].Markup);
            Assert.AreEqual(1, chunks[0].LinkCount);
            Assert.AreEqual(3, chunks[0].VisibleLength);
        }

        [Test]
        public void RepeatedKanji_EachOccurrenceHasOwnIndex()
        {
            var chunks = Builder().Build(MakeStory("@" + Cat, "a", "@" + Cat));
            CollectionAssert.AreEqual(new[] { "k0", "k1" }, TmpVisibleText.LinkIds(chunks[0].Markup));
        }

        [TestCase("<b>x</b>")]
        [TestCase("</noparse>")]
        [TestCase("</NoParse>")]
        [TestCase("<noparse>")]
        [TestCase("<link=\"k0\">")]
        [TestCase("<<")]
        [TestCase("final<")]
        [TestCase("a\\nb \\u0041 \\t")]
        [TestCase("\\U0001F600 \\\\u0041 \\u")]
        [TestCase("1 < 2 > 1 <color=#FF0000>red")]
        public void HostileMarkup_IsLiteral(string hostile)
        {
            var chunks = Builder().Build(MakeStory(hostile, "@" + Cat, hostile));
            Assert.AreEqual(hostile + Cat + hostile, Reassemble(chunks));
            Assert.AreEqual(1, chunks[0].LinkCount);
            CollectionAssert.AreEqual(new[] { "k0" }, TmpVisibleText.LinkIds(chunks[0].Markup));
        }

        [Test]
        public void HostileKanjiValue_IsEscaped()
        {
            var chunks = Builder().Build(MakeStory("@<b>"));
            Assert.AreEqual("<b>", TmpVisibleText.Strip(chunks[0].Markup));
        }

        [Test]
        public void NoKanji_NoLinks()
        {
            var chunks = Builder().Build(MakeStory("solo texto\nsin kanjis"));
            Assert.AreEqual(0, chunks[0].LinkCount);
            Assert.IsFalse(chunks[0].Markup.Contains("<link"));
        }

        [Test]
        public void EmptyStory_NoChunks()
        {
            Assert.AreEqual(0, Builder().Build(MakeStory()).Count);
            Assert.AreEqual(0, Builder().Build(MakeStory("")).Count);
        }

        [Test]
        public void Build_NullStory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Builder().Build(null));
        }

        [Test]
        public void TrailingNewline_IsKept()
        {
            Assert.AreEqual("a\n", Reassemble(Builder().Build(MakeStory("a\n"))));
        }

        [Test]
        public void Chunks_RespectMax_AndNeverSplitLinks()
        {
            var parts = new List<string>();
            for (int i = 0; i < 200; i++)
            {
                parts.Add("linea " + i + " con algo de texto\n");
                parts.Add("@" + Cat);
                parts.Add("\n\n");
            }
            Story story = MakeStory(parts.ToArray());
            var chunks = Builder(300).Build(story);

            Assert.Greater(chunks.Count, 5);
            foreach (RichTextChunk c in chunks)
            {
                Assert.LessOrEqual(c.VisibleLength, 300);
                Assert.AreEqual(c.LinkCount, TmpVisibleText.LinkIds(c.Markup).Count);
            }
            Assert.AreEqual(200, TotalLinks(chunks));
            Assert.AreEqual(Visible(story), Reassemble(chunks));
        }

        [Test]
        public void ParagraphsAreKeptWhole_WhenTheyFit()
        {
            string p = new string('a', 200);
            Story story = MakeStory(p + "\n" + p + "\n" + p);
            var chunks = Builder(300).Build(story);
            Assert.AreEqual(3, chunks.Count);
            Assert.IsTrue(chunks[0].BreakAfter);
            Assert.IsTrue(chunks[1].BreakAfter);
            Assert.IsFalse(chunks[2].BreakAfter);
            Assert.AreEqual(Visible(story), Reassemble(chunks));
        }

        [Test]
        public void LongParagraph_CutAtSoftBreak()
        {
            string sentence = new string('あ', 99) + "。";
            Story story = MakeStory(sentence + sentence + sentence);
            var chunks = Builder(256).Build(story);
            Assert.AreEqual(2, chunks.Count);
            Assert.AreEqual(200, chunks[0].VisibleLength); // cut after the second full stop
            Assert.IsFalse(chunks[0].BreakAfter);
            Assert.AreEqual(Visible(story), Reassemble(chunks));
        }

        [Test]
        public void LongParagraph_NoSoftBreak_HardCutNoSurrogateSplit()
        {
            // U+20BB7 is a surrogate pair; the odd offset makes a pair straddle the limit.
            string pair = "𠮷";
            var sb = new StringBuilder("a");
            sb.Append('x', 254);
            for (int i = 0; i < 300; i++) sb.Append(pair);
            Story story = MakeStory(sb.ToString(), "@" + Cat, new string('y', 700));
            var chunks = Builder(256).Build(story);

            foreach (RichTextChunk c in chunks)
            {
                Assert.LessOrEqual(c.VisibleLength, 256);
                string vis = TmpVisibleText.Strip(c.Markup);
                if (vis.Length > 0)
                {
                    Assert.IsFalse(char.IsHighSurrogate(vis[vis.Length - 1]), "chunk ends in half a pair");
                    Assert.IsFalse(char.IsLowSurrogate(vis[0]), "chunk starts with half a pair");
                }
            }
            Assert.AreEqual(Visible(story), Reassemble(chunks));
            Assert.AreEqual(1, TotalLinks(chunks));
        }

        [Test]
        public void RoundTrip_ConcatWithBreakAfter_EqualsNormalizedText()
        {
            var parts = new List<string>();
            for (int i = 0; i < 80; i++)
            {
                parts.Add(new string('z', i * 7 % 150) + "\r\n");
                parts.Add(i % 3 == 0 ? "@" + Cat : "<i>\n");
                if (i % 10 == 0) parts.Add(new string('q', 400)); // oversize paragraph without breaks
            }
            Story story = MakeStory(parts.ToArray());
            Assert.AreEqual(Visible(story), Reassemble(Builder(256).Build(story)));
        }

        [Test]
        public void TryParseLinkId_RoundTrip_AndRejectsGarbage()
        {
            Assert.AreEqual("k42", StoryRichTextBuilder.LinkIdFor(42));
            Assert.IsTrue(StoryRichTextBuilder.TryParseLinkId("k42", out int n));
            Assert.AreEqual(42, n);
            Assert.IsTrue(StoryRichTextBuilder.TryParseLinkId("k0", out n));
            foreach (string bad in new[] { null, "", "k", "42", "k-1", "kx", "k1x", "K1", "k 1", "k+1", "k99999999999" })
            {
                Assert.IsFalse(StoryRichTextBuilder.TryParseLinkId(bad, out int r), bad);
                Assert.AreEqual(-1, r);
            }
        }

        [Test]
        public void Ctor_InvalidColorOrMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => new StoryRichTextBuilder(null));
            Assert.Throws<ArgumentException>(() => new StoryRichTextBuilder("blue"));
            Assert.Throws<ArgumentException>(() => new StoryRichTextBuilder("#12345"));
            Assert.Throws<ArgumentException>(() => new StoryRichTextBuilder("#12345G"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StoryRichTextBuilder(Blue, 255));
            Assert.DoesNotThrow(() => new StoryRichTextBuilder(Blue, 256));
        }

        [Test]
        public void Build_Fixture50k_KeepsAllTextAndLinks()
        {
            GeneratedFixture fx = StoryFixtureGenerator.Generate();
            Story story = new StoryParser().Parse("fixture", fx.Text).Story;
            var chunks = Builder().Build(story);

            Assert.AreEqual(fx.BlockCount, story.Entries.Count);
            Assert.AreEqual(Visible(story), Reassemble(chunks));

            var ids = new List<string>();
            foreach (RichTextChunk c in chunks)
            {
                Assert.LessOrEqual(c.VisibleLength, StoryRichTextBuilder.DefaultMaxChunkChars);
                ids.AddRange(TmpVisibleText.LinkIds(c.Markup));
            }
            Assert.AreEqual(fx.BlockCount, ids.Count);
            var kanji = new HashSet<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                Assert.AreEqual("k" + i, ids[i]);
                Assert.IsTrue(StoryRichTextBuilder.TryParseLinkId(ids[i], out int idx));
                kanji.Add(story.Entries[idx].Kanji);
            }
            Assert.AreEqual(200, kanji.Count);
        }

        [Test]
        public void Build_Fixture50k_UnderBudget()
        {
            GeneratedFixture fx = StoryFixtureGenerator.Generate();
            Story story = new StoryParser().Parse("fixture", fx.Text).Story;
            StoryRichTextBuilder builder = Builder();
            builder.Build(story); // warm-up (JIT)

            var sw = Stopwatch.StartNew();
            builder.Build(story);
            sw.Stop();
            TestContext.Out.WriteLine("[Kanji][Perf] rich-text-build 50k: " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");
            Assert.Less(sw.Elapsed.TotalMilliseconds, 50);
        }
    }
}
