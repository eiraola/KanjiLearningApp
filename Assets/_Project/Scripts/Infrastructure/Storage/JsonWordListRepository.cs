using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Kanji.Core.Library;
using Kanji.Core.Words;
using UnityEngine;

namespace Kanji.Infrastructure.Storage
{
    /// <summary>One JSON file per word list: root/wordlists/id.json, written atomically (ADR 0004).</summary>
    public sealed class JsonWordListRepository : IWordListRepository
    {
        public const string FolderName = "wordlists";
        private const int CurrentVersion = 1;
        private const string Extension = ".json";
        private static readonly Regex ValidId = new Regex("^[A-Za-z0-9_-]{1,64}\\z", RegexOptions.Compiled);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly IFileSystem _fs;
        private readonly AtomicFileWriter _writer;
        private readonly string _dir;

        public JsonWordListRepository(IFileSystem fs, string rootDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _writer = new AtomicFileWriter(fs);
            _dir = Path.Combine(rootDirectory, FolderName);
        }

        public WordListListing List()
        {
            var infos = new List<WordListInfo>();
            int unreadable = 0;
            try
            {
                foreach (string file in _fs.ListFiles(_dir, Extension))
                {
                    if (!string.Equals(Path.GetExtension(file), Extension, StringComparison.OrdinalIgnoreCase)) continue;
                    WordListInfo info = ReadInfo(file);
                    if (info != null) infos.Add(info);
                    else unreadable++;
                }
            }
            catch (Exception)
            {
                // Listing never throws; return what could be read.
            }
            return new WordListListing(infos, unreadable);
        }

        private WordListInfo ReadInfo(string file)
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (!IsValidId(id)) return null;
            try
            {
                var header = JsonUtility.FromJson<HeaderDto>(Utf8NoBom.GetString(_fs.ReadAllBytes(file)));
                if (header == null || header.version < 1 || header.version > CurrentVersion || header.id != id) return null;
                if (header.wordCount < 0) return null;
                return new WordListInfo(id, header.name, header.wordCount, FromUnixMs(header.createdAtUnixMs));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public RepoResult<WordList> Load(string id)
        {
            if (!IsValidId(id)) return RepoResult<WordList>.Fail(RepoError.InvalidId, "Invalid id.");
            string path = PathFor(id);
            byte[] bytes;
            try
            {
                if (!_fs.FileExists(path)) return RepoResult<WordList>.Fail(RepoError.NotFound, "List not found.");
                bytes = _fs.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                return RepoResult<WordList>.Fail(RepoError.IoError, e.Message);
            }

            ListDto dto;
            try { dto = JsonUtility.FromJson<ListDto>(Utf8NoBom.GetString(bytes)); }
            catch (Exception e) { return RepoResult<WordList>.Fail(RepoError.Corrupt, e.Message); }

            if (dto == null || dto.version <= 0) return RepoResult<WordList>.Fail(RepoError.Corrupt, "Missing or invalid version.");
            if (dto.version > CurrentVersion)
                return RepoResult<WordList>.Fail(RepoError.UnsupportedVersion, "Unsupported version " + dto.version + ".");
            if (dto.id != id) return RepoResult<WordList>.Fail(RepoError.Corrupt, "Id does not match file name.");

            var words = new List<WordEntry>();
            if (dto.words != null)
            {
                foreach (WordDto w in dto.words)
                {
                    if (w == null || string.IsNullOrWhiteSpace(w.word))
                        return RepoResult<WordList>.Fail(RepoError.Corrupt, "Word entry without word.");
                    words.Add(new WordEntry(w.word, w.reading, w.meaning, w.sentence, w.sentenceReading, w.sentenceMeaning, w.progress, w.difficult, w.reverseProgress));
                }
            }
            return RepoResult<WordList>.Ok(new WordList(dto.id, dto.name, FromUnixMs(dto.createdAtUnixMs), words));
        }

        public RepoResult Save(WordList list)
        {
            if (list == null) return RepoResult.Fail(RepoError.InvalidId, "List is null.");
            if (!IsValidId(list.Id)) return RepoResult.Fail(RepoError.InvalidId, "Invalid id.");

            byte[] data;
            try
            {
                var dto = new ListDto
                {
                    version = CurrentVersion,
                    id = list.Id,
                    name = list.Name,
                    createdAtUnixMs = ToUnixMs(list.CreatedAtUtc),
                    wordCount = list.Words.Count,
                    words = new List<WordDto>(list.Words.Count),
                };
                foreach (WordEntry w in list.Words)
                {
                    dto.words.Add(new WordDto
                    {
                        word = w.Word, reading = w.Reading, meaning = w.Meaning, sentence = w.Sentence,
                        sentenceReading = w.SentenceReading, sentenceMeaning = w.SentenceMeaning,
                        progress = w.Progress, difficult = w.Difficult, reverseProgress = w.ReverseProgress,
                    });
                }
                data = Utf8NoBom.GetBytes(JsonUtility.ToJson(dto));
            }
            catch (Exception e)
            {
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }
            return _writer.Write(PathFor(list.Id), data);
        }

        private static bool IsValidId(string id) => !string.IsNullOrEmpty(id) && ValidId.IsMatch(id);

        private string PathFor(string id) => Path.Combine(_dir, id + Extension);

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

        [Serializable]
        private sealed class ListDto
        {
            public int version;
            public string id = "";
            public string name = "";
            public long createdAtUnixMs;
            public int wordCount;
            public List<WordDto> words = new List<WordDto>();
        }

        /// <summary>Subset of ListDto read by List(); JsonUtility ignores the rest.</summary>
        [Serializable]
        private sealed class HeaderDto
        {
            public int version;
            public string id = "";
            public string name = "";
            public long createdAtUnixMs;
            public int wordCount;
        }

        [Serializable]
        private sealed class WordDto
        {
            public string word = "";
            public string reading = "";
            public string meaning = "";
            public string sentence = "";
            public string sentenceReading = "";
            public string sentenceMeaning = "";
            // S15: missing in S14 files -> 0 / false.
            public int progress;
            public bool difficult;
            // S16: missing in older files -> 0.
            public int reverseProgress;
        }
    }
}
