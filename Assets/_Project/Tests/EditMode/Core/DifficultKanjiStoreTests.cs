using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class DifficultKanjiStoreTests
    {
        private sealed class MemRepo : IDifficultKanjiRepository
        {
            public readonly Dictionary<string, List<KanjiEntry>> Data = new Dictionary<string, List<KanjiEntry>>();
            public IReadOnlyList<KanjiEntry> Load(string id) =>
                Data.TryGetValue(id, out var l) ? l : new List<KanjiEntry>();
            public RepoResult Save(string id, IReadOnlyList<KanjiEntry> items)
            {
                Data[id] = new List<KanjiEntry>(items);
                return RepoResult.Ok();
            }
            public RepoResult Delete(string id) { Data.Remove(id); return RepoResult.Ok(); }
            public IReadOnlyList<string> ListStoryIds() => new List<string>(Data.Keys);
        }

        private static KanjiEntry E(string k, string r = "r") => new KanjiEntry(k, r, "m", "s", "sm");

        [Test]
        public void For_IsCached()
        {
            var store = new DifficultKanjiStore(new MemRepo());
            Assert.AreSame(store.For("a"), store.For("a"));
            Assert.AreNotSame(store.For("a"), store.For("b"));
        }

        [Test]
        public void AddRemoveContainsCount_PerStory()
        {
            var store = new DifficultKanjiStore(new MemRepo());
            Assert.AreEqual(DifficultAddResult.Added, store.For("a").Add(E("木")));
            Assert.IsTrue(store.For("a").Contains(E("木")));
            Assert.AreEqual(1, store.Count("a"));
            Assert.IsTrue(store.For("a").Remove(E("木")));
            Assert.AreEqual(0, store.Count("a"));
        }

        [Test]
        public void NoDuplicatesWithinStory_ByKanjiAndReading()
        {
            var store = new DifficultKanjiStore(new MemRepo());
            store.For("a").Add(E("生", "せい"));
            Assert.AreEqual(DifficultAddResult.AlreadyPresent, store.For("a").Add(E("生", "せい")));
            Assert.AreEqual(DifficultAddResult.Added, store.For("a").Add(E("生", "なま")));
            Assert.AreEqual(2, store.Count("a"));
        }

        [Test]
        public void SameKanjiInTwoStories_IsIndependent()
        {
            var store = new DifficultKanjiStore(new MemRepo());
            store.For("a").Add(E("木"));
            store.For("b").Add(E("木"));
            store.For("a").Remove(E("木"));
            Assert.IsFalse(store.For("a").Contains(E("木")));
            Assert.IsTrue(store.For("b").Contains(E("木")));
        }

        [Test]
        public void StoryIdsWithItems_ListsOnlyNonEmpty()
        {
            var store = new DifficultKanjiStore(new MemRepo());
            store.For("a").Add(E("木"));
            store.For("b").Add(E("火"));
            store.For("b").Remove(E("火"));
            CollectionAssert.AreEqual(new[] { "a" }, store.StoryIdsWithItems());
        }

        [Test]
        public void DeleteStory_RemovesDataAndKeepsRevisionMonotonic()
        {
            var repo = new MemRepo();
            var store = new DifficultKanjiStore(repo);
            store.For("a").Add(E("木"));
            int before = store.Revision;

            Assert.IsTrue(store.DeleteStory("a").IsSuccess);

            Assert.IsFalse(repo.Data.ContainsKey("a"));
            Assert.AreEqual(0, store.Count("a"));
            Assert.Greater(store.Revision, before);
        }

        [Test]
        public void PersistsAcrossStoreInstances()
        {
            var repo = new MemRepo();
            new DifficultKanjiStore(repo).For("a").Add(E("木"));
            Assert.IsTrue(new DifficultKanjiStore(repo).For("a").Contains(E("木")));
        }
    }
}
