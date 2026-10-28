using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class DifficultStudyTests
    {
        private sealed class ProgressRepo : IDifficultKanjiRepository
        {
            public List<KanjiEntry> Stored = new List<KanjiEntry>();
            public List<int> Progress;
            public bool FailSave;

            public IReadOnlyList<KanjiEntry> Load(string storyId) => Stored;
            public IReadOnlyList<int> LoadProgress(string storyId) => Progress;
            public RepoResult Delete(string storyId) => RepoResult.Ok();
            public IReadOnlyList<string> ListStoryIds() => new List<string>();
            public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items) => Save(storyId, items, null);

            public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items, IReadOnlyList<int> progress)
            {
                if (FailSave) return RepoResult.Fail(RepoError.IoError, "fail");
                Stored = new List<KanjiEntry>(items);
                Progress = progress == null ? null : new List<int>(progress);
                return RepoResult.Ok();
            }
        }

        private static KanjiEntry E(string k) => new KanjiEntry(k, "r", "m", "s", "sm");

        private static DifficultKanjiList ListOf(ProgressRepo repo, params string[] kanji)
        {
            var list = new DifficultKanjiList(repo, "s");
            foreach (string k in kanji) list.Add(E(k));
            return list;
        }

        [Test]
        public void RecordStudy_GreenAddsUpToFive_RedLeavesUnchanged()
        {
            var repo = new ProgressRepo();
            var list = ListOf(repo, "木");
            Assert.AreEqual(0, list.ProgressOf(E("木")));

            for (int i = 0; i < 7; i++) Assert.IsTrue(list.RecordStudy(E("木"), true));
            Assert.AreEqual(DifficultKanjiList.MaxProgress, list.ProgressOf(E("木")));

            Assert.IsTrue(list.RecordStudy(E("木"), false));
            Assert.AreEqual(5, list.ProgressOf(E("木")));
            CollectionAssert.AreEqual(new[] { 5 }, repo.Progress);
        }

        [Test]
        public void RecordStudy_SaveFails_Reverts()
        {
            var repo = new ProgressRepo();
            var list = ListOf(repo, "木");
            repo.FailSave = true;
            Assert.IsFalse(list.RecordStudy(E("木"), true));
            Assert.AreEqual(0, list.ProgressOf(E("木")));
        }

        [Test]
        public void Progress_SurvivesReloadAndFollowsRemove()
        {
            var repo = new ProgressRepo();
            var list = ListOf(repo, "木", "林");
            list.RecordStudy(E("林"), true);
            list.RecordStudy(E("林"), true);
            list.Remove(E("木"));

            var reloaded = new DifficultKanjiList(repo, "s");
            Assert.AreEqual(2, reloaded.ProgressOf(E("林")));
            Assert.AreEqual(0, reloaded.ProgressOf(E("木")));
        }

        [Test]
        public void Load_WithoutProgress_IsZero()
        {
            var repo = new ProgressRepo { Stored = new List<KanjiEntry> { E("木") }, Progress = null };
            Assert.AreEqual(0, new DifficultKanjiList(repo, "s").ProgressOf(E("木")));
        }

        [Test]
        public void Session_VisitsEveryKanjiOnce_AndBuildsStats()
        {
            var repo = new ProgressRepo();
            var list = ListOf(repo, "一", "二", "三", "四");
            var session = new DifficultStudySession(list, new System.Random(7));

            var seen = new HashSet<string>();
            bool knew = true;
            while (!session.IsFinished)
            {
                Assert.IsTrue(seen.Add(session.Current.Kanji));
                session.Record(knew);
                knew = !knew;
            }
            Assert.AreEqual(4, seen.Count);
            Assert.IsNull(session.Current);

            var stats = session.BuildStats();
            Assert.AreEqual(4, stats.Total);
            Assert.AreEqual(2, stats.Failed);
            Assert.AreEqual(50, stats.Percent);
        }

        [Test]
        public void Session_StoppedEarly_CountsOnlyAnswered()
        {
            var list = ListOf(new ProgressRepo(), "一", "二", "三");
            var session = new DifficultStudySession(list, new System.Random(1));
            KanjiEntry first = session.Current;
            session.Record(true);

            Assert.AreEqual(1, session.BuildStats().Total);
            Assert.AreEqual(100, session.BuildStats().Percent);
            Assert.AreEqual(1, list.ProgressOf(first));
        }

        [Test]
        public void Session_OrderIsShuffled()
        {
            var list = ListOf(new ProgressRepo(), "一", "二", "三", "四", "五", "六", "七", "八");
            bool differs = false;
            for (int seed = 0; seed < 10 && !differs; seed++)
            {
                var order = new DifficultStudySession(list, new System.Random(seed)).Order;
                for (int i = 0; i < order.Count; i++)
                    if (!ReferenceEquals(order[i], list.Items[i])) differs = true;
            }
            Assert.IsTrue(differs);
        }
    }
}
