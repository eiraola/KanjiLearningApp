using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;

namespace Kanji.Core.Words
{
    /// <summary>Word list use cases: create, list, load and paste words (plan first, then apply).</summary>
    public sealed class WordListLibrary
    {
        private readonly IWordListRepository _repository;
        private readonly Func<string> _newId;
        private readonly Func<DateTime> _utcNow;

        public WordListLibrary(IWordListRepository repository, Func<string> newId, Func<DateTime> utcNow)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _newId = newId ?? throw new ArgumentNullException(nameof(newId));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        /// <summary>Incremented after every successful create or word import; views compare it to re-render.</summary>
        public int Revision { get; private set; }

        /// <summary>Newest first, then by name.</summary>
        public WordListListing List()
        {
            WordListListing raw = _repository.List();
            var sorted = new List<WordListInfo>(raw.Items);
            sorted.Sort((a, b) =>
            {
                int byDate = b.CreatedAtUtc.CompareTo(a.CreatedAtUtc);
                if (byDate != 0) return byDate;
                int byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : string.CompareOrdinal(a.Id, b.Id);
            });
            return new WordListListing(sorted, raw.UnreadableCount);
        }

        /// <summary>Same rules as story names: trimmed, required, unique ignoring case.</summary>
        public NameResult Check(string input, string ownId = null)
        {
            string name = (input ?? string.Empty).Trim();
            if (name.Length == 0) return new NameResult(NameCheck.Empty, string.Empty, null);
            foreach (WordListInfo info in _repository.List().Items)
            {
                if (info.Id == ownId) continue;
                if (StoryName.AreSame(info.Name, name)) return new NameResult(NameCheck.Duplicate, name, info.Name);
            }
            return new NameResult(NameCheck.Ok, name, null);
        }

        public RepoResult<WordListInfo> Create(string name)
        {
            NameResult check = Check(name);
            if (check.Check == NameCheck.Empty) return RepoResult<WordListInfo>.Fail(RepoError.NameInvalid, "Empty list name.");
            if (check.Check == NameCheck.Duplicate)
                return RepoResult<WordListInfo>.Fail(RepoError.NameDuplicate, check.ConflictingName);

            var list = new WordList(_newId(), check.Name, _utcNow(), Array.Empty<WordEntry>());
            RepoResult saved = _repository.Save(list);
            if (!saved.IsSuccess) return RepoResult<WordListInfo>.Fail(saved.Error, saved.Message);
            Revision++;
            return RepoResult<WordListInfo>.Ok(new WordListInfo(list.Id, list.Name, 0, list.CreatedAtUtc));
        }

        public RepoResult<WordList> Load(string id) => _repository.Load(id);

        /// <summary>Adds or removes the word from the list's difficult sub-list. Returns the updated word.</summary>
        public RepoResult<WordEntry> SetDifficult(string listId, string wordKey, bool difficult) =>
            Change(listId, wordKey, w => w.WithDifficult(difficult));

        /// <summary>Study answer: green adds 1 to that mode's progress (max 5), red leaves it (P9). Saved at once.</summary>
        public RepoResult<WordEntry> RecordStudy(string listId, string wordKey, bool knew, bool reverse = false) =>
            Change(listId, wordKey, w => reverse
                ? w.WithState(w.Progress, w.ReverseProgress + (knew ? 1 : 0), w.Difficult)
                : w.WithState(w.Progress + (knew ? 1 : 0), w.ReverseProgress, w.Difficult));

        private RepoResult<WordEntry> Change(string listId, string wordKey, Func<WordEntry, WordEntry> change)
        {
            RepoResult<WordList> loaded = _repository.Load(listId);
            if (!loaded.IsSuccess) return RepoResult<WordEntry>.Fail(loaded.Error, loaded.Message);
            var words = new List<WordEntry>(loaded.Value.Words);
            int index = words.FindIndex(w => string.Equals(w.Key, wordKey, StringComparison.Ordinal));
            if (index < 0) return RepoResult<WordEntry>.Fail(RepoError.NotFound, "Word not in list.");

            WordEntry before = words[index];
            WordEntry after = change(before);
            if (after.Progress == before.Progress && after.ReverseProgress == before.ReverseProgress && after.Difficult == before.Difficult)
                return RepoResult<WordEntry>.Ok(before);
            words[index] = after;
            RepoResult saved = _repository.Save(loaded.Value.WithWords(words));
            if (!saved.IsSuccess) return RepoResult<WordEntry>.Fail(saved.Error, saved.Message);
            Revision++;
            return RepoResult<WordEntry>.Ok(after);
        }

        /// <summary>Parses the pasted text against the list's current words. Saves nothing.</summary>
        public RepoResult<WordImportPlan> PrepareImport(string listId, string text)
        {
            RepoResult<WordList> loaded = _repository.Load(listId);
            if (!loaded.IsSuccess) return RepoResult<WordImportPlan>.Fail(loaded.Error, loaded.Message);
            return RepoResult<WordImportPlan>.Ok(WordImport.Plan(loaded.Value.Words, WordTsvParser.Parse(text)));
        }

        /// <param name="update">True: repeated words take the pasted data. False: they are left as they were.</param>
        public RepoResult<WordImportOutcome> ApplyImport(string listId, WordImportPlan plan, bool update)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            RepoResult<WordList> loaded = _repository.Load(listId);
            if (!loaded.IsSuccess) return RepoResult<WordImportOutcome>.Fail(loaded.Error, loaded.Message);

            IReadOnlyList<WordEntry> merged = WordImport.Merge(loaded.Value.Words, plan.Incoming, update,
                out WordImportOutcome outcome, plan.InvalidLines);
            if (outcome.Added == 0 && outcome.Updated == 0) return RepoResult<WordImportOutcome>.Ok(outcome);

            RepoResult saved = _repository.Save(loaded.Value.WithWords(merged));
            if (!saved.IsSuccess) return RepoResult<WordImportOutcome>.Fail(saved.Error, saved.Message);
            Revision++;
            return RepoResult<WordImportOutcome>.Ok(outcome);
        }
    }
}
