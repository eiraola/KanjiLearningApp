using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Words;
using NUnit.Framework;

namespace Kanji.Tests.Core
{
    public sealed class WordTsvParserTests
    {
        private const string UserSample =
            "Word\tSentence\tReading\tMeaning\tSentenceMeaning\n" +
            "二重丸\t作文のテストで二重丸をもらいました。\tにじゅうまる\tDoble círculo, marca de excelente\tRecibí una marca de excelente en la prueba de redacción.\n" +
            "丸い\t丸いテーブルを買いました。\tまるい\tRedondo\tCompré una mesa redonda.\n" +
            "弾丸\tピストルや弾丸を買うことはできません。\tだんがん\tBala, proyectil\tNo se pueden comprar pistolas ni balas.\n" +
            "面積\tこの土地の面積は100平方メートルです。\tめんせき\tSuperficie, área\tLa superficie de este terreno es de 100 metros cuadrados.\n" +
            "表面\tパイの表面に卵を塗ります。\tひょうめん\tSuperficie\tSe unta huevo sobre la superficie de la tarta.\n" +
            "正面玄関\t受付は正面玄関にあります。\tしょうめんげんかん\tEntrada principal\tLa recepción está en la entrada principal.\n" +
            "～列目\t私は前から3列目に座っています。\t～れつめ\tFila número...\tEstoy sentado en la tercera fila desde delante.\n" +
            "行列\tあの店はおいしいですから行列ができます。\tぎょうれつ\tCola, fila de personas\tComo ese restaurante es bueno, se forman colas.\n";

        [Test]
        public void UserSample_EightWords_NoSentenceReading()
        {
            WordParseResult r = WordTsvParser.Parse(UserSample);
            Assert.AreEqual(WordParseError.None, r.Error);
            Assert.IsTrue(r.HasHeader);
            Assert.AreEqual(8, r.Entries.Count);
            Assert.AreEqual(0, r.InvalidLines);
            WordEntry first = r.Entries[0];
            Assert.AreEqual("二重丸", first.Word);
            Assert.AreEqual("作文のテストで二重丸をもらいました。", first.Sentence);
            Assert.AreEqual("にじゅうまる", first.Reading);
            Assert.AreEqual("Doble círculo, marca de excelente", first.Meaning);
            Assert.AreEqual("Recibí una marca de excelente en la prueba de redacción.", first.SentenceMeaning);
            Assert.AreEqual("", first.SentenceReading);
            Assert.AreEqual("～列目", r.Entries[6].Word);
        }

        [Test]
        public void Header_AnyOrderAndCase_WithSentenceReading_UnknownIgnored()
        {
            string text = "reading\tExtra\tSENTENCE READING\tword\tsentence_meaning\r\n" +
                          "まるい\tx\tまるいテーブル\t丸い\tRedonda\r\n";
            WordParseResult r = WordTsvParser.Parse(text);
            Assert.AreEqual(1, r.Entries.Count);
            WordEntry w = r.Entries[0];
            Assert.AreEqual("丸い", w.Word);
            Assert.AreEqual("まるい", w.Reading);
            Assert.AreEqual("まるいテーブル", w.SentenceReading);
            Assert.AreEqual("Redonda", w.SentenceMeaning);
            Assert.AreEqual("", w.Meaning);
            Assert.AreEqual("", w.Sentence);
        }

        [Test]
        public void NoHeader_UsesDefaultOrder()
        {
            WordParseResult r = WordTsvParser.Parse("丸い\t丸いテーブル。\tまるい\tRedondo\tMesa redonda.\tまるいてーぶる。");
            Assert.IsFalse(r.HasHeader);
            Assert.AreEqual(1, r.Entries.Count);
            Assert.AreEqual("丸いテーブル。", r.Entries[0].Sentence);
            Assert.AreEqual("まるい", r.Entries[0].Reading);
            Assert.AreEqual("Mesa redonda.", r.Entries[0].SentenceMeaning);
            Assert.AreEqual("まるいてーぶる。", r.Entries[0].SentenceReading);
        }

        [Test]
        public void BlankLinesSkipped_LinesWithoutWordOrTabsAreInvalid()
        {
            string text = "\n  \nWord\tReading\n\n丸い\tまるい\n\tsolo lectura\nsin tabulador\n  \n行列\tぎょうれつ\n";
            WordParseResult r = WordTsvParser.Parse(text);
            Assert.AreEqual(2, r.Entries.Count);
            Assert.AreEqual(2, r.InvalidLines);
        }

