using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Tests.Core.Fakes;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class DifficultMigrationTests
    {
        private sealed class MemRepo : IDifficultKanjiRepository
        {
            public readonly Dictionary<string, List<KanjiEntry>> Data = new Dictionary<string, List<KanjiEntry>>();
            public int FailSaveAfter = int.MaxValue;
            private int _saves;

            public IReadOnlyList<KanjiEntry> Load(string id) =>
                Data.TryGetValue(id, out var l) ? l : new List<KanjiEntry>();

            public RepoResult Save(string id, IReadOnlyList<KanjiEntry> items)
            {
                if (_saves++ >= FailSaveAfter) return RepoResult.Fail(RepoError.IoError, "disk");
                Data[id] = new List<KanjiEntry>(items);
                return RepoResult.Ok();
            }

            public RepoResult Delete(string id) { Data.Remove(id); return RepoResult.Ok(); }
            public IReadOnlyList<string> ListStoryIds() => new List<string>(Data.Keys);
        }

        private sealed class MemSource : IDifficultMigrationSource
        {
            public List<KanjiEntry> Legacy;      // null = missing
            public bool Corrupt;
            public bool Migrated;
            public int MarkedDiscarded = -1;

            public bool IsMigrated() => Migrated;
            public bool LegacyExists() => Legacy != null || Corrupt;

            public RepoResult<IReadOnlyList<KanjiEntry>> LoadLegacy() =>
                Corrupt
                    ? RepoResult<IReadOnlyList<KanjiEntry>>.Fail(RepoError.Corrupt, "bad")
                    : RepoResult<IReadOnlyList<KanjiEntry>>.Ok(Legacy ?? new List<KanjiEntry>());

            public RepoResult MarkMigrated(int discarded) { Migrated = true; MarkedDiscarded = discarded; return RepoResult.Ok(); }
            public RepoResult DeleteLegacy() { Legacy = null; Corrupt = false; return RepoResult.Ok(); }
        }

        private static KanjiEntry E(string k, string r = "r") => new KanjiEntry(k, r, "m", "s", "sm");

        private static StoryLibrary NewLibrary(out string[] ids, params (string name, string[] kanjis)[] stories)
        {
            var repo = new InMemoryStoryRepository();
            int n = 0;
            var lib = new StoryLibrary(repo, () => "s" + (n++), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            ids = new string[stories.Length];
            for (int i = 0; i < stories.Length; i++)
            {
                var entries = new List<KanjiEntry>();
                foreach (string k in stories[i].kanjis) entries.Add(E(k));
                ImportResult r = lib.Import(new Story("", "", null, entries), stories[i].name);
                ids[i] = r.Story.Id;
            }
            return lib;
        }

        [Test]
        public void Run_AssignsToFirstStoryContainingKanji_AndDiscardsOthers()
        {
            // Names sort A < B with equal dates, so A is first.
            StoryLibrary lib = NewLibrary(out string[] ids, ("A", new[] { "木" }), ("B", new[] { "木", "水" }));
            var repo = new MemRepo();
            var src = new MemSource { Legacy = new List<KanjiEntry> { E("木"), E("水"), E("火") } };

            MigrationResult r = new DifficultMigration(lib, repo, src).Run();

            Assert.AreEqual(MigrationStatus.Done, r.Status);
            Assert.AreEqual(2, r.Assigned);
            Assert.AreEqual(1, r.Discarded);
            Assert.AreEqual(1, repo.Data[ids[0]].Count);
            Assert.AreEqual("木", repo.Data[ids[0]][0].Kanji);
            Assert.AreEqual(1, repo.Data[ids[1]].Count);
            Assert.AreEqual("水", repo.Data[ids[1]][0].Kanji);
            Assert.AreEqual(1, src.MarkedDiscarded);
        }

        [Test]
        public void Run_DeletesLegacy_AndDoesNotRepeat()
        {
            StoryLibrary lib = NewLibrary(out string[] ids, ("A", new[] { "木" }));
            var repo = new MemRepo();
            var src = new MemSource { Legacy = new List<KanjiEntry> { E("木") } };
            var migration = new DifficultMigration(lib, repo, src);

            Assert.AreEqual(MigrationStatus.Done, migration.Run().Status);
            Assert.IsFalse(src.LegacyExists());

            src.Legacy = new List<KanjiEntry> { E("木", "other") }; // reappears: must be cleaned, not migrated
            MigrationResult second = migration.Run();
            Assert.AreEqual(MigrationStatus.Skipped, second.Status);
            Assert.IsFalse(src.LegacyExists());
            Assert.AreEqual(1, repo.Data[ids[0]].Count);
        }

        [Test]
        public void Run_NoLegacy_MarksMigratedAndSkips()
        {
            StoryLibrary lib = NewLibrary(out _, ("A", new[] { "木" }));
            var src = new MemSource();
            MigrationResult r = new DifficultMigration(lib, new MemRepo(), src).Run();
            Assert.AreEqual(MigrationStatus.Skipped, r.Status);
            Assert.IsTrue(src.Migrated);
        }

        [Test]
        public void Run_CorruptLegacy_FailsWithoutTouchingAnything()
        {
            StoryLibrary lib = NewLibrary(out _, ("A", new[] { "木" }));
            var repo = new MemRepo();
            var src = new MemSource { Corrupt = true };
            MigrationResult r = new DifficultMigration(lib, repo, src).Run();
            Assert.AreEqual(MigrationStatus.Failed, r.Status);
            Assert.IsFalse(src.Migrated);
            Assert.IsTrue(src.LegacyExists());
            Assert.AreEqual(0, repo.Data.Count);
        }

        [Test]
        public void Run_PartialFailure_KeepsLegacy_AndRetryDoesNotDuplicate()
        {
            StoryLibrary lib = NewLibrary(out string[] ids, ("A", new[] { "木" }), ("B", new[] { "水" }));
            var repo = new MemRepo { FailSaveAfter = 1 }; // first save ok, second fails
            var src = new MemSource { Legacy = new List<KanjiEntry> { E("木"), E("水") } };
            var migration = new DifficultMigration(lib, repo, src);

            Assert.AreEqual(MigrationStatus.Failed, migration.Run().Status);
            Assert.IsFalse(src.Migrated);
            Assert.IsTrue(src.LegacyExists());

            repo.FailSaveAfter = int.MaxValue;
            MigrationResult retry = migration.Run();
            Assert.AreEqual(MigrationStatus.Done, retry.Status);
            Assert.AreEqual(1, repo.Data[ids[0]].Count);
            Assert.AreEqual(1, repo.Data[ids[1]].Count);
            Assert.IsFalse(src.LegacyExists());
        }

        private sealed class FlakyStoryRepo : IStoryRepository
        {
            private readonly InMemoryStoryRepository _inner = new InMemoryStoryRepository();
            public RepoError LoadError = RepoError.None;
            public StoryListing List() => _inner.List();
            public RepoResult<Story> Load(string id) =>
                LoadError == RepoError.None ? _inner.Load(id) : RepoResult<Story>.Fail(LoadError, "x");
            public RepoResult Save(Story story) => _inner.Save(story);
            public RepoResult Delete(string id) => _inner.Delete(id);
        }

        [Test]
        public void Run_UnreadableStory_FailsWithoutMarkingOrDeleting_ThenRetrySucceeds()
        {
            var storyRepo = new FlakyStoryRepo();
            int n = 0;
            var lib = new StoryLibrary(storyRepo, () => "s" + (n++), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            ImportResult imp = lib.Import(new Story("", "", null, new List<KanjiEntry> { E("木") }), "A");
            var repo = new MemRepo();
            var src = new MemSource { Legacy = new List<KanjiEntry> { E("木") } };
            var migration = new DifficultMigration(lib, repo, src);

            storyRepo.LoadError = RepoError.IoError;
            Assert.AreEqual(MigrationStatus.Failed, migration.Run().Status);
            Assert.IsFalse(src.Migrated);
            Assert.IsTrue(src.LegacyExists());
            Assert.AreEqual(0, repo.Data.Count);

            storyRepo.LoadError = RepoError.None;
            Assert.AreEqual(MigrationStatus.Done, migration.Run().Status);
            Assert.AreEqual(1, repo.Data[imp.Story.Id].Count);
        }

        [Test]
        public void Run_StoryVanishedBetweenListAndLoad_IsNotAFailure()
        {
            var storyRepo = new FlakyStoryRepo();
            int n = 0;
            var lib = new StoryLibrary(storyRepo, () => "s" + (n++), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            lib.Import(new Story("", "", null, new List<KanjiEntry> { E("木") }), "A");
            var src = new MemSource { Legacy = new List<KanjiEntry> { E("木") } };

            storyRepo.LoadError = RepoError.NotFound;
            MigrationResult r = new DifficultMigration(lib, new MemRepo(), src).Run();
            Assert.AreEqual(MigrationStatus.Done, r.Status);
            Assert.AreEqual(1, r.Discarded);
        }

        [Test]
        public void Run_500Difficult_50Stories_IsFast()
        {
            var specs = new (string, string[])[50];
            for (int s = 0; s < 50; s++)
            {
                var ks = new string[20];
                for (int k = 0; k < 20; k++) ks[k] = "k" + (s * 10 + k); // overlapping ranges
                specs[s] = ("S" + s.ToString("D2"), ks);
            }
            StoryLibrary lib = NewLibrary(out _, specs);
            var legacy = new List<KanjiEntry>();
            for (int i = 0; i < 500; i++) legacy.Add(E("k" + i));
            var src = new MemSource { Legacy = legacy };

            var sw = Stopwatch.StartNew();
            MigrationResult r = new DifficultMigration(lib, new MemRepo(), src).Run();
            sw.Stop();

            Assert.AreEqual(MigrationStatus.Done, r.Status);
            Assert.AreEqual(500, r.Assigned + r.Discarded);
            Assert.Less(sw.ElapsedMilliseconds, 2000);
        }
    }
}
