using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kanji.Core.Library;
using Kanji.Core.Model;
using UnityEngine;

namespace Kanji.Infrastructure.Storage
{
    /// <summary>One versioned file per story: root/difficult/&lt;storyId&gt;.json = {version:1, items:[{..., successes}]}.</summary>
    public sealed class JsonDifficultKanjiRepository : IDifficultKanjiRepository, IDifficultMigrationSource
    {
        private const int CurrentVersion = 1;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly IFileSystem _fs;
        private readonly AtomicFileWriter _writer;
        private readonly string _dir;
        private readonly string _legacyPath;
        private readonly string _markerPath;

        public JsonDifficultKanjiRepository(IFileSystem fs, string rootDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _writer = new AtomicFileWriter(fs);
            _dir = Path.Combine(rootDirectory, "difficult");
            _legacyPath = Path.Combine(rootDirectory, "difficult_kanji.json");
            _markerPath = Path.Combine(rootDirectory, "difficult_migrated.json");
        }

        // ---- IDifficultMigrationSource: legacy global list (root/difficult_kanji.json) + marker ----

        public bool IsMigrated()
        {
            try { return _fs.FileExists(_markerPath); }
            catch (Exception) { return false; }
        }

        public bool LegacyExists()
        {
            try { return _fs.FileExists(_legacyPath); }
            catch (Exception) { return false; }
        }

        public RepoResult<IReadOnlyList<KanjiEntry>> LoadLegacy()
        {
            var result = new List<KanjiEntry>();
            try
            {
                if (!_fs.FileExists(_legacyPath)) return RepoResult<IReadOnlyList<KanjiEntry>>.Ok(result);
                ListDto dto = JsonUtility.FromJson<ListDto>(Utf8NoBom.GetString(_fs.ReadAllBytes(_legacyPath)));
                if (dto == null || dto.version < 1 || dto.version > CurrentVersion || dto.items == null)
                    return RepoResult<IReadOnlyList<KanjiEntry>>.Fail(RepoError.Corrupt, "Legacy list unreadable.");
                foreach (ItemDto i in dto.items)
                {
                    if (i == null || string.IsNullOrWhiteSpace(i.kanji))
                        return RepoResult<IReadOnlyList<KanjiEntry>>.Fail(RepoError.Corrupt, "Legacy list has an invalid item.");
                    result.Add(new KanjiEntry(i.kanji, i.reading, i.meaning, i.sentence, i.sentenceMeaning));
                }
                return RepoResult<IReadOnlyList<KanjiEntry>>.Ok(result);
            }
            catch (Exception e)
            {
                return RepoResult<IReadOnlyList<KanjiEntry>>.Fail(RepoError.Corrupt, e.Message);
            }
        }

        public RepoResult MarkMigrated(int discarded) =>
            _writer.Write(_markerPath, Utf8NoBom.GetBytes("{\"version\":1,\"discarded\":" + discarded + "}"));

        public RepoResult DeleteLegacy()
        {
            try
            {
                if (_fs.FileExists(_legacyPath)) _fs.Delete(_legacyPath);
                return RepoResult.Ok();
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }
        }

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
                if (!hex && c != '-') return false;
            }
            return true;
        }

        private string PathOf(string storyId) => Path.Combine(_dir, storyId + ".json");

        public IReadOnlyList<string> ListStoryIds()
        {
            var ids = new List<string>();
            try
            {
                foreach (string file in _fs.ListFiles(_dir, ".json"))
                {
                    if (!string.Equals(Path.GetExtension(file), ".json", StringComparison.OrdinalIgnoreCase)) continue;
                    string id = Path.GetFileNameWithoutExtension(file);
                    if (IsValidId(id)) ids.Add(id);
                }
            }
            catch (Exception)
            {
                // unreadable folder: report what we have
            }
            return ids;
        }

        public RepoResult Delete(string storyId)
        {
            if (!IsValidId(storyId)) return RepoResult.Fail(RepoError.InvalidId, "Invalid story id.");
            try
            {
                string path = PathOf(storyId);
                if (_fs.FileExists(path)) _fs.Delete(path);
                return RepoResult.Ok();
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }
        }

        public IReadOnlyList<KanjiEntry> Load(string storyId)
        {
            var result = new List<KanjiEntry>();
            LoadInto(storyId, result, null);
            return result;
        }

        public IReadOnlyList<int> LoadProgress(string storyId)
        {
            var entries = new List<KanjiEntry>();
            var progress = new List<int>();
            LoadInto(storyId, entries, progress);
            return progress.Count == entries.Count ? progress : null;
        }

        // Fills entries (and progress, when given). Leaves both empty when the file is missing or unreadable.
        private void LoadInto(string storyId, List<KanjiEntry> result, List<int> progress)
        {
            if (!IsValidId(storyId)) return;
            string path = PathOf(storyId);
            byte[] raw = null;
            try
            {
                if (!_fs.FileExists(path)) return;
                raw = _fs.ReadAllBytes(path);
                ListDto dto = JsonUtility.FromJson<ListDto>(Utf8NoBom.GetString(raw));
                if (dto == null || dto.version < 1 || dto.version > CurrentVersion || dto.items == null)
                {
                    Unreadable(path, raw, result, progress);
                    return;
                }

                foreach (ItemDto i in dto.items)
                {
                    if (i == null || string.IsNullOrWhiteSpace(i.kanji))
                    {
                        Unreadable(path, raw, result, progress); // corrupt: treat the whole file as unreadable
                        return;
                    }
                    result.Add(new KanjiEntry(i.kanji, i.reading, i.meaning, i.sentence, i.sentenceMeaning));
                    progress?.Add(i.successes);
                }
            }
            catch (Exception)
            {
                Unreadable(path, raw, result, progress);
            }
        }

        // Keeps a best-effort copy so the next Save does not destroy the unreadable original.
        private void Unreadable(string path, byte[] raw, List<KanjiEntry> result, List<int> progress)
        {
            result.Clear();
            progress?.Clear();
            try
            {
                if (raw != null) _fs.WriteAllBytes(path + ".corrupt", raw);
            }
            catch (Exception)
            {
                // best effort only
            }
        }

        public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items) => Save(storyId, items, null);

        public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items, IReadOnlyList<int> progress)
        {
            if (!IsValidId(storyId)) return RepoResult.Fail(RepoError.InvalidId, "Invalid story id.");
            if (items == null) return RepoResult.Fail(RepoError.InvalidId, "Items is null.");
            byte[] data;
            try
            {
                var dto = new ListDto { version = CurrentVersion, items = new List<ItemDto>(items.Count) };
                for (int n = 0; n < items.Count; n++)
                {
                    KanjiEntry e = items[n];
                    dto.items.Add(new ItemDto
                    {
                        kanji = e.Kanji, reading = e.Reading, meaning = e.Meaning,
                        sentence = e.Sentence, sentenceMeaning = e.SentenceMeaning,
                        successes = progress != null && n < progress.Count ? progress[n] : 0,
                    });
                }
                data = Utf8NoBom.GetBytes(JsonUtility.ToJson(dto));
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }

            return _writer.Write(PathOf(storyId), data);
        }

        [Serializable]
        private sealed class ListDto
        {
            public int version;
            public List<ItemDto> items = new List<ItemDto>();
        }

        [Serializable]
        private sealed class ItemDto
        {
            public string kanji = "";
            public string reading = "";
            public string meaning = "";
            public string sentence = "";
            public string sentenceMeaning = "";
            public int successes; // S11: study progress 0..5; absent in older files = 0
        }
    }
}
