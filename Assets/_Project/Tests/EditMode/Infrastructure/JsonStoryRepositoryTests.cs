using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Infrastructure.Storage;
using NUnit.Framework;

namespace Kanji.Infrastructure.Tests
{
    public class JsonStoryRepositoryTests
    {
        private string _root;
        private FaultyFileSystem _fs;
        private JsonStoryRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _fs = new FaultyFileSystem();
            _repo = new JsonStoryRepository(_fs, _root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private string FileOf(string id) => Path.Combine(_root, "stories", id + ".json");

        private void WriteRaw(string id, string json)
        {
            Directory.CreateDirectory(Path.Combine(_root, "stories"));
            File.WriteAllText(FileOf(id), json, new UTF8Encoding(false));
        }

        private static Story Sample(string id = "s1", string name = "Mi historia")
        {
            var entries = new List<KanjiEntry>
            {
                new KanjiEntry("勉強", "べんきょう", "estudio", "毎日10:30に勉強する \"ok\" <b>", "Estudio a las 10:30\nlinea 2 \U0001F600"),
                new KanjiEntry("日", "ひ", "dia", "", ""),
            };
            var segments = new List<Segment>
            {
                Segment.Plain("Hoy 10:30 <\"hola\">\n\U0001F600 "),
                Segment.ForKanji("勉強", 0),
                Segment.ForKanji("日", 1),
            };
            return new Story(id, name, segments, entries);
        }

        private static void AssertSame(Story a, Story b)
        {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.Entries.Count, b.Entries.Count);
            for (int i = 0; i < a.Entries.Count; i++)
            {
                Assert.AreEqual(a.Entries[i].Kanji, b.Entries[i].Kanji);
                Assert.AreEqual(a.Entries[i].Reading, b.Entries[i].Reading);
                Assert.AreEqual(a.Entries[i].Meaning, b.Entries[i].Meaning);
                Assert.AreEqual(a.Entries[i].Sentence, b.Entries[i].Sentence);
                Assert.AreEqual(a.Entries[i].SentenceMeaning, b.Entries[i].SentenceMeaning);
            }
            Assert.AreEqual(a.Segments.Count, b.Segments.Count);
            for (int i = 0; i < a.Segments.Count; i++)
            {
                Assert.AreEqual(a.Segments[i].Kind, b.Segments[i].Kind);
                Assert.AreEqual(a.Segments[i].Text, b.Segments[i].Text);
                Assert.AreEqual(a.Segments[i].EntryIndex, b.Segments[i].EntryIndex);
            }
        }

        [Test]
        public void SaveLoad_RoundTrip_Identical()
        {
            Story s = Sample();
            Assert.IsTrue(_repo.Save(s).IsSuccess);
            RepoResult<Story> r = _repo.Load("s1");
            Assert.IsTrue(r.IsSuccess, r.Message);
            AssertSame(s, r.Value);
        }

        private StoryLibrary NewLibrary(string id) =>
            new StoryLibrary(_repo, () => id, () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        private string[] FilesOnDisk() => Directory.GetFiles(Path.Combine(_root, "stories"));

        [Test]
        public void ImportConflict_Cancel_LeavesNoTmpAndSameFiles()
        {
            Assert.IsTrue(NewLibrary("a1").Import(Sample("", "X"), "X").Status == ImportStatus.Imported);
            string[] before = FilesOnDisk();
            string content = File.ReadAllText(before[0]);

            var lib = NewLibrary("a2");
            Assert.AreEqual(ImportStatus.Duplicate, lib.Import(Sample("", "X"), "x").Status);

            string[] after = FilesOnDisk();
            CollectionAssert.AreEquivalent(before, after);
            Assert.AreEqual(content, File.ReadAllText(after[0]));
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(_root, "stories"), "*.tmp"));
        }

