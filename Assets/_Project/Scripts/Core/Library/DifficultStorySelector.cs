using System.Collections.Generic;

namespace Kanji.Core.Library
{
    public readonly struct DifficultStoryItem
    {
        public DifficultStoryItem(StoryInfo story, int count)
        {
            Story = story;
            Count = count;
        }

        public StoryInfo Story { get; }
        public int Count { get; }
    }

    /// <summary>Stories with at least one difficult kanji, in library order (GDD 2.3).</summary>
    public static class DifficultStorySelector
    {
        public static List<DifficultStoryItem> Build(StoryLibrary library, DifficultKanjiStore store)
        {
            var result = new List<DifficultStoryItem>();
            IReadOnlyList<StoryInfo> stories = library.List().Items; // already sorted as 2.3
            for (int i = 0; i < stories.Count; i++)
            {
                int n = store.Count(stories[i].Id);
                if (n > 0) result.Add(new DifficultStoryItem(stories[i], n));
            }
            return result;
        }
    }
}
