using System;
using System.Collections.Generic;

namespace Kanji.Core.Library
{
    /// <summary>Facade over the per-story difficult lists, with a cache. Created once by the composition root.</summary>
    public sealed class DifficultKanjiStore
    {
        private readonly IDifficultKanjiRepository _repo;
        private readonly Dictionary<string, DifficultKanjiList> _lists = new Dictionary<string, DifficultKanjiList>();
        private int _deletedCount;

        public DifficultKanjiStore(IDifficultKanjiRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <summary>Sum of changes in all lists plus story deletions; lets screens detect staleness.</summary>
        public int Revision
        {
            get
            {
                int sum = _deletedCount;
                foreach (DifficultKanjiList l in _lists.Values) sum += l.Revision;
                return sum;
            }
        }

        public DifficultKanjiList For(string storyId)
        {
            if (storyId == null) throw new ArgumentNullException(nameof(storyId));
            if (!_lists.TryGetValue(storyId, out DifficultKanjiList list))
            {
                list = new DifficultKanjiList(_repo, storyId);
                _lists[storyId] = list;
            }
            return list;
        }

        public int Count(string storyId) => For(storyId).Items.Count;

        public IReadOnlyList<string> StoryIdsWithItems()
        {
            var result = new List<string>();
            IReadOnlyList<string> ids = _repo.ListStoryIds();
            if (ids == null) return result;
            for (int i = 0; i < ids.Count; i++)
                if (Count(ids[i]) > 0) result.Add(ids[i]);
            return result;
        }

        public RepoResult DeleteStory(string storyId)
        {
            RepoResult r = _repo.Delete(storyId);
            if (!r.IsSuccess) return r;
            // Keep Revision monotonic: fold the dropped list's revisions into the counter.
            _deletedCount++;
            if (_lists.TryGetValue(storyId, out DifficultKanjiList dropped))
            {
                _deletedCount += dropped.Revision;
                _lists.Remove(storyId);
            }
            return r;
        }
    }
}
