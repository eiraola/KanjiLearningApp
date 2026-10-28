using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Kanji.Core.Library;
using Kanji.Core.Model;
using UnityEngine;

namespace Kanji.Infrastructure.Storage
{
    /// <summary>One JSON file per story: root/stories/id.json (see ADR 0004).</summary>
    public sealed class JsonStoryRepository : IStoryRepository
    {
        private const int CurrentVersion = 2;
        private const string Extension = ".json";
        private static readonly Regex ValidId = new Regex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly IFileSystem _fs;
        private readonly AtomicFileWriter _writer;
        private readonly string _storiesDir;

        public JsonStoryRepository(IFileSystem fs, string rootDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _writer = new AtomicFileWriter(fs);
            _storiesDir = Path.Combine(rootDirectory, "stories");
        }

        public StoryListing List()
        {
            var infos = new List<StoryInfo>();
            int unreadable = 0;
            try
            {
                foreach (string file in _fs.ListFiles(_storiesDir, Extension))
                {
                    if (!string.Equals(Path.GetExtension(file), Extension, StringComparison.OrdinalIgnoreCase)) continue;
                    StoryInfo info = ReadInfo(file);
                    if (info != null) infos.Add(info);
                    else unreadable++;
                }
            }
            catch (Exception)
            {
                // Listing never throws; return what could be read.
            }

            return new StoryListing(infos, unreadable);
        }

        // Returns null when the file cannot be listed (unreadable, corrupt or unsupported version).
        private StoryInfo ReadInfo(string file)
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (!IsValidId(id)) return null;

            byte[] bytes;
            try { bytes = _fs.ReadAllBytes(file); }
            catch (Exception) { return null; }

            HeaderDto header;
            try { header = JsonUtility.FromJson<HeaderDto>(Utf8NoBom.GetString(bytes)); }
            catch (Exception) { return null; }

            if (header == null || header.version < 1 || header.version > CurrentVersion || header.id != id) return null;

            if (header.version < CurrentVersion)
            {
                // v1 has no counters in the header: load the full story (legacy test files only).
                RepoResult<Story> full = Decode(id, bytes);
                return full.IsSuccess
                    ? new StoryInfo(id, full.Value.Name, full.Value.Entries.Count, full.Value.ImportedAtUtc)
                    : null;
            }

            if (header.entryCount < 0) return null;
            return new StoryInfo(id, header.name, header.entryCount, FromUnixMs(header.importedAtUnixMs));
        }

        private static DateTime FromUnixMs(long ms)
        {
            if (ms <= 0) return DateTime.MinValue;
            try { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; }
            catch (ArgumentOutOfRangeException) { return DateTime.MinValue; }
        }

        private static long ToUnixMs(DateTime utc)
        {
            if (utc == DateTime.MinValue) return 0;
            return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        }

        public RepoResult<Story> Load(string id)
        {
            if (!IsValidId(id)) return RepoResult<Story>.Fail(RepoError.InvalidId, "Invalid id.");
            string path = PathFor(id);
            byte[] bytes;
            try
            {
                if (!_fs.FileExists(path)) return RepoResult<Story>.Fail(RepoError.NotFound, "Story not found.");
                bytes = _fs.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                return RepoResult<Story>.Fail(RepoError.IoError, e.Message);
            }

            return Decode(id, bytes);
        }

        public RepoResult Save(Story story)
        {
            if (story == null) return RepoResult.Fail(RepoError.InvalidId, "Story is null.");
            if (!IsValidId(story.Id)) return RepoResult.Fail(RepoError.InvalidId, "Invalid id.");

            byte[] data;
            try
            {
                data = Utf8NoBom.GetBytes(JsonUtility.ToJson(ToDto(story)));
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }

            return _writer.Write(PathFor(story.Id), data);
        }

        public RepoResult Delete(string id)
        {
            if (!IsValidId(id)) return RepoResult.Fail(RepoError.InvalidId, "Invalid id.");
            try
            {
                _fs.Delete(PathFor(id));
                return RepoResult.Ok();
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }
        }

        private static bool IsValidId(string id) => !string.IsNullOrEmpty(id) && ValidId.IsMatch(id);

        private string PathFor(string id) => Path.Combine(_storiesDir, id + Extension);