        [Test]
        public void ShortRows_MissingCellsAreEmpty()
        {
            WordParseResult r = WordTsvParser.Parse("Word\tSentence\tReading\tMeaning\n丸い\t丸いテーブル。");
            Assert.AreEqual(1, r.Entries.Count);
            Assert.AreEqual("", r.Entries[0].Reading);
        }

        [Test]
        public void EmptyText_IsError() => Assert.AreEqual(WordParseError.Empty, WordTsvParser.Parse("  \n ").Error);

        [Test]
        public void HeaderWithoutTabs_IsNoTabsError()
        {
            WordParseResult r = WordTsvParser.Parse("Word Sentence Reading\n丸い 丸いテーブル まるい");
            Assert.AreEqual(WordParseError.NoTabs, r.Error);
        }

        [Test]
        public void OneWordPerLine_WithoutTabs_IsAccepted()
        {
            WordParseResult r = WordTsvParser.Parse("丸い\n行列");
            Assert.AreEqual(WordParseError.None, r.Error);
            Assert.AreEqual(2, r.Entries.Count);
        }

        [Test]
        public void BomAndSpaces_AreTrimmed()
        {
            WordParseResult r = WordTsvParser.Parse("﻿Word\tReading\n 丸い \t まるい ");
            Assert.IsTrue(r.HasHeader);
            Assert.AreEqual("丸い", r.Entries[0].Word);
            Assert.AreEqual("まるい", r.Entries[0].Reading);
        }
    }

    public sealed class WordListLibraryTests
    {
        private sealed class MemoryRepo : IWordListRepository
        {
            public readonly Dictionary<string, WordList> Lists = new Dictionary<string, WordList>();
            public bool FailOnSave;
            public int Saves;

            public WordListListing List()
            {
                var items = new List<WordListInfo>();
                foreach (WordList l in Lists.Values) items.Add(new WordListInfo(l.Id, l.Name, l.Words.Count, l.CreatedAtUtc));
                return new WordListListing(items, 0);
            }

            public RepoResult<WordList> Load(string id) =>
                Lists.TryGetValue(id, out WordList l) ? RepoResult<WordList>.Ok(l) : RepoResult<WordList>.Fail(RepoError.NotFound, "nf");

            public RepoResult Save(WordList list)
            {
                if (FailOnSave) return RepoResult.Fail(RepoError.IoError, "disk");
                Saves++;
                Lists[list.Id] = list;
                return RepoResult.Ok();
            }
        }

        private MemoryRepo _repo;
        private WordListLibrary _lib;
        private int _ids;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _repo = new MemoryRepo();
            _ids = 0;
            _now = new DateTime(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);
            _lib = new WordListLibrary(_repo, () => "l" + (++_ids), () => _now);
        }

        private string NewList(string name = "N3")
        {
            RepoResult<WordListInfo> r = _lib.Create(name);
            Assert.IsTrue(r.IsSuccess, r.Message);
            return r.Value.Id;
        }

        private WordImportOutcome Paste(string id, string text, bool update)
        {
            WordImportPlan plan = _lib.PrepareImport(id, text).Value;
            RepoResult<WordImportOutcome> r = _lib.ApplyImport(id, plan, update);
            Assert.IsTrue(r.IsSuccess, r.Message);
            return r.Value;
        }

        [Test]
        public void Create_TrimsName_BumpsRevision_EmptyList()
        {
            int rev = _lib.Revision;
            string id = NewList("  Lección 1 ");
            Assert.AreEqual("Lección 1", _repo.Lists[id].Name);
            Assert.AreEqual(0, _repo.Lists[id].Words.Count);
            Assert.AreEqual(rev + 1, _lib.Revision);
        }

        [Test]
        public void Create_DuplicateIgnoringCase_Fails()
        {
            NewList("Lección 1");
            RepoResult<WordListInfo> r = _lib.Create("lección 1");
            Assert.AreEqual(RepoError.NameDuplicate, r.Error);
            Assert.AreEqual(NameCheck.Duplicate, _lib.Check("LECCIÓN 1").Check);
            Assert.AreEqual(NameCheck.Empty, _lib.Check("   ").Check);
            Assert.AreEqual(RepoError.NameInvalid, _lib.Create(" ").Error);
        }

