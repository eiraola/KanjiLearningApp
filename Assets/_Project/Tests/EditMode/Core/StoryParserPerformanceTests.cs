using System.Diagnostics;
using System.Linq;
using System.Text;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class StoryParserPerformanceTests
    {
        [Test]
        public void Generator_SameSeed_SameText()
        {
            Assert.AreEqual(StoryFixtureGenerator.Generate(7).Text, StoryFixtureGenerator.Generate(7).Text);
        }

        [Test]
        public void Generator_DifferentSeed_DifferentText()
        {
            Assert.AreNotEqual(StoryFixtureGenerator.Generate(1).Text, StoryFixtureGenerator.Generate(2).Text);
        }

        [Test]
        public void Fixture_Has200ExpectedKeys_AndAtLeast50000Chars()
        {
            var f = StoryFixtureGenerator.Generate();
            Assert.AreEqual(200, f.ExpectedKanji.Distinct().Count());
            Assert.GreaterOrEqual(f.Text.Length, 50000);
            Assert.LessOrEqual(f.Text.Length, 50500);
        }

        [Test]
        public void Parse_Fixture_Under1Second()
        {
            var f = StoryFixtureGenerator.Generate();
            var parser = new StoryParser();
            parser.Parse("warmup", "{Kanji: 日; Reading: ひ; Meaning: sun}");

            var sw = Stopwatch.StartNew();
            ParseResult r = parser.Parse("fixture", f.Text);
            sw.Stop();
            TestContext.WriteLine($"PARSE_MS={sw.Elapsed.TotalMilliseconds:F2} chars={f.Text.Length}");

            Assert.Less(sw.ElapsedMilliseconds, 1000);
            Assert.AreEqual(0, r.Report.Warnings.Count);
            Assert.AreEqual(200, r.Report.DistinctKanjiCount);
            Assert.AreEqual(f.BlockCount, r.Report.BlockCount);
            var keys = r.Story.Entries.Select(e => e.Key).Distinct().Select(k => k.Value).ToList();
            CollectionAssert.AreEqual(f.ExpectedKanji.ToList(), keys);
        }

        [Test]
        public void Parse_Fixture_ConcatenatedText_Preserved()
        {
            var f = StoryFixtureGenerator.Generate();
            Story story = new StoryParser().Parse("fixture", f.Text).Story;

            var expected = new StringBuilder();
            bool inBlock = false;
            foreach (char c in f.Text)
            {
                if (c == '{') inBlock = true;
                else if (c == '}') inBlock = false;
                else if (!inBlock) expected.Append(c);
            }
            var actual = new StringBuilder();
            foreach (var s in story.Segments)
                if (s.Kind == SegmentKind.Text) actual.Append(s.Text);

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }
    }
}
