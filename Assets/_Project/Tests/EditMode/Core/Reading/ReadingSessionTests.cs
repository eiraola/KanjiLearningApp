using System.Collections.Generic;
using Kanji.Core.Model;
using Kanji.Core.Reading;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class ReadingSessionTests
    {
        private static KanjiEntry E(string k) => new KanjiEntry(k, "r", "m", "s", "sm");

        private static Story StoryOf(params string[] kanji)
        {
            var entries = new List<KanjiEntry>();
            foreach (string k in kanji) entries.Add(E(k));
            return new Story("id", "n", null, entries);
        }

        [Test]
        public void NoAnswers_AllPassed()
        {
            var stats = new ReadingSession(StoryOf("a", "b")).BuildStats();
            Assert.AreEqual(2, stats.Total);
            Assert.AreEqual(0, stats.Failed);
            Assert.AreEqual(100, stats.Percent);
        }

        [Test]
        public void RedCountsAsFailed_GreenDoesNot()
        {
            var story = StoryOf("a", "b", "c");
            var s = new ReadingSession(story);
            s.Record(story.Entries[0], KanjiAnswer.DidntKnow);
            s.Record(story.Entries[1], KanjiAnswer.Knew);
            var stats = s.BuildStats();
            Assert.AreEqual(1, stats.Failed);
            Assert.AreEqual(2, stats.Passed);
            Assert.AreEqual(67, stats.Percent);
            Assert.AreEqual("a", stats.FailedEntries[0].Kanji);
        }

        [Test]
        public void LastAnswerWins()
        {
            var story = StoryOf("a");
            var s = new ReadingSession(story);
            s.Record(story.Entries[0], KanjiAnswer.DidntKnow);
            s.Record(story.Entries[0], KanjiAnswer.Knew);
            Assert.AreEqual(0, s.BuildStats().Failed);
        }

        [Test]
        public void RepeatedKanjiCountOnce()
        {
            var story = StoryOf("a", "a", "b");
            var s = new ReadingSession(story);
            s.Record(story.Entries[1], KanjiAnswer.DidntKnow);
            var stats = s.BuildStats();
            Assert.AreEqual(2, stats.Total);
            Assert.AreEqual(1, stats.Failed);
            Assert.AreEqual(1, stats.FailedEntries.Count);
        }

        [Test]
        public void EmptyStory_ZeroPercent()
        {
            var stats = new ReadingSession(StoryOf()).BuildStats();
            Assert.AreEqual(0, stats.Total);
            Assert.AreEqual(0, stats.Percent);
        }
    }
}