        [Test]
        public void ImportDistinctName_LeavesTwoFilesNoTmp()
        {
            NewLibrary("a1").Import(Sample("", "X"), "X");
            ImportResult r = NewLibrary("a2").Import(Sample("", "X"), "Y");
            Assert.AreEqual(ImportStatus.Imported, r.Status);
            Assert.AreEqual(2, FilesOnDisk().Length);
            Assert.AreEqual("a2", r.Story.Id);
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(_root, "stories"), "*.tmp"));
        }

        [Test]
        public void SaveTwice_Overwrites()
        {
            _repo.Save(Sample("s1", "Uno"));
            _repo.Save(Sample("s1", "Dos"));
            Assert.AreEqual("Dos", _repo.Load("s1").Value.Name);
            Assert.AreEqual(1, _repo.List().Items.Count);
        }

        [Test]
        public void Delete_RemovesFile()
        {
            _repo.Save(Sample());
            Assert.IsTrue(_repo.Delete("s1").IsSuccess);
            Assert.IsFalse(File.Exists(FileOf("s1")));
            Assert.AreEqual(RepoError.NotFound, _repo.Load("s1").Error);
        }

        [Test]
        public void Delete_ThenList_DoesNotContain()
        {
            _repo.Save(Sample("s1"));
            _repo.Save(Sample("s2", "Otra"));
            _repo.Delete("s1");
            var items = _repo.List().Items;
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("s2", items[0].Id);
        }

        [Test]
        public void Delete_PersistsAcrossNewRepositoryInstance()
        {
            _repo.Save(Sample("s1"));
            _repo.Delete("s1");
            var fresh = new JsonStoryRepository(new FaultyFileSystem(), _root);
            Assert.AreEqual(0, fresh.List().Items.Count);
            Assert.AreEqual(RepoError.NotFound, fresh.Load("s1").Error);
        }

        [Test]
        public void Delete_Nonexistent_Ok()
        {
            Assert.IsTrue(_repo.Delete("nope").IsSuccess);
        }

        [Test]
        public void Load_Nonexistent_NotFound()
        {
            Assert.AreEqual(RepoError.NotFound, _repo.Load("nope").Error);
        }

        [Test]
        public void Load_ExtraField_Tolerated()
        {
            WriteRaw("a", "{\"version\":1,\"id\":\"a\",\"name\":\"N\",\"future\":{\"x\":1},\"entries\":[],\"segments\":[{\"kind\":0,\"text\":\"hi\",\"entry\":-1,\"extra\":5}]}");
            RepoResult<Story> r = _repo.Load("a");
            Assert.IsTrue(r.IsSuccess, r.Message);
            Assert.AreEqual("hi", r.Value.Segments[0].Text);
        }

        [Test]
        public void Load_MissingOptionalField_Tolerated()
        {
            WriteRaw("a", "{\"version\":1,\"id\":\"a\",\"entries\":[{\"kanji\":\"日\"}],\"segments\":[{\"kind\":1,\"entry\":0}]}");
            RepoResult<Story> r = _repo.Load("a");
            Assert.IsTrue(r.IsSuccess, r.Message);
            Assert.AreEqual("", r.Value.Name);
            Assert.AreEqual("", r.Value.Entries[0].Reading);
            Assert.AreEqual("日", r.Value.Segments[0].Text);
        }

        [TestCase("garbage ###")]
        [TestCase("{\"version\":1,\"id\":\"a\",\"entries\":[{\"kanji\"")]
        [TestCase("")]
        [TestCase("null")]
        [TestCase("{}")]
        public void Load_CorruptJson_ReturnsCorrupt(string json)
        {
            WriteRaw("a", json);
            Assert.AreEqual(RepoError.Corrupt, _repo.Load("a").Error);
        }

        [Test]
        public void Load_FutureVersion_UnsupportedVersion()
        {
            WriteRaw("a", "{\"version\":99,\"id\":\"a\",\"name\":\"N\"}");
            Assert.AreEqual(RepoError.UnsupportedVersion, _repo.Load("a").Error);
        }

        [Test]
        public void Load_BadEntryIndex_Corrupt()
        {
            WriteRaw("a", "{\"version\":1,\"id\":\"a\",\"entries\":[],\"segments\":[{\"kind\":1,\"text\":\"\",\"entry\":3}]}");
            Assert.AreEqual(RepoError.Corrupt, _repo.Load("a").Error);
        }

        [Test]
        public void Load_IdMismatchWithFileName_Corrupt()
        {
            WriteRaw("a", "{\"version\":1,\"id\":\"b\",\"name\":\"N\"}");
            Assert.AreEqual(RepoError.Corrupt, _repo.Load("a").Error);
        }

        [TestCase("..\\x")]
        [TestCase("a/b")]
        [TestCase("")]
        public void Save_InvalidId_InvalidId(string id)
        {
            Assert.AreEqual(RepoError.InvalidId, _repo.Save(Sample(id)).Error);
        }

        [Test]
        public void Save_WriteFailure_KeepsPreviousStory()
        {
            _repo.Save(Sample("s1", "Original"));
            _fs.FailOnReplace = true;
            RepoResult r = _repo.Save(Sample("s1", "Nueva"));
            Assert.AreEqual(RepoError.IoError, r.Error);
            Assert.AreEqual("Original", _repo.Load("s1").Value.Name);
        }

        [Test]
        public void List_StableOrder_SkipsCorruptAndTmp()
        {
            _repo.Save(Sample("c", "beta"));
            _repo.Save(Sample("a", "Alfa"));
            _repo.Save(Sample("b", "alfa"));
            WriteRaw("bad", "not json");
            File.WriteAllText(FileOf("c") + ".tmp", "partial");

            StoryListing listing = _repo.List();
            string[] ids = Ids(listing.Items);
            Array.Sort(ids, StringComparer.Ordinal); // the repository no longer orders
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, ids);
            Assert.AreEqual(1, listing.UnreadableCount);
            Assert.AreEqual(2, listing.Items[0].EntryCount);
        }

        [Test]
        public void SaveLoad_RoundTripsImportedAtAndEntryCount()
        {
            var at = new DateTime(2026, 3, 4, 5, 6, 7, 89, DateTimeKind.Utc);
            Story s = Sample().With("s1", "Mi historia", at);
            Assert.IsTrue(_repo.Save(s).IsSuccess);
            Assert.AreEqual(at, _repo.Load("s1").Value.ImportedAtUtc);
            StoryInfo info = _repo.List().Items[0];
            Assert.AreEqual(at, info.ImportedAtUtc);
            Assert.AreEqual(2, info.EntryCount);
        }

        [Test]
        public void List_ReadsHeaderOnly_V2()
        {
            // Heavy lists are garbage: a header-only read must still list it (Load would fail).
            WriteRaw("a", "{\"version\":2,\"id\":\"a\",\"name\":\"N\",\"importedAtUnixMs\":1000,\"entryCount\":7,\"entries\":[{\"kanji\":\"\"}],\"segments\":[]}");
            WriteRaw("b", "{\"version\":2,\"id\":\"b\",\"name\":\"M\",\"importedAtUnixMs\":2000,\"entryCount\":3,\"entries\":[],\"segments\":[]}");
            StoryListing l = _repo.List();
            Assert.AreEqual(0, l.UnreadableCount);
            Assert.AreEqual(2, l.Items.Count);
            StoryInfo a = l.Items[0].Id == "a" ? l.Items[0] : l.Items[1];
            Assert.AreEqual(7, a.EntryCount);
            Assert.AreEqual("N", a.Name);
            Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1000).UtcDateTime, a.ImportedAtUtc);
        }

        [Test]
        public void List_V1File_FallsBackAndCounts()
        {
            WriteRaw("a", "{\"version\":1,\"id\":\"a\",\"name\":\"Old\",\"entries\":[{\"kanji\":\"日\"},{\"kanji\":\"月\"}],\"segments\":[]}");
            StoryListing l = _repo.List();
            Assert.AreEqual(1, l.Items.Count);
            Assert.AreEqual(2, l.Items[0].EntryCount);
            Assert.AreEqual(DateTime.MinValue, l.Items[0].ImportedAtUtc);
        }

        [Test]
        public void List_CorruptFile_SkippedAndCounted()
        {
            _repo.Save(Sample("ok"));
            WriteRaw("bad", "not json");
            WriteRaw("bad2", "{\"version\":2,\"id\":\"other\",\"name\":\"x\",\"entryCount\":1}");
            WriteRaw("bad3", "{\"version\":2,\"id\":\"bad3\",\"name\":\"x\",\"entryCount\":-1}");
            StoryListing l = _repo.List();
            Assert.AreEqual(1, l.Items.Count);
            Assert.AreEqual(3, l.UnreadableCount);
        }

        [Test]
        public void List_FutureVersion_Counted()
        {
            WriteRaw("a", "{\"version\":99,\"id\":\"a\",\"name\":\"N\"}");
            StoryListing l = _repo.List();
            Assert.AreEqual(0, l.Items.Count);
            Assert.AreEqual(1, l.UnreadableCount);
        }

        [Test]
        public void List_IgnoresTmpFiles()
        {
            _repo.Save(Sample("a"));
            File.WriteAllText(FileOf("a") + ".tmp", "partial");
            File.WriteAllText(FileOf("z") + ".tmp", "partial");
            StoryListing l = _repo.List();
            Assert.AreEqual(1, l.Items.Count);
            Assert.AreEqual(0, l.UnreadableCount);
        }

        [Test]
        public void List_200Stories_UnderBudget()
        {
            var repo = new JsonStoryRepository(new DiskFileSystem(), _root);
            var parser = new Kanji.Core.Parsing.StoryParser();
            var at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 200; i++)
            {
                var fx = Kanji.Core.Tests.StoryFixtureGenerator.Generate(1000 + i, 5000, 20);
                Story s = parser.Parse("Historia " + i, fx.Text).Story.With("s" + i, "Historia " + i, at.AddMinutes(i));
                Assert.IsTrue(repo.Save(s).IsSuccess);
            }

            repo.List(); // warm-up (JIT, file cache)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            StoryListing l = repo.List();
            sw.Stop();
            UnityEngine.Debug.Log("[Kanji][Perf] List() 200 stories: " + sw.ElapsedMilliseconds + " ms");
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "kanji_list200_ms.txt"), sw.ElapsedMilliseconds.ToString());
            Assert.AreEqual(200, l.Items.Count);
            Assert.AreEqual(0, l.UnreadableCount);
            Assert.Less(sw.ElapsedMilliseconds, 300);
        }

        private static string[] Ids(IReadOnlyList<StoryInfo> list)
        {
            var ids = new string[list.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = list[i].Id;
            return ids;
        }

        [Test]
        public void List_EmptyOrMissingFolder_Empty()
        {
            Assert.AreEqual(0, _repo.List().Items.Count);
            Directory.CreateDirectory(Path.Combine(_root, "stories"));
            Assert.AreEqual(0, _repo.List().Items.Count);
        }

        [Test]
        public void Save_LargeStory_50kChars_Ok()
        {
            var entries = new List<KanjiEntry> { new KanjiEntry("日", "ひ", "dia", "s", "m") };
            var sb = new StringBuilder();
            for (int i = 0; i < 5000; i++) sb.Append("0123456789");
            var segments = new List<Segment> { Segment.Plain(sb.ToString()), Segment.ForKanji("日", 0) };
            var s = new Story("big", "Grande", segments, entries);
            Assert.IsTrue(_repo.Save(s).IsSuccess);
            RepoResult<Story> r = _repo.Load("big");
            Assert.IsTrue(r.IsSuccess, r.Message);
            Assert.AreEqual(50000, r.Value.Segments[0].Text.Length);
        }
    }
}
