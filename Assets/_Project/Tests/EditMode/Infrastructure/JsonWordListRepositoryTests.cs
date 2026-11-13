using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kanji.Core.Library;
using Kanji.Core.Words;
using Kanji.Infrastructure.Storage;
using NUnit.Framework;

namespace Kanji.Infrastructure.Tests
{
    public class JsonWordListRepositoryTests
    {
        private string _root;
        private JsonWordListRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _repo = new JsonWordListRepository(new DiskFileSystem(), _root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private static WordList Sample() => new WordList("l1", "Lección \"1\"", new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc),
            new List<WordEntry>
            {
                new WordEntry("二重丸", "にじゅうまる", "Doble círculo", "作文のテストで二重丸をもらいました。", "さくぶんの…", "Recibí\nuna marca \U0001F600"),
                new WordEntry("～列目", "～れつめ", "Fila número...", "", "", ""),
            });

        [Test]
        public void SaveLoad_RoundTrip_AllFields()
        {
            Assert.IsTrue(_repo.Save(Sample()).IsSuccess);
            RepoResult<WordList> r = _repo.Load("l1");
            Assert.IsTrue(r.IsSuccess, r.Message);
            WordList l = r.Value;
            Assert.AreEqual("Lección \"1\"", l.Name);
            Assert.AreEqual(new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc), l.CreatedAtUtc);
            Assert.AreEqual(2, l.Words.Count);
            WordEntry w = l.Words[0];
            Assert.AreEqual("二重丸", w.Word);
            Assert.AreEqual("にじゅうまる", w.Reading);
            Assert.AreEqual("Doble círculo", w.Meaning);
            Assert.AreEqual("作文のテストで二重丸をもらいました。", w.Sentence);
            Assert.AreEqual("さくぶんの…", w.SentenceReading);
            Assert.AreEqual("Recibí\nuna marca \U0001F600", w.SentenceMeaning);
            Assert.AreEqual("～列目", l.Words[1].Word);
        }

        [Test]
        public void List_ReadsHeaders_CountsUnreadable()
        {
            _repo.Save(Sample());
            File.WriteAllText(Path.Combine(_root, JsonWordListRepository.FolderName, "bad.json"), "{nope", new UTF8Encoding(false));
            WordListListing listing = _repo.List();
            Assert.AreEqual(1, listing.Items.Count);
            Assert.AreEqual("l1", listing.Items[0].Id);
            Assert.AreEqual(2, listing.Items[0].WordCount);
            Assert.AreEqual(1, listing.UnreadableCount);
        }

        [Test]
        public void SaveLoad_KeepsProgressAndDifficult()
        {
            var list = new WordList("l2", "x", DateTime.MinValue,
                new List<WordEntry> { new WordEntry("丸い", "", "", "", "", "", 3, true, 4) });
            Assert.IsTrue(_repo.Save(list).IsSuccess);
            WordEntry w = _repo.Load("l2").Value.Words[0];
            Assert.AreEqual(3, w.Progress);
            Assert.AreEqual(4, w.ReverseProgress);
            Assert.IsTrue(w.Difficult);
        }

        [Test]
        public void Load_S14File_WithoutStudyFields_DefaultsToZero()
        {
            Directory.CreateDirectory(Path.Combine(_root, JsonWordListRepository.FolderName));
            File.WriteAllText(Path.Combine(_root, JsonWordListRepository.FolderName, "l3.json"),
                "{\"version\":1,\"id\":\"l3\",\"name\":\"x\",\"wordCount\":1,\"words\":[{\"word\":\"丸い\",\"reading\":\"まるい\"}]}",
                new UTF8Encoding(false));
            WordEntry w = _repo.Load("l3").Value.Words[0];
            Assert.AreEqual(0, w.Progress);
            Assert.AreEqual(0, w.ReverseProgress);
            Assert.IsFalse(w.Difficult);
            Assert.AreEqual("まるい", w.Reading);
        }

        [Test]
        public void List_NoFolder_IsEmpty() => Assert.AreEqual(0, _repo.List().Items.Count);

        [Test]
        public void Load_Missing_NotFound_InvalidId_Rejected()
        {
            Assert.AreEqual(RepoError.NotFound, _repo.Load("zz").Error);
            Assert.AreEqual(RepoError.InvalidId, _repo.Load("../x").Error);
        }

        [Test]
        public void Load_FutureVersion_Unsupported()
        {
            Directory.CreateDirectory(Path.Combine(_root, JsonWordListRepository.FolderName));
            File.WriteAllText(Path.Combine(_root, JsonWordListRepository.FolderName, "l9.json"),
                "{\"version\":99,\"id\":\"l9\",\"name\":\"x\"}", new UTF8Encoding(false));
            Assert.AreEqual(RepoError.UnsupportedVersion, _repo.Load("l9").Error);
        }
    }
}