        [Test]
        public void List_NewestFirst()
        {
            NewList("a");
            _now = _now.AddMinutes(1);
            NewList("b");
            IReadOnlyList<WordListInfo> items = _lib.List().Items;
            Assert.AreEqual("b", items[0].Name);
            Assert.AreEqual("a", items[1].Name);
        }

        [Test]
        public void Paste_NewWords_AppendedInOrder()
        {
            string id = NewList();
            WordImportOutcome o = Paste(id, "Word\tReading\n丸い\tまるい\n行列\tぎょうれつ", false);
            Assert.AreEqual(2, o.Added);
            Assert.AreEqual(0, o.Ignored);
            Assert.AreEqual("丸い", _repo.Lists[id].Words[0].Word);
            Assert.AreEqual("行列", _repo.Lists[id].Words[1].Word);
        }

        [Test]
        public void Plan_ListsDuplicates_FromListAndInsidePaste_SavesNothing()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tまるい", false);
            int saves = _repo.Saves;
            WordImportPlan plan = _lib.PrepareImport(id, "Word\tReading\n丸い\tマルイ\n行列\tぎょうれつ\n行列\tギョウレツ").Value;
            Assert.AreEqual(1, plan.NewCount);
            Assert.AreEqual(2, plan.Duplicates.Count);
            Assert.AreEqual("丸い", plan.Duplicates[0].Word);
            Assert.AreEqual("行列", plan.Duplicates[1].Word);
            Assert.AreEqual("ギョウレツ", plan.Duplicates[1].Reading, "Update writes the last pasted version");
            Assert.AreEqual(saves, _repo.Saves);
        }

        [Test]
        public void Apply_Ignore_KeepsExistingData()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tまるい", false);
            WordImportOutcome o = Paste(id, "Word\tReading\n丸い\tマルイ\n行列\tぎょうれつ", false);
            Assert.AreEqual(1, o.Added);
            Assert.AreEqual(1, o.Ignored);
            Assert.AreEqual(0, o.Updated);
            Assert.AreEqual("まるい", _repo.Lists[id].Words[0].Reading);
            Assert.AreEqual(2, _repo.Lists[id].Words.Count);
        }

        [Test]
        public void Apply_Update_OverwritesInPlace_LastPasteWins()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tまるい\n面積\tめんせき", false);
            WordImportOutcome o = Paste(id, "Word\tReading\n丸い\tA\n丸い\tB", true);
            Assert.AreEqual(0, o.Added);
            Assert.AreEqual(1, o.Updated);
            Assert.AreEqual("丸い", _repo.Lists[id].Words[0].Word);
            Assert.AreEqual("B", _repo.Lists[id].Words[0].Reading);
            Assert.AreEqual(2, _repo.Lists[id].Words.Count);
        }

        [Test]
        public void Apply_RepeatedInsidePasteOnly_IgnoreKeepsFirst()
        {
            string id = NewList();
            WordImportOutcome o = Paste(id, "Word\tReading\n行列\tA\n行列\tB", false);
            Assert.AreEqual(1, o.Added);
            Assert.AreEqual(1, o.Ignored);
            Assert.AreEqual("A", _repo.Lists[id].Words[0].Reading);
        }

        [Test]
        public void Apply_NothingToSave_DoesNotWriteOrBumpRevision()
        {
            string id = NewList();
            Paste(id, "Word\n丸い", false);
            int saves = _repo.Saves, rev = _lib.Revision;
            WordImportOutcome o = Paste(id, "Word\n丸い", false);
            Assert.AreEqual(1, o.Ignored);
            Assert.AreEqual(saves, _repo.Saves);
            Assert.AreEqual(rev, _lib.Revision);
        }

        [Test]
        public void Apply_SaveFails_ReturnsError_ListUnchanged()
        {
            string id = NewList();
            WordImportPlan plan = _lib.PrepareImport(id, "Word\n丸い").Value;
            _repo.FailOnSave = true;
            Assert.AreEqual(RepoError.IoError, _lib.ApplyImport(id, plan, false).Error);
            Assert.AreEqual(0, _repo.Lists[id].Words.Count);
        }

        [Test]
        public void SetDifficult_TogglesAndPersists_BumpsRevision()
        {
            string id = NewList();
            Paste(id, "Word\n丸い\n行列", false);
            int rev = _lib.Revision;
            RepoResult<WordEntry> r = _lib.SetDifficult(id, "行列", true);
            Assert.IsTrue(r.Value.Difficult);
            Assert.IsTrue(_repo.Lists[id].Words[1].Difficult);
            Assert.IsFalse(_repo.Lists[id].Words[0].Difficult);
            Assert.AreEqual(rev + 1, _lib.Revision);
            Assert.IsFalse(_lib.SetDifficult(id, "行列", false).Value.Difficult);
            Assert.AreEqual(RepoError.NotFound, _lib.SetDifficult(id, "nope", true).Error);
        }

        [Test]
        public void RecordStudy_GreenAddsOne_RedKeeps_MaxFive()
        {
            string id = NewList();
            Paste(id, "Word\n丸い", false);
            Assert.AreEqual(1, _lib.RecordStudy(id, "丸い", true).Value.Progress);
            Assert.AreEqual(1, _lib.RecordStudy(id, "丸い", false).Value.Progress);
            for (int i = 0; i < 10; i++) _lib.RecordStudy(id, "丸い", true);
            Assert.AreEqual(WordEntry.MaxProgress, _repo.Lists[id].Words[0].Progress);
        }

        [Test]
        public void Update_KeepsProgressAndDifficult()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tA", false);
            _lib.SetDifficult(id, "丸い", true);
            _lib.RecordStudy(id, "丸い", true);
            Paste(id, "Word\tReading\n丸い\tB", true);
            WordEntry w = _repo.Lists[id].Words[0];
            Assert.AreEqual("B", w.Reading);
            Assert.AreEqual(1, w.Progress);
            Assert.IsTrue(w.Difficult);
        }

        [Test]
        public void StudySession_AllWordsOnce_RecordsProgress_StatsCountFailed()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tまるい\n行列\tぎょうれつ\n面積\tめんせき", false);
            var session = new WordStudySession(_lib, _lib.Load(id).Value, new Random(1));
            var seen = new HashSet<string>();
            bool first = true;
            while (!session.IsFinished)
            {
                seen.Add(session.Current.Word);
                Assert.IsTrue(session.Record(!first));
                first = false;
            }
            Assert.AreEqual(3, seen.Count);
            var stats = session.BuildStats();
            Assert.AreEqual(3, stats.Total);
            Assert.AreEqual(1, stats.Failed);
            Assert.AreEqual(session.Order[0].Word, stats.FailedEntries[0].Kanji);
            int total = 0;
            foreach (WordEntry w in _repo.Lists[id].Words) total += w.Progress;
            Assert.AreEqual(2, total);
        }

        [Test]
        public void ReverseStudy_UpdatesOnlyReverseProgress()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tまるい\n行列\tぎょうれつ", false);
            var session = new WordStudySession(_lib, _lib.Load(id).Value, new Random(2), reverse: true);
            Assert.IsTrue(session.Reverse);
            while (!session.IsFinished) Assert.IsTrue(session.Record(true));
            foreach (WordEntry w in _repo.Lists[id].Words)
            {
                Assert.AreEqual(1, w.ReverseProgress);
                Assert.AreEqual(0, w.Progress);
                Assert.AreEqual(1, w.ProgressOf(true));
            }
            _lib.RecordStudy(id, "丸い", true);
            Assert.AreEqual(1, _repo.Lists[id].Words[0].Progress);
            Assert.AreEqual(1, _repo.Lists[id].Words[0].ReverseProgress);
        }

        [Test]
        public void Update_And_SetDifficult_KeepReverseProgress()
        {
            string id = NewList();
            Paste(id, "Word\tReading\n丸い\tA", false);
            _lib.RecordStudy(id, "丸い", true, reverse: true);
            _lib.SetDifficult(id, "丸い", true);
            Paste(id, "Word\tReading\n丸い\tB", true);
            WordEntry w = _repo.Lists[id].Words[0];
            Assert.AreEqual(1, w.ReverseProgress);
            Assert.IsTrue(w.Difficult);
            Assert.AreEqual("B", w.Reading);
        }

        [Test]
        public void Prepare_UnknownList_NotFound() =>
            Assert.AreEqual(RepoError.NotFound, _lib.PrepareImport("nope", "Word\n丸い").Error);
    }
}
