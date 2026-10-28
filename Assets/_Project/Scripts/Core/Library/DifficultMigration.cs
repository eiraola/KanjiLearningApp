using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    /// <summary>Access to the legacy global difficult list and the "migrated" marker.</summary>
    public interface IDifficultMigrationSource
    {
        bool IsMigrated();

        /// <summary>Fail if corrupt; Ok(empty) if missing.</summary>
        RepoResult<IReadOnlyList<KanjiEntry>> LoadLegacy();

        bool LegacyExists();
        RepoResult MarkMigrated(int discarded);
        RepoResult DeleteLegacy();
    }

    public enum MigrationStatus { Skipped, Done, Failed }

    public readonly struct MigrationResult
    {
        public MigrationResult(MigrationStatus status, int assigned, int discarded)
        {
            Status = status;
            Assigned = assigned;
            Discarded = discarded;
        }

        public MigrationStatus Status { get; }
        public int Assigned { get; }
        public int Discarded { get; }
    }

    /// <summary>One-time move of the global difficult list into per-story lists. Idempotent and safe to retry.</summary>
    public sealed class DifficultMigration
    {
        private readonly StoryLibrary _library;
        private readonly IDifficultKanjiRepository _repo;
        private readonly IDifficultMigrationSource _source;

        public DifficultMigration(StoryLibrary library, IDifficultKanjiRepository repo, IDifficultMigrationSource source)
        {
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public MigrationResult Run()
        {
            if (_source.IsMigrated())
            {
                if (_source.LegacyExists()) _source.DeleteLegacy();
                return new MigrationResult(MigrationStatus.Skipped, 0, 0);
            }

            if (!_source.LegacyExists())
            {
                _source.MarkMigrated(0);
                return new MigrationResult(MigrationStatus.Skipped, 0, 0);
            }

            RepoResult<IReadOnlyList<KanjiEntry>> legacy = _source.LoadLegacy();
            if (!legacy.IsSuccess || legacy.Value == null) return Failed();

            var byStory = new Dictionary<string, List<KanjiEntry>>();
            var order = new List<string>();
            int discarded = Assign(legacy.Value, byStory, order);
            if (discarded < 0) return Failed();
            int assigned = legacy.Value.Count - discarded;

            for (int i = 0; i < order.Count; i++)
                if (!MergeAndSave(order[i], byStory[order[i]])) return Failed();

            if (!_source.MarkMigrated(discarded).IsSuccess) return Failed();
            _source.DeleteLegacy();
            return new MigrationResult(MigrationStatus.Done, assigned, discarded);
        }

        private static MigrationResult Failed() => new MigrationResult(MigrationStatus.Failed, 0, 0);

        // Each story is indexed once; a kanji goes to the first story (stable library order) that contains it.
        private int Assign(IReadOnlyList<KanjiEntry> legacy, Dictionary<string, List<KanjiEntry>> byStory, List<string> order)
        {
            var sets = new List<KeyValuePair<string, HashSet<KanjiKey>>>();
            foreach (StoryInfo info in _library.List().Items)
            {
                RepoResult<Story> loaded = _library.Load(info.Id);
                if (!loaded.IsSuccess || loaded.Value == null)
                {
                    // Vanished between List() and Load() is fine; an unreadable history must abort (irreversible loss otherwise).
                    if (loaded.Error == RepoError.NotFound) continue;
                    return -1;
                }
                var set = new HashSet<KanjiKey>();
                IReadOnlyList<KanjiEntry> entries = loaded.Value.Entries;
                for (int i = 0; i < entries.Count; i++) set.Add(entries[i].Key);
                sets.Add(new KeyValuePair<string, HashSet<KanjiKey>>(info.Id, set));
            }

            int discarded = 0;
            for (int i = 0; i < legacy.Count; i++)
            {
                KanjiEntry entry = legacy[i];
                string target = null;
                if (entry != null)
                {
                    for (int s = 0; s < sets.Count; s++)
                    {
                        if (!sets[s].Value.Contains(entry.Key)) continue;
                        target = sets[s].Key;
                        break;
                    }
                }
                if (target == null) { discarded++; continue; }
                if (!byStory.TryGetValue(target, out List<KanjiEntry> list))
                {
                    list = new List<KanjiEntry>();
                    byStory[target] = list;
                    order.Add(target);
                }
                list.Add(entry);
            }
            return discarded;
        }

        private bool MergeAndSave(string storyId, List<KanjiEntry> incoming)
        {
            var merged = new List<KanjiEntry>();
            var keys = new HashSet<string>();
            IReadOnlyList<KanjiEntry> existing = _repo.Load(storyId);
            if (existing != null)
                for (int i = 0; i < existing.Count; i++)
                    if (existing[i] != null && keys.Add(DedupKey(existing[i]))) merged.Add(existing[i]);
            for (int i = 0; i < incoming.Count; i++)
                if (keys.Add(DedupKey(incoming[i]))) merged.Add(incoming[i]);
            return _repo.Save(storyId, merged).IsSuccess;
        }

        private static string DedupKey(KanjiEntry e) => e.Key.Value + "\u0001" + e.Reading.Trim();
    }
}
