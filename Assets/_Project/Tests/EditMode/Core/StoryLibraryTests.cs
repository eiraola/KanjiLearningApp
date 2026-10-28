using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Tests.Core.Fakes;
using NUnit.Framework;

namespace Kanji.Tests.Core
{
    public sealed class StoryLibraryTests
    {
        private InMemoryStoryRepository _repo;
        private StoryLibrary _lib;
        private int _idCounter;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _repo = new InMemoryStoryRepository();
            _idCounter = 0;
            _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _lib = new StoryLibrary(_repo, () => "id" + (++_idCounter), () => _now);
        }

        private static Story Make(string name, string text = "t")
        {
            var entries = new List<KanjiEntry> { new KanjiEntry("k", "r", "m", "", "") };
            var segments = new List<Segment> { Segment.Plain(text) };
            return new Story("", name, segments, entries);
        }

        /// <summary>Imports a story under the given name (the name the user would type).</summary>
        private ImportResult Imp(string name, string text = "t") => _lib.Import(Make("file", text), name);

        [Test]
        public void Delete_Success_RaisesDeletedWithId()
        {
            Imp("a");
            string raised = null;
            _lib.Deleted += id => raised = id;
            Assert.IsTrue(_lib.Delete("id1").IsSuccess);
            Assert.AreEqual("id1", raised);
        }

        [Test]
        public void Delete_Nonexistent_StillRaisesDeleted_CascadeIsIdempotent()
        {
            var raised = 0;
            _lib.Deleted += _ => raised++;
            Assert.IsTrue(_lib.Delete("nope").IsSuccess);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Import_NewName_Saves_WithGeneratedId()
        {
            ImportResult r = Imp("a");
            Assert.AreEqual(ImportStatus.Imported, r.Status);
            Assert.AreEqual("id1", r.Story.Id);
            Assert.AreEqual("a", _repo.Load("id1").Value.Name);
        }

        [Test]
        public void Import_UsesGivenNameNotParsedName()
        {
            Assert.AreEqual("custom", _lib.Import(Make("file"), "  custom ").Story.Name);
        }

        [Test]
        public void Import_DuplicateName_ReturnsDuplicate_SavesNothing()
        {
            Imp("a");
            ImportResult r = Imp("a");
            Assert.AreEqual(ImportStatus.Duplicate, r.Status);
            Assert.AreEqual("a", r.ConflictingName);
            Assert.IsNull(r.Story);
            Assert.AreEqual(1, _repo.SaveCount);
            Assert.AreEqual(1, _repo.List().Items.Count);
        }

        [Test]
        public void Import_Duplicate_IsCaseInsensitive()
        {
            Imp("Foo");
            ImportResult r = Imp("foo");
            Assert.AreEqual(ImportStatus.Duplicate, r.Status);
            Assert.AreEqual("Foo", r.ConflictingName);
        }

        [Test]
        public void Import_Duplicate_IgnoresSurroundingSpaces()
        {
            Imp("foo");
            Assert.AreEqual(ImportStatus.Duplicate, Imp("  foo ").Status);
        }

        [Test]
        public void Import_EmptyName_FailsWithNameInvalid()
        {
            ImportResult r = Imp("   ");
            Assert.AreEqual(ImportStatus.Failed, r.Status);
            Assert.AreEqual(RepoError.NameInvalid, r.Error);
            Assert.AreEqual(0, _repo.SaveCount);
        }

        [Test]
        public void Import_RepoFailure_ReturnsFailed()
        {
            _repo.FailOnSave = true;
            ImportResult r = Imp("a");
            Assert.AreEqual(ImportStatus.Failed, r.Status);
            Assert.AreEqual(RepoError.IoError, r.Error);
            Assert.IsNull(r.Story);
            Assert.IsNotEmpty(r.Message);
        }

        [Test]
        public void Import_StampsUtcNow()
        {
            ImportResult r = Imp("a");
            Assert.AreEqual(_now, r.Story.ImportedAtUtc);
            Assert.AreEqual(_now, _lib.List().Items[0].ImportedAtUtc);
        }

        [Test]
        public void Check_Ok_Empty_Duplicate_AndFallback()
        {
            Imp("a");
            Assert.AreEqual(NameCheck.Ok, _lib.Check("b", null, null).Check);
            Assert.AreEqual(NameCheck.Empty, _lib.Check("  ", null, null).Check);
            Assert.AreEqual(NameCheck.Duplicate, _lib.Check("A", null, null).Check);
            Assert.AreEqual(NameCheck.Duplicate, _lib.Check("", "a", null).Check);
            Assert.AreEqual(NameCheck.Ok, _lib.Check("A", null, "id1").Check);
        }

        [Test]
        public void Rename_ChangesName_KeepsIdDateAndContent()
        {
            Imp("a", "body");
            _now = _now.AddHours(5);
            Assert.IsTrue(_lib.Rename("id1", "  b ").IsSuccess);

            Story s = _repo.Load("id1").Value;
            Assert.AreEqual("b", s.Name);
            Assert.AreEqual("body", s.Segments[0].Text);
            Assert.AreEqual(1, s.Entries.Count);
            Assert.AreEqual(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), s.ImportedAtUtc);
            Assert.AreEqual(1, _repo.List().Items.Count);
        }

