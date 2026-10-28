using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    /// <summary>Port for persisting the "difficult kanji" list of each story. Order of the array is the list order.</summary>
    public interface IDifficultKanjiRepository
    {
        /// <summary>Never throws; missing, corrupt or future-version data yields an empty list.</summary>
        IReadOnlyList<KanjiEntry> Load(string storyId);

        RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items);

        /// <summary>Study successes (0..5) per item, same order as <see cref="Load"/>; null when not stored.</summary>
        IReadOnlyList<int> LoadProgress(string storyId) => null;

        /// <summary>Saves items with their study successes (same order). Default ignores progress.</summary>
        RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items, IReadOnlyList<int> progress) => Save(storyId, items);

        /// <summary>A missing list counts as success.</summary>
        RepoResult Delete(string storyId);

        IReadOnlyList<string> ListStoryIds();
    }
}
