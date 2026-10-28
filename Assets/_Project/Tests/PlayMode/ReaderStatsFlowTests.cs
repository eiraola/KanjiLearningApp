using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using Kanji.Core.Reading;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Kanji.UI.Tests
{
    /// <summary>Reader -> answers in KanjiPopup -> close -> StatsScreen -> Accept, on hand-wired views.</summary>
    public sealed class ReaderStatsFlowTests
    {
        private const string Fixture = "a {Kanji: 水; Reading: みず} b {Kanji: 山; Reading: やま} c {Kanji: 火; Reading: ひ} d";

        private sealed class StubScreen : ScreenView { }

        private sealed class MemoryStories : IStoryRepository
        {
            public readonly Dictionary<string, Story> Stories = new Dictionary<string, Story>();
            public StoryListing List() => throw new NotSupportedException();
            public RepoResult<Story> Load(string id) => RepoResult<Story>.Ok(Stories[id]);
            public RepoResult Save(Story story) => throw new NotSupportedException();
            public RepoResult Delete(string id) => throw new NotSupportedException();
        }

        private readonly List<Object> _created = new List<Object>();
        private ScreenNavigator _navigator;
        private StubScreen _list;
        private ReaderScreen _reader;
        private StatsScreen _stats;
        private KanjiPopup _popup;
        private Button _readerBack;
        private Button _knew;
        private Button _didntKnow;
        private Button _accept;
        private TMP_Text _failedCount;
        private RectTransform _failedContent;
        private MemoryStories _repo;

        [SetUp]
        public void SetUp()
        {
            var layer = Make("Layer", null);
            _repo = new MemoryStories();
            _repo.Stories["withKanji"] = new StoryParser().Parse("t", Fixture).Story;
            _repo.Stories["noKanji"] = new StoryParser().Parse("t", "sin kanjis").Story;
            var library = new StoryLibrary(_repo, () => "id", () => DateTime.UtcNow);

            _popup = BuildPopup(layer.transform);
            _list = Make("List", layer.transform).AddComponent<StubScreen>();
            _reader = BuildReader(layer.transform);
            _stats = BuildStats(layer.transform);

            _navigator = new ScreenNavigator(new Dictionary<ScreenId, IScreen>
            {
                { ScreenId.StoryList, _list }, { ScreenId.Reader, _reader }, { ScreenId.Stats, _stats }
            });
            _reader.Init(_navigator, library, null, _popup);
            _stats.Init(_navigator);
            _reader.OnHide();
            _stats.OnHide();
            _navigator.Push(ScreenId.StoryList);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [UnityTest]
        public IEnumerator GreenRedAndUnanswered_ShowStatsAndAcceptReturnsToList()
        {
            yield return OpenStory("withKanji");

            Tap(0); _knew.onClick.Invoke();
            Tap(1); _didntKnow.onClick.Invoke();
            Tap(2); _popup.Close(); // dismissed: counts as passed
            _readerBack.onClick.Invoke();

            Assert.AreSame(_stats, _navigator.Current);
            ReadingStats stats = _stats.Stats;
            Assert.AreEqual(3, stats.Total);
            Assert.AreEqual(1, stats.Failed);
            Assert.AreEqual(67, stats.Percent);
            Assert.AreEqual("山", stats.FailedEntries[0].Kanji);
            Assert.AreEqual("1", _failedCount.text);
            Assert.AreEqual(1, ActiveRows());

            _accept.onClick.Invoke();
            Assert.AreSame(_list, _navigator.Current);
        }

        [UnityTest]
        public IEnumerator ClosingWithPopupOpen_KeepsEarlierAnswerAndShowsStats()
        {
            yield return OpenStory("withKanji");

            Tap(1); _didntKnow.onClick.Invoke();
            Tap(1); // reopened, left open while the reader closes
            Assert.IsTrue(_reader.HandleBack());

            Assert.AreSame(_stats, _navigator.Current);
            Assert.AreEqual(1, _stats.Stats.Failed);
            Assert.IsFalse(_popup.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator ReopeningStory_StartsAFreshSession()
        {
            yield return OpenStory("withKanji");
            Tap(0); _didntKnow.onClick.Invoke();
            _readerBack.onClick.Invoke();
            _accept.onClick.Invoke();

            yield return OpenStory("withKanji");
            _readerBack.onClick.Invoke();

            Assert.AreEqual(0, _stats.Stats.Failed);
            Assert.AreEqual(100, _stats.Stats.Percent);
            Assert.AreEqual(0, ActiveRows());
        }

        [UnityTest]
        public IEnumerator StoryWithoutKanji_ClosesStraightToList()
        {
            yield return OpenStory("noKanji");
            _readerBack.onClick.Invoke();
            Assert.AreSame(_list, _navigator.Current);
        }

        [Test]
        public void ClosingWhileLoading_ReturnsToList()
        {
            _navigator.Push(ScreenId.Reader, Info("withKanji"));
            Assert.IsTrue(_reader.HandleBack());
            Assert.AreSame(_list, _navigator.Current);
        }

        // ---------- helpers ----------

        private IEnumerator OpenStory(string id)
        {
            _navigator.Push(ScreenId.Reader, Info(id));
            for (int i = 0; i < 30 && Field<Story>(_reader, "_story") == null; i++) yield return null;
            Assert.IsNotNull(Field<Story>(_reader, "_story"), "story did not open");
        }

        private StoryInfo Info(string id) => new StoryInfo(id, "t", _repo.Stories[id].Entries.Count, DateTime.UtcNow);

        private void Tap(int index) =>
            typeof(ReaderScreen).GetMethod("OnEntryTapped", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_reader, new object[] { index });

        private int ActiveRows()
        {
            int n = 0;
            foreach (Transform child in _failedContent) if (child.gameObject.activeSelf) n++;
            return n;
        }

        private KanjiPopup BuildPopup(Transform layer)
        {
            var blocker = Make("Blocker", layer);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            blocker.AddComponent<CanvasGroup>();
            var service = new PopupService(blocker);

            var root = Make("Popup", layer);
            root.SetActive(false); // keep Awake pending until fields are wired
            var popup = root.AddComponent<KanjiPopup>();
            var content = Make("Content", root.transform).GetComponent<RectTransform>();
            var scroll = Make("Body", root.transform).AddComponent<ScrollRect>();
            scroll.content = content;
            _knew = NewButton("Knew", root.transform);
            _didntKnow = NewButton("DidntKnow", root.transform);
            Set(popup, "_kanji", NewText("Kanji", content));
            Set(popup, "_sentence", NewText("Sentence", content));
            Set(popup, "_reading", NewText("Reading", content));
            Set(popup, "_meaning", NewText("Meaning", content));
            Set(popup, "_sentenceMeaning", NewText("SentenceMeaning", content));
            Set(popup, "_rowReading", Make("RowReading", content));
            Set(popup, "_rowMeaning", Make("RowMeaning", content));
            Set(popup, "_rowSentenceMeaning", Make("RowSentenceMeaning", content));
            Set(popup, "_btnKnew", _knew);
            Set(popup, "_btnDidntKnow", _didntKnow);
            Set(popup, "_body", scroll);
            Set(popup, "_bodyLayout", root.AddComponent<LayoutElement>());
            Set(popup, "_bodyContent", content);
            root.SetActive(true);
            popup.Init(service, null, null);
            root.SetActive(false);
            return popup;
        }

        private ReaderScreen BuildReader(Transform layer)
        {
            var root = Make("Reader", layer);
            var reader = root.AddComponent<ReaderScreen>();
            var scroll = Make("Scroll", root.transform).AddComponent<ScrollRect>();
            var content = Make("Content", scroll.transform).GetComponent<RectTransform>();
            scroll.content = content;
            var chunk = Make("Chunk", content).AddComponent<TextMeshProUGUI>();
            chunk.gameObject.AddComponent<LinkClickHandler>();
            chunk.gameObject.SetActive(false);
            var theme = ScriptableObject.CreateInstance<UiTheme>();
            _created.Add(theme);
            _readerBack = NewButton("Back", root.transform);
            Set(reader, "_backButton", _readerBack);
            Set(reader, "_title", NewText("Title", root.transform));
            Set(reader, "_scroll", scroll);
            Set(reader, "_content", content);
            Set(reader, "_chunkTemplate", chunk);
            Set(reader, "_theme", theme);
            return reader;
        }

        private StatsScreen BuildStats(Transform layer)
        {
            var root = Make("Stats", layer);
            var stats = root.AddComponent<StatsScreen>();
            var scroll = Make("FailedList", root.transform).AddComponent<ScrollRect>();
            _failedContent = Make("Content", scroll.transform).GetComponent<RectTransform>();
            scroll.content = _failedContent;
            var row = Make("RowTemplate", root.transform).AddComponent<TextMeshProUGUI>();
            row.gameObject.SetActive(false);
            _accept = NewButton("Accept", root.transform);
            _failedCount = NewText("Failed", root.transform);
            Set(stats, "_backButton", NewButton("Back", root.transform));
            Set(stats, "_acceptButton", _accept);
            Set(stats, "_total", NewText("Total", root.transform));
            Set(stats, "_passed", NewText("Passed", root.transform));
            Set(stats, "_failed", _failedCount);
            Set(stats, "_percent", NewText("Percent", root.transform));
            Set(stats, "_failedScroll", scroll);
            Set(stats, "_failedContent", _failedContent);
            Set(stats, "_failedRowTemplate", row);
            Set(stats, "_noFailedMessage", Make("NoFailed", root.transform));
            return stats;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private static T Field<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

        private GameObject Make(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            _created.Add(go);
            return go;
        }

        private Button NewButton(string name, Transform parent)
        {
            var go = Make(name, parent);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        private TMP_Text NewText(string name, Transform parent) => Make(name, parent).AddComponent<TextMeshProUGUI>();
    }
}
