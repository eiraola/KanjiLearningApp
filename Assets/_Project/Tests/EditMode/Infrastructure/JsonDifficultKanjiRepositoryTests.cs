using System;
using System.Collections.Generic;
using System.IO;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Infrastructure.Storage;
using NUnit.Framework;

namespace Kanji.Infrastructure.Tests
{
    public class JsonDifficultKanjiRepositoryTests
    {
        private string _root;
        private FaultyFileSystem _fs;
        private JsonDifficultKanjiRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _fs = new FaultyFileSystem();
            _repo = new JsonDifficultKanjiRepository(_fs, _root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private const string Id = "abc123";
        private string FilePath => Path.Combine(_root, "difficult", Id + ".json");

        private void WriteRaw(string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, json);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFieldsAndOrder()
        {
            var items = new List<KanjiEntry>
            {
                new KanjiEntry("勉強", "べんきょう", "estudio", "毎日\"勉強\"する\n<b>", "Estudio \U0001F600"),
                new KanjiEntry("生", "なま", "crudo", "", ""),
                new KanjiEntry("生", "せい", "vida", "x", "y"),
            };

            Assert.IsTrue(_repo.Save(Id, items).IsSuccess);
            IReadOnlyList<KanjiEntry> loaded = new JsonDifficultKanjiRepository(_fs, _root).Load(Id);

            Assert.AreEqual(items.Count, loaded.Count);
            for (int i = 0; i < items.Count; i++)
            {
                Assert.AreEqual(items[i].Kanji, loaded[i].Kanji);
                Assert.AreEqual(items[i].Reading, loaded[i].Reading);
                Assert.AreEqual(items[i].Meaning, loaded[i].Meaning);
                Assert.AreEqual(items[i].Sentence, loaded[i].Sentence);
                Assert.AreEqual(items[i].SentenceMeaning, loaded[i].SentenceMeaning);
            }
        }

        [Test]
        public void Save_LeavesNoTmpFile()
        {
            _repo.Save(Id, new List<KanjiEntry>());
            Assert.IsFalse(File.Exists(FilePath + ".tmp"));
            Assert.IsTrue(File.Exists(FilePath));
        }

        [Test]
        public void Save_Empty_LoadsEmpty()
        {
            Assert.IsTrue(_repo.Save(Id, new List<KanjiEntry>()).IsSuccess);
            Assert.AreEqual(0, _repo.Load(Id).Count);
        }

        [Test]
        public void Load_MissingFile_ReturnsEmpty()
        {
            Assert.AreEqual(0, _repo.Load(Id).Count);
        }

        [TestCase("{not json")]
        [TestCase("")]
        [TestCase("{\"version\":1,\"items\":[{\"kanji\":\"\"}]}")]
        [TestCase("{\"items\":[]}")]
        public void Load_Corrupt_ReturnsEmptyWithoutThrowing(string json)
        {
            WriteRaw(json);
            Assert.AreEqual(0, _repo.Load(Id).Count);
        }

        [TestCase("{not json")]
        [TestCase("{\"version\":1,\"items\":[{\"kanji\":\"\"}]}")]
        [TestCase("{\"version\":99,\"items\":[{\"kanji\":\"木\",\"reading\":\"き\"}]}")]
        public void Load_Unreadable_BacksUpOriginalToCorruptFile(string json)
        {
            WriteRaw(json);

            Assert.AreEqual(0, _repo.Load(Id).Count);

            Assert.IsTrue(File.Exists(FilePath + ".corrupt"));
            Assert.AreEqual(json, File.ReadAllText(FilePath + ".corrupt"));
        }

        [Test]
        public void Load_ValidFile_DoesNotCreateCorruptBackup()
        {
            _repo.Save(Id, new List<KanjiEntry> { new KanjiEntry("木", "き", "m", "", "") });
            _repo.Load(Id);
            Assert.IsFalse(File.Exists(FilePath + ".corrupt"));
        }

        [Test]
        public void Load_FutureVersion_ReturnsEmpty()
        {
            WriteRaw("{\"version\":99,\"items\":[{\"kanji\":\"木\",\"reading\":\"き\"}]}");
            Assert.AreEqual(0, _repo.Load(Id).Count);
        }

        [Test]
        public void Save_ReplaceFails_KeepsPreviousFileAndReportsFailure()
        {
            _repo.Save(Id, new List<KanjiEntry> { new KanjiEntry("木", "き", "m", "", "") });
            _fs.FailOnReplace = true;

            RepoResult r = _repo.Save(Id, new List<KanjiEntry> { new KanjiEntry("火", "ひ", "m", "", "") });
            _fs.FailOnReplace = false;

            Assert.IsFalse(r.IsSuccess);
            IReadOnlyList<KanjiEntry> loaded = _repo.Load(Id);
            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual("木", loaded[0].Kanji);
        }

        [Test]
        public void TwoStories_AreStoredIndependently_AndListed()
        {
            _repo.Save("aa", new List<KanjiEntry> { new KanjiEntry("木", "き", "m", "", "") });
            _repo.Save("bb", new List<KanjiEntry>());

            Assert.AreEqual(1, _repo.Load("aa").Count);
            Assert.AreEqual(0, _repo.Load("bb").Count);
            CollectionAssert.AreEquivalent(new[] { "aa", "bb" }, _repo.ListStoryIds());
        }

        [Test]
        public void Delete_RemovesFile_AndMissingIsSuccess()
        {
            _repo.Save(Id, new List<KanjiEntry> { new KanjiEntry("木", "き", "m", "", "") });
            Assert.IsTrue(_repo.Delete(Id).IsSuccess);
            Assert.IsFalse(File.Exists(FilePath));
            Assert.IsTrue(_repo.Delete(Id).IsSuccess);
            Assert.AreEqual(0, _repo.ListStoryIds().Count);
        }

        [Test]
        public void Migration_Marker_PersistsAcrossInstances()
        {
            Assert.IsFalse(_repo.IsMigrated());
            Assert.IsTrue(_repo.MarkMigrated(3).IsSuccess);
            Assert.IsTrue(new JsonDifficultKanjiRepository(_fs, _root).IsMigrated());
        }

        [Test]
        public void Legacy_LoadThenDelete()
        {
            Directory.CreateDirectory(_root);
            string legacy = Path.Combine(_root, "difficult_kanji.json");
            File.WriteAllText(legacy, "{\"version\":1,\"items\":[{\"kanji\":\"木\",\"reading\":\"き\",\"meaning\":\"m\",\"sentence\":\"\",\"sentenceMeaning\":\"\"}]}");

            Assert.IsTrue(_repo.LegacyExists());
            RepoResult<IReadOnlyList<KanjiEntry>> r = _repo.LoadLegacy();
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(1, r.Value.Count);
            Assert.IsTrue(_repo.DeleteLegacy().IsSuccess);
            Assert.IsFalse(_repo.LegacyExists());
        }

        [Test]
        public void Legacy_Corrupt_FailsAndKeepsFile()
        {
            Directory.CreateDirectory(_root);
            string legacy = Path.Combine(_root, "difficult_kanji.json");
            File.WriteAllText(legacy, "{not json");

            Assert.IsFalse(_repo.LoadLegacy().IsSuccess);
            Assert.IsTrue(File.Exists(legacy));
        }

        [Test]
        public void InvalidId_IsRejected()
        {
            Assert.IsFalse(_repo.Save("../x", new List<KanjiEntry>()).IsSuccess);
            Assert.IsFalse(_repo.Delete("..").IsSuccess);
            Assert.AreEqual(0, _repo.Load("../x").Count);
        }

        [Test]
        public void SaveWithProgress_RoundTrips()
        {
            var items = new List<KanjiEntry> { new KanjiEntry("木", "き", "", "", ""), new KanjiEntry("林", "はやし", "", "", "") };
            Assert.IsTrue(_repo.Save(Id, items, new List<int> { 3, 5 }).IsSuccess);
            CollectionAssert.AreEqual(new[] { 3, 5 }, _repo.LoadProgress(Id));
            Assert.AreEqual(2, _repo.Load(Id).Count);
        }

        [Test]
        public void OldFileWithoutSuccesses_LoadsZeroProgress()
        {
            WriteRaw("{\"version\":1,\"items\":[{\"kanji\":\"木\",\"reading\":\"き\"}]}");
            Assert.AreEqual(1, _repo.Load(Id).Count);
            CollectionAssert.AreEqual(new[] { 0 }, _repo.LoadProgress(Id));
        }
    }
}
