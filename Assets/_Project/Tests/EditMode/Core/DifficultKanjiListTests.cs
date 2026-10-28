using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class DifficultKanjiListTests
    {
        private sealed class FakeRepo : IDifficultKanjiRepository
        {
            public List<KanjiEntry> Stored = new List<KanjiEntry>();
            public bool FailSave;
            public RepoResult Delete(string storyId) => RepoResult.Ok();
            public IReadOnlyList<string> ListStoryIds() => new List<string>();
            public int SaveCount;

            public IReadOnlyList<KanjiEntry> Load(string storyId) => Stored;

            public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items)
            {
                SaveCount++;
                if (FailSave) return RepoResult.Fail(RepoError.IoError, "fail");
                Stored = new List<KanjiEntry>(items);
                return RepoResult.Ok();
            }
        }

        private static KanjiEntry E(string k) => new KanjiEntry(k, "r", "m", "s", "sm");

        [Test]
        public void Add_StoresCopyOfAllFields()
        {
            var list = new DifficultKanjiList(new FakeRepo(), "s");
            var original = new KanjiEntry("木", "き", "tree", "木がある", "There is a tree");

            Assert.AreEqual(DifficultAddResult.Added, list.Add(original));

            KanjiEntry stored = list.Items[0];
            Assert.AreNotSame(original, stored);
            Assert.AreEqual("木", stored.Kanji);
            Assert.AreEqual("き", stored.Reading);
            Assert.AreEqual("tree", stored.Meaning);
            Assert.AreEqual("木がある", stored.Sentence);
            Assert.AreEqual("There is a tree", stored.SentenceMeaning);
        }

        [Test]
        public void Add_SameKanjiDifferentReading_AreTwoEntries()
        {
            var list = new DifficultKanjiList(new FakeRepo(), "s");
            var a = new KanjiEntry("生", "せい", "vida", "", "");
            var b = new KanjiEntry("生", "なま", "crudo", "", "");

            Assert.AreEqual(DifficultAddResult.Added, list.Add(a));
            Assert.AreEqual(DifficultAddResult.Added, list.Add(b));
            Assert.AreEqual(DifficultAddResult.AlreadyPresent, list.Add(a));

            Assert.AreEqual(2, list.Items.Count);
            Assert.IsTrue(list.Remove(a));
            Assert.IsTrue(list.Contains(b));
            Assert.IsFalse(list.Contains(a));
        }

        [Test]
        public void Add_Duplicate_ReturnsAlreadyPresent_AndDoesNotChange()
        {
            var repo = new FakeRepo();
            var list = new DifficultKanjiList(repo, "s");
            list.Add(E("木"));
            int rev = list.Revision, saves = repo.SaveCount;

            Assert.AreEqual(DifficultAddResult.AlreadyPresent, list.Add(E("木")));

            Assert.AreEqual(1, list.Items.Count);
            Assert.AreEqual(rev, list.Revision);
            Assert.AreEqual(saves, repo.SaveCount);
        }

        [Test]
        public void Contains_And_Remove_Work()
        {
            var list = new DifficultKanjiList(new FakeRepo(), "s");
            list.Add(E("木"));
            Assert.IsTrue(list.Contains(E("木")));

            Assert.IsTrue(list.Remove(E("木")));
            Assert.IsFalse(list.Contains(E("木")));
            Assert.AreEqual(0, list.Items.Count);
            Assert.IsFalse(list.Remove(E("木")));
        }

        [Test]
        public void Items_KeepInsertionOrder()
        {
            var list = new DifficultKanjiList(new FakeRepo(), "s");
            list.Add(E("一"));
            list.Add(E("二"));
            list.Add(E("三"));
            list.Remove(E("二"));

            Assert.AreEqual("一", list.Items[0].Kanji);
            Assert.AreEqual("三", list.Items[1].Kanji);
        }

        [Test]
        public void Add_SaveFails_RevertsAndReturnsSaveFailed()
        {
            var repo = new FakeRepo { FailSave = true };
            var list = new DifficultKanjiList(repo, "s");

            Assert.AreEqual(DifficultAddResult.SaveFailed, list.Add(E("木")));

            Assert.AreEqual(0, list.Items.Count);
            Assert.IsFalse(list.Contains(E("木")));
            Assert.AreEqual(0, list.Revision);
        }

        [Test]
        public void Remove_SaveFails_RevertsAndReturnsFalse()
        {
            var repo = new FakeRepo();
            var list = new DifficultKanjiList(repo, "s");
            list.Add(E("一"));
            list.Add(E("二"));
            int rev = list.Revision;
            repo.FailSave = true;

            Assert.IsFalse(list.Remove(E("一")));

            Assert.AreEqual(2, list.Items.Count);
            Assert.AreEqual("一", list.Items[0].Kanji);
            Assert.IsTrue(list.Contains(E("一")));
            Assert.AreEqual(rev, list.Revision);
        }

        [Test]
        public void Revision_IncrementsOnEachChange()
        {
            var list = new DifficultKanjiList(new FakeRepo(), "s");
            Assert.AreEqual(0, list.Revision);
            list.Add(E("木"));
            Assert.AreEqual(1, list.Revision);
            list.Remove(E("木"));
            Assert.AreEqual(2, list.Revision);
        }

        [Test]
        public void Constructor_LoadsFromRepo_AndNewInstanceSeesSavedData()
        {
            var repo = new FakeRepo();
            new DifficultKanjiList(repo, "s").Add(E("木"));

            var second = new DifficultKanjiList(repo, "s");

            Assert.AreEqual(1, second.Items.Count);
            Assert.IsTrue(second.Contains(E("木")));
        }
    }
}