        [Test]
        public void Rename_KeepsListOrder()
        {
            Imp("a");
            _now = _now.AddMinutes(1);
            Imp("b");
            _lib.Rename("id1", "zzz");
            CollectionAssert.AreEqual(new[] { "b", "zzz" }, Names(_lib.List().Items));
        }

        [Test]
        public void Rename_OnlyCase_IsAllowed()
        {
            Imp("a");
            Assert.IsTrue(_lib.Rename("id1", "A").IsSuccess);
            Assert.AreEqual("A", _repo.Load("id1").Value.Name);
        }

        [Test]
        public void Rename_Duplicate_FailsAndSavesNothing()
        {
            Imp("a");
            Imp("b");
            int saves = _repo.SaveCount;
            RepoResult r = _lib.Rename("id2", "A");
            Assert.AreEqual(RepoError.NameDuplicate, r.Error);
            Assert.AreEqual(saves, _repo.SaveCount);
            Assert.AreEqual("b", _repo.Load("id2").Value.Name);
        }

        [Test]
        public void Rename_Empty_FailsWithNameInvalid()
        {
            Imp("a");
            Assert.AreEqual(RepoError.NameInvalid, _lib.Rename("id1", "  ").Error);
        }

        [Test]
        public void Rename_UnknownId_ReturnsNotFound()
        {
            Assert.AreEqual(RepoError.NotFound, _lib.Rename("nope", "x").Error);
        }

        [Test]
        public void Rename_RepoFailure_DoesNotBumpRevision()
        {
            Imp("a");
            int rev = _lib.Revision;
            _repo.FailOnSave = true;
            Assert.AreEqual(RepoError.IoError, _lib.Rename("id1", "b").Error);
            Assert.AreEqual(rev, _lib.Revision);
        }

        [Test]
        public void List_StableOrderBetweenCalls()
        {
            Imp("b");
            Imp("a");
            Imp("c");
            CollectionAssert.AreEqual(Names(_lib.List().Items), Names(_lib.List().Items));
        }

        [Test]
        public void List_OrderIsCaseInsensitiveByName()
        {
            Imp("b");
            Imp("A");
            Imp("c");
            CollectionAssert.AreEqual(new[] { "A", "b", "c" }, Names(_lib.List().Items));
        }

        [Test]
        public void List_OrdersByImportedAtDesc_ThenName()
        {
            _now = _now.AddMinutes(1); Imp("b");
            _now = _now.AddMinutes(1); Imp("z");
            _now = _now.AddMinutes(1); Imp("a");
            Imp("c");
            CollectionAssert.AreEqual(new[] { "a", "c", "z", "b" }, Names(_lib.List().Items));
        }

        [Test]
        public void List_PropagatesUnreadableCount()
        {
            _repo.UnreadableCount = 3;
            Assert.AreEqual(3, _lib.List().UnreadableCount);
        }

        [Test]
        public void Delete_Existing_Removes()
        {
            Imp("a");
            Assert.IsTrue(_lib.Delete("id1").IsSuccess);
            Assert.AreEqual(0, _lib.List().Items.Count);
        }

        [Test]
        public void Delete_Nonexistent_DoesNotFail()
        {
            Assert.IsTrue(_lib.Delete("nope").IsSuccess);
        }

        [Test]
        public void Load_Passthrough()
        {
            Imp("a");
            Assert.AreEqual("a", _lib.Load("id1").Value.Name);
            Assert.AreEqual(RepoError.NotFound, _lib.Load("zzz").Error);
        }

        [Test]
        public void Revision_IncrementsOnImportRenameDelete_NotOnFailureOrDuplicate()
        {
            Assert.AreEqual(0, _lib.Revision);
            Imp("a");
            Assert.AreEqual(1, _lib.Revision);
            Imp("a");
            Assert.AreEqual(1, _lib.Revision);
            _repo.FailOnSave = true;
            Imp("b");
            Assert.AreEqual(1, _lib.Revision);
            _repo.FailOnSave = false;
            _lib.Rename("id1", "c");
            Assert.AreEqual(2, _lib.Revision);
            _lib.Delete("id1");
            Assert.AreEqual(3, _lib.Revision);
        }

        private static string[] Names(IReadOnlyList<StoryInfo> infos)
        {
            var names = new string[infos.Count];
            for (int i = 0; i < infos.Count; i++) names[i] = infos[i].Name;
            return names;
        }
    }
}
