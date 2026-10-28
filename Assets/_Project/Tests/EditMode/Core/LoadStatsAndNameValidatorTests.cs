using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class LoadStatsAndNameValidatorTests
    {
        private static Story MakeStory(int entries, params Segment[] segments)
        {
            var list = new List<KanjiEntry>();
            for (int i = 0; i < entries; i++) list.Add(new KanjiEntry("k" + i, "", "", "", ""));
            return new Story("id", "n", segments, list);
        }

        private static ParseReport Report(int warnings)
        {
            var w = new List<ParseWarning>();
            for (int i = 0; i < warnings; i++) w.Add(new ParseWarning(ParseWarningKind.UnknownField, i + 1, ""));
            return new ParseReport(w, 0, 0);
        }

        [Test]
        public void Stats_CountsKanjisCharactersAndWarnings()
        {
            Story s = MakeStory(2, Segment.Plain("ab c\n"), Segment.ForKanji("日本", 0), Segment.Plain("　d"));
            LoadStats st = LoadStats.From(s, Report(3));
            Assert.AreEqual(2, st.Kanjis);
            Assert.AreEqual(6, st.Characters); // a b c 日 本 d
            Assert.AreEqual(3, st.Warnings);
        }

        [Test]
        public void Stats_SurrogatePairCountsOnce()
        {
            Story s = MakeStory(1, Segment.ForKanji("\U00020BB7", 0), Segment.Plain("a"));
            Assert.AreEqual(2, LoadStats.From(s, null).Characters);
        }

        [Test]
        public void Stats_NullReportMeansZeroWarnings()
        {
            Assert.AreEqual(0, LoadStats.From(MakeStory(0), null).Warnings);
        }

        private static readonly IReadOnlyList<StoryInfo> Existing = new[]
        {
            new StoryInfo("a", "Cuento", 1, DateTime.MinValue),
            new StoryInfo("b", "Otra", 1, DateTime.MinValue),
        };

        [Test]
        public void Validate_EmptyUsesFallback()
        {
            NameResult r = StoryNameValidator.Validate("   ", Existing, " Nueva ", null);
            Assert.AreEqual(NameCheck.Ok, r.Check);
            Assert.AreEqual("Nueva", r.Name);
        }

        [Test]
        public void Validate_EmptyWithoutFallbackIsEmpty()
        {
            Assert.AreEqual(NameCheck.Empty, StoryNameValidator.Validate("", Existing, null, "a").Check);
            Assert.AreEqual(NameCheck.Empty, StoryNameValidator.Validate(null, Existing, "  ", null).Check);
        }

        [Test]
        public void Validate_DuplicateIgnoresCaseAndTrim()
        {
            NameResult r = StoryNameValidator.Validate("  cUENTO ", Existing, null, null);
            Assert.AreEqual(NameCheck.Duplicate, r.Check);
            Assert.AreEqual("Cuento", r.ConflictingName);
            Assert.AreEqual("cUENTO", r.Name);
        }

        [Test]
        public void Validate_DuplicateAppliesToFallback()
        {
            Assert.AreEqual(NameCheck.Duplicate, StoryNameValidator.Validate("", Existing, "otra", null).Check);
        }

        [Test]
        public void Validate_OwnNameIsNotDuplicateWhenRenaming()
        {
            Assert.AreEqual(NameCheck.Ok, StoryNameValidator.Validate("Cuento", Existing, null, "a").Check);
            Assert.AreEqual(NameCheck.Ok, StoryNameValidator.Validate("CUENTO", Existing, null, "a").Check);
            Assert.AreEqual(NameCheck.Duplicate, StoryNameValidator.Validate("Otra", Existing, null, "a").Check);
        }

        [Test]
        public void Validate_NullExistingIsOk()
        {
            Assert.AreEqual(NameCheck.Ok, StoryNameValidator.Validate("X", null, null, null).Check);
        }
    }
}
