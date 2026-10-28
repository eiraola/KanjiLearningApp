using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Tests.Core.Fakes;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class DifficultStorySelectorTests
    {
        private sealed class MemRepo : IDifficultKanjiRepository
        {
            private readonly Dictionary<string, List<KanjiEntry>> _data = new Dictionary<string, List<KanjiEntry>>();
            public IReadOnlyList<KanjiEntry> Load(string id) =>
                _data.TryGetValue(id, out var l) ? l : new List<KanjiEntry>();
            public RepoResult Save(string id, IReadOnlyList<KanjiEntry> items)
            {
                _data[id] = new List<KanjiEntry>(items);
                return RepoResult.Ok();
            }
            public RepoResult Delete(string id) { _data.Remove(id); return RepoResult.Ok(); }
            public IReadOnlyList<string> ListStoryIds() => new List<string>(_data.Keys);
        }

        private static KanjiEntry E(string k) => new KanjiEntry(k, "r", "m", "s", "sm");

        private static string Import(StoryLibrary lib, string name) =>
            lib.Import(new Story("", "", null, new List<KanjiEntry> { E("木") }), name).Story.Id;

        [Test]
        public void Build_OnlyStoriesWithItems_InLibraryOrder_WithCounts()
        {
            var day = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int n = 0;
            var lib = new StoryLibrary(new InMemoryStoryRepository(), () => "s" + (n++), () => day.AddDays(n));
            string a = Import(lib, "A"), b = Import(lib, "B"), c = Import(lib, "C");
            var store = new DifficultKanjiStore(new MemRepo());
            store.For(a).Add(E("木"));
            store.For(c).Add(E("木"));
            store.For(c).Add(E("火"));

            List<DifficultStoryItem> items = DifficultStorySelector.Build(lib, store);

            // Same order as the library list (GDD 2.3), B (no difficult) filtered out.
            var expected = new List<string>();
            foreach (StoryInfo s in lib.List().Items) if (s.Id != b) expected.Add(s.Id);
            Assert.AreEqual(expected.Count, items.Count);
            for (int i = 0; i < items.Count; i++) Assert.AreEqual(expected[i], items[i].Story.Id);
            Assert.AreEqual(2, items.Find(x => x.Story.Id == c).Count);
            Assert.AreEqual(1, items.Find(x => x.Story.Id == a).Count);
        }

        [Test]
        public void Build_NoDifficult_IsEmpty()
        {
            var lib = new StoryLibrary(new InMemoryStoryRepository(), () => "x", () => DateTime.UtcNow);
            Import(lib, "A");
            Assert.IsEmpty(DifficultStorySelector.Build(lib, new DifficultKanjiStore(new MemRepo())));
        }
    }
}
