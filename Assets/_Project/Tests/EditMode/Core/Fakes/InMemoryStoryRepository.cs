using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;

namespace Kanji.Tests.Core.Fakes
{
    /// <summary>In-memory IStoryRepository for tests; List() returns insertion order.</summary>
    public sealed class InMemoryStoryRepository : IStoryRepository
    {
        private readonly List<Story> _stories = new List<Story>();

        public bool FailOnSave { get; set; }
        public int SaveCount { get; private set; }

        public int UnreadableCount { get; set; }

        public StoryListing List()
        {
            var infos = new List<StoryInfo>();
            foreach (Story s in _stories)
                infos.Add(new StoryInfo(s.Id, s.Name, s.Entries.Count, s.ImportedAtUtc));
            return new StoryListing(infos, UnreadableCount);
        }

        public RepoResult<Story> Load(string id)
        {
            int i = IndexOf(id);
            return i < 0
                ? RepoResult<Story>.Fail(RepoError.NotFound, "not found")
                : RepoResult<Story>.Ok(_stories[i]);
        }

        public RepoResult Save(Story story)
        {
            if (FailOnSave) return RepoResult.Fail(RepoError.IoError, "disk full");
            SaveCount++;
            int i = IndexOf(story.Id);
            if (i < 0) _stories.Add(story); else _stories[i] = story;
            return RepoResult.Ok();
        }

        public RepoResult Delete(string id)
        {
            int i = IndexOf(id);
            if (i >= 0) _stories.RemoveAt(i);
            return RepoResult.Ok();
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < _stories.Count; i++)
                if (_stories[i].Id == id) return i;
            return -1;
        }
    }
}
