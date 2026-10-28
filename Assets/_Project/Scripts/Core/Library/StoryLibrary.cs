using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    public enum ImportStatus { Imported, Duplicate, Failed }

    public sealed class ImportResult
    {
        private ImportResult(ImportStatus status, Story story, string conflictingName, RepoError error, string message)
        {
            Status = status;
            Story = story;
            ConflictingName = conflictingName ?? string.Empty;
            Error = error;
            Message = message ?? string.Empty;
        }

        public ImportStatus Status { get; }
        public Story Story { get; }
        public string ConflictingName { get; }
        public RepoError Error { get; }
        public string Message { get; }

        public static ImportResult Imported(Story story) =>
            new ImportResult(ImportStatus.Imported, story, string.Empty, RepoError.None, string.Empty);

        public static ImportResult Duplicate(string existingName) =>
            new ImportResult(ImportStatus.Duplicate, null, existingName, RepoError.None, string.Empty);

        public static ImportResult Failed(RepoError error, string message) =>
            new ImportResult(ImportStatus.Failed, null, string.Empty, error, message);
    }

    /// <summary>Story library use cases on top of a repository: import with a user-chosen name, rename, list, load, delete.</summary>
    public sealed class StoryLibrary
    {
        private readonly IStoryRepository _repository;
        private readonly Func<string> _newId;
        private readonly Func<DateTime> _utcNow;

        public StoryLibrary(IStoryRepository repository, Func<string> newId, Func<DateTime> utcNow)
        {
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _newId = newId ?? throw new ArgumentNullException(nameof(newId));
        }

        /// <summary>Incremented after every successful import, rename or delete; views compare it to know when to re-render.</summary>
        public int Revision { get; private set; }

        /// <summary>Validates a candidate name (see <see cref="StoryNameValidator"/>) without touching the repository.</summary>
        public NameResult Check(string input, string fallback, string ownId) =>
            StoryNameValidator.Validate(input, List().Items, fallback, ownId);

        public ImportResult Import(Story parsed, string name)
        {
            if (parsed == null) throw new ArgumentNullException(nameof(parsed));

            NameResult check = Check(name, null, null);
            if (check.Check == NameCheck.Duplicate) return ImportResult.Duplicate(check.ConflictingName);
            if (check.Check == NameCheck.Empty) return ImportResult.Failed(RepoError.NameInvalid, "Empty story name.");

            return Save(parsed.With(_newId(), check.Name, _utcNow()));
        }

        /// <summary>Renames keeping id, import date and content. Bumps Revision on success.</summary>
        public RepoResult Rename(string id, string newName)
        {
            RepoResult<Story> loaded = _repository.Load(id);
            if (!loaded.IsSuccess) return RepoResult.Fail(loaded.Error, loaded.Message);

            NameResult check = Check(newName, null, id);
            if (check.Check == NameCheck.Empty) return RepoResult.Fail(RepoError.NameInvalid, "Empty story name.");
            if (check.Check == NameCheck.Duplicate) return RepoResult.Fail(RepoError.NameDuplicate, check.ConflictingName);

            Story story = loaded.Value;
            RepoResult saved = _repository.Save(story.With(story.Id, check.Name, story.ImportedAtUtc));
            if (saved.IsSuccess) Revision++;
            return saved;
        }

        public StoryListing List()
        {
            StoryListing raw = _repository.List();
            var sorted = new List<StoryInfo>(raw.Items);
            sorted.Sort(CompareInfo);
            return new StoryListing(sorted, raw.UnreadableCount);
        }

        public RepoResult<Story> Load(string id) => _repository.Load(id);

        public RepoResult Delete(string id)
        {
            RepoResult result = _repository.Delete(id);
            if (!result.IsSuccess) return result;
            Revision++;
            Deleted?.Invoke(id);
            return result;
        }

        /// <summary>Raised after a story was deleted successfully (cascade hook for per-story data).</summary>
        public event Action<string> Deleted;

        private ImportResult Save(Story story)
        {
            RepoResult result = _repository.Save(story);
            if (!result.IsSuccess) return ImportResult.Failed(result.Error, result.Message);
            Revision++;
            return ImportResult.Imported(story);
        }

        private static int CompareInfo(StoryInfo a, StoryInfo b)
        {
            int byDate = b.ImportedAtUtc.CompareTo(a.ImportedAtUtc);
            if (byDate != 0) return byDate;
            int byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