        private static RepoResult<Story> Decode(string id, byte[] bytes)
        {
            StoryDto dto;
            try
            {
                dto = JsonUtility.FromJson<StoryDto>(Utf8NoBom.GetString(bytes));
            }
            catch (Exception e)
            {
                return RepoResult<Story>.Fail(RepoError.Corrupt, e.Message);
            }

            if (dto == null || dto.version <= 0) return RepoResult<Story>.Fail(RepoError.Corrupt, "Missing or invalid version.");
            if (dto.version > CurrentVersion)
                return RepoResult<Story>.Fail(RepoError.UnsupportedVersion, "Unsupported version " + dto.version + ".");
            if (dto.version < CurrentVersion) dto = Migrate(dto);
            if (dto.id != id) return RepoResult<Story>.Fail(RepoError.Corrupt, "Id does not match file name.");

            return FromDto(dto);
        }

        private static StoryDto Migrate(StoryDto dto)
        {
            if (dto.version == 1)
            {
                dto.entryCount = dto.entries?.Count ?? 0;
                dto.importedAtUnixMs = 0;
            }
            dto.version = CurrentVersion;
            return dto;
        }

        private static RepoResult<Story> FromDto(StoryDto dto)
        {
            var entries = new List<KanjiEntry>();
            if (dto.entries != null)
            {
                foreach (EntryDto e in dto.entries)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.kanji))
                        return RepoResult<Story>.Fail(RepoError.Corrupt, "Entry without kanji.");
                    entries.Add(new KanjiEntry(e.kanji, e.reading, e.meaning, e.sentence, e.sentenceMeaning));
                }
            }

            var segments = new List<Segment>();
            if (dto.segments != null)
            {
                foreach (SegmentDto s in dto.segments)
                {
                    if (s == null) return RepoResult<Story>.Fail(RepoError.Corrupt, "Null segment.");
                    if (s.kind == (int)SegmentKind.Text)
                    {
                        segments.Add(Segment.Plain(s.text));
                    }
                    else if (s.kind == (int)SegmentKind.Kanji)
                    {
                        if (s.entry < 0 || s.entry >= entries.Count)
                            return RepoResult<Story>.Fail(RepoError.Corrupt, "Segment entry out of range.");
                        segments.Add(Segment.ForKanji(entries[s.entry].Kanji, s.entry));
                    }
                    else
                    {
                        return RepoResult<Story>.Fail(RepoError.Corrupt, "Unknown segment kind.");
                    }
                }
            }

            return RepoResult<Story>.Ok(new Story(dto.id, dto.name, segments, entries, FromUnixMs(dto.importedAtUnixMs)));
        }

        private static StoryDto ToDto(Story story)
        {
            var dto = new StoryDto
            {
                version = CurrentVersion,
                id = story.Id,
                name = story.Name,
                importedAtUnixMs = ToUnixMs(story.ImportedAtUtc),
                entryCount = story.Entries.Count,
                entries = new List<EntryDto>(story.Entries.Count),
                segments = new List<SegmentDto>(story.Segments.Count),
            };

            foreach (KanjiEntry e in story.Entries)
            {
                dto.entries.Add(new EntryDto
                {
                    kanji = e.Kanji, reading = e.Reading, meaning = e.Meaning,
                    sentence = e.Sentence, sentenceMeaning = e.SentenceMeaning,
                });
            }

            foreach (Segment s in story.Segments)
            {
                bool isKanji = s.Kind == SegmentKind.Kanji;
                dto.segments.Add(new SegmentDto
                {
                    kind = (int)s.Kind,
                    text = isKanji ? string.Empty : s.Text,
                    entry = isKanji ? s.EntryIndex : -1,
                });
            }

            return dto;
        }

        [Serializable]
        private sealed class StoryDto
        {
            public int version;
            public string id = "";
            public string name = "";
            // Header fields stay before the heavy lists (v2).
            public long importedAtUnixMs;
            public int entryCount;
            public List<EntryDto> entries = new List<EntryDto>();
            public List<SegmentDto> segments = new List<SegmentDto>();
        }

        /// <summary>Subset of StoryDto read by List(); JsonUtility ignores the rest.</summary>
        [Serializable]
        private sealed class HeaderDto
        {
            public int version;
            public string id = "";
            public string name = "";
            public long importedAtUnixMs;
            public int entryCount;
        }

        [Serializable]
        private sealed class EntryDto
        {
            public string kanji = "";
            public string reading = "";
            public string meaning = "";
            public string sentence = "";
            public string sentenceMeaning = "";
        }

        [Serializable]
        private sealed class SegmentDto
        {
            public int kind;
            public string text = "";
            public int entry = -1;
        }
    }
}
