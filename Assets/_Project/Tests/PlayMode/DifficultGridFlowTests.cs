using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.UI;

namespace Kanji.UI.Tests
{
    public sealed class DifficultGridFlowTests
    {
        private sealed class MemoryRepo : IDifficultKanjiRepository
        {
            private readonly Dictionary<string, List<KanjiEntry>> _saved = new Dictionary<string, List<KanjiEntry>>();
            public IReadOnlyList<KanjiEntry> Load(string storyId) =>
                _saved.TryGetValue(storyId, out var l) ? l : new List<KanjiEntry>();
            public RepoResult Delete(string storyId) => RepoResult.Ok();
            public IReadOnlyList<string> ListStoryIds() => new List<string>();
            public RepoResult Save(string storyId, IReadOnlyList<KanjiEntry> items)
            {
                _saved[storyId] = new List<KanjiEntry>(items);
                return RepoResult.Ok();
            }
        }

        private sealed class FakeScreen : IScreen
        {
            public FakeScreen(ScreenId id) { Id = id; }
            public ScreenId Id { get; }
            public object LastArgs;
            public void OnShow(object args) { LastArgs = args; }
            public void OnHide() { }
            public bool HandleBack() => false;
        }

        private readonly List<GameObject> _created = new List<GameObject>();
        private DifficultKanjiStore _store;
        private ScreenNavigator _navigator;
        private DifficultGridScreen _grid;
        private KanjiPopup _popup;
        private RectTransform _content;
        private Button _remove;
        private Button _reveal, _knew, _didntKnow, _study;
        private FakeScreen _stats;
        private readonly KanjiEntry _a = new KanjiEntry("山", "やま", "montaña", "山が高い。", "La montaña es alta.");
        private readonly KanjiEntry _b = new KanjiEntry("川", "かわ", "río", "川が長い。", "El río es largo.");

        [SetUp]
        public void SetUp()
        {
            _store = new DifficultKanjiStore(new MemoryRepo());
            _store.For("s").Add(_a);
            _store.For("s").Add(_b);

            var layer = Make("Layer", null);
            var blocker = Make("Blocker", layer.transform);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            blocker.AddComponent<CanvasGroup>();
            var service = new PopupService(blocker);
            _popup = BuildPopup(layer.transform);
            _popup.Init(service, null, null, _store);

            _grid = BuildGrid(layer.transform);
            var screens = new Dictionary<ScreenId, IScreen>
            {
                { ScreenId.DifficultList, new FakeScreen(ScreenId.DifficultList) },
                { ScreenId.DifficultGrid, _grid },
                { ScreenId.Stats, _stats = new FakeScreen(ScreenId.Stats) },
            };
            _navigator = new ScreenNavigator(screens);
            _grid.Init(_navigator, null, _store, _popup);
            _navigator.Push(ScreenId.DifficultList);
            _navigator.Push(ScreenId.DifficultGrid, "s");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        [Test]
        public void Remove_UpdatesGrid_ThenReturnsToSelectorWhenEmpty()
        {
            Assert.AreEqual(2, ActiveCells());

            _content.GetChild(0).GetComponent<Button>().onClick.Invoke(); // newest first: 川
            _remove.onClick.Invoke();

            Assert.IsFalse(_store.For("s").Contains(_b));
            Assert.IsTrue(_store.For("s").Contains(_a));
            Assert.AreEqual(1, ActiveCells());
            Assert.AreSame(_grid, _navigator.Current);

            _content.GetChild(0).GetComponent<Button>().onClick.Invoke();
            _remove.onClick.Invoke();

            Assert.AreEqual(0, _store.For("s").Items.Count);
            Assert.AreEqual(ScreenId.DifficultList, _navigator.Current.Id);
        }

        [Test]
        public void Study_RevealThenAnswerAll_ShowsStatsAndFillsCircles()
        {
            GameObject answerRow = _knew.transform.parent.Find("AnswerRow").gameObject;
            _study.onClick.Invoke();

            Assert.IsTrue(_popup.gameObject.activeSelf);
            Assert.IsTrue(_reveal.gameObject.activeSelf);
            Assert.IsFalse(answerRow.activeSelf);
            Assert.IsFalse(_popup.transform.Find("Content/RowReading").gameObject.activeSelf);

            _reveal.onClick.Invoke();
            Assert.IsFalse(_reveal.gameObject.activeSelf);
            Assert.IsTrue(answerRow.activeSelf);
            Assert.IsTrue(_popup.transform.Find("Content/RowReading").gameObject.activeSelf);

            KanjiEntry first = _grid.Session.Current;
            _knew.onClick.Invoke();
            Assert.IsTrue(_popup.gameObject.activeSelf, "popup stays open for the next kanji");
            Assert.IsTrue(_reveal.gameObject.activeSelf, "next kanji starts hidden again");
            KanjiEntry second = _grid.Session.Current;
            Assert.AreNotSame(first, second);
            _reveal.onClick.Invoke();
            _didntKnow.onClick.Invoke();

            Assert.IsNull(_grid.Session);
            Assert.AreSame(_stats, _navigator.Current);
            var stats = (Kanji.Core.Reading.ReadingStats)_stats.LastArgs;
            Assert.AreEqual(2, stats.Total);
            Assert.AreEqual(50, stats.Percent);
            Assert.AreEqual(1, _store.For("s").ProgressOf(first));
            Assert.AreEqual(0, _store.For("s").ProgressOf(second));

            _navigator.Pop();
            Assert.AreSame(_grid, _navigator.Current);
            float fills = 0f;
            for (var i = 0; i < _content.childCount; i++) fills += _content.GetChild(i).GetComponent<DifficultKanjiCell>().ProgressFill;
            Assert.AreEqual(0.2f, fills, 0.001f);
        }

        [Test]
        public void Study_BackBeforeAnswering_ReturnsToListWithoutStats()
        {
            _study.onClick.Invoke();
            _popup.Cancel();

            Assert.IsNull(_grid.Session);
            Assert.AreSame(_grid, _navigator.Current);
            Assert.IsNull(_stats.LastArgs);
        }

        private int ActiveCells()
        {
            var n = 0;
            for (var i = 0; i < _content.childCount; i++) if (_content.GetChild(i).gameObject.activeSelf) n++;
            return n;
        }

        private DifficultGridScreen BuildGrid(Transform parent)
        {
            var root = Make("Grid", parent);
            root.SetActive(false);
            var screen = root.AddComponent<DifficultGridScreen>();
            var scrollGo = Make("Scroll", root.transform);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = Make("Viewport", scrollGo.transform).GetComponent<RectTransform>();
            _content = Make("Content", viewport).GetComponent<RectTransform>();
            _content.gameObject.AddComponent<VerticalLayoutGroup>();
            scroll.viewport = viewport;
            scroll.content = _content;

            var cellGo = Make("Cell", null);
            cellGo.SetActive(false);
            var cellButton = cellGo.AddComponent<Button>();
            cellGo.AddComponent<Image>();
            var cell = cellGo.AddComponent<DifficultKanjiCell>();
            SetOn(cell, "_button", cellButton);
            SetOn(cell, "_kanji", NewText("K", cellGo.transform));
            SetOn(cell, "_progressFill", Make("Fill", cellGo.transform).AddComponent<Image>());

            SetOn(screen, "_backButton", NewButton("Back", root.transform));
            SetOn(screen, "_title", NewText("Title", root.transform));
            SetOn(screen, "_scroll", scroll);
            SetOn(screen, "_content", _content);
            SetOn(screen, "_cellPrefab", cell);
            SetOn(screen, "_emptyMessage", NewText("Empty", root.transform));
            SetOn(screen, "_studyButton", _study = NewButton("Study", root.transform));
            return screen;
        }

        private KanjiPopup BuildPopup(Transform parent)
        {
            var root = Make("Popup", parent);
            root.SetActive(false);
            var popup = root.AddComponent<KanjiPopup>();
            var content = Make("Content", root.transform).GetComponent<RectTransform>();
            var scroll = Make("Body", root.transform).AddComponent<ScrollRect>();
            scroll.content = content;
            _remove = NewButton("Remove", root.transform);
            SetOn(popup, "_kanji", NewText("Kanji", content));
            SetOn(popup, "_sentence", NewText("Sentence", content));
            SetOn(popup, "_reading", NewText("Reading", content));
            SetOn(popup, "_meaning", NewText("Meaning", content));
            SetOn(popup, "_sentenceMeaning", NewText("SentenceMeaning", content));
            SetOn(popup, "_rowReading", Make("RowReading", content));
            SetOn(popup, "_rowMeaning", Make("RowMeaning", content));
            SetOn(popup, "_rowSentenceMeaning", Make("RowSentenceMeaning", content));
            SetOn(popup, "_btnKnew", _knew = NewButton("Knew", root.transform));
            SetOn(popup, "_btnDidntKnow", _didntKnow = NewButton("DidntKnow", root.transform));
            SetOn(popup, "_btnReveal", _reveal = NewButton("Reveal", root.transform));
            SetOn(popup, "_secondaryActions", Make("Secondary", root.transform).GetComponent<RectTransform>());
            SetOn(popup, "_btnAddDifficult", NewButton("Add", root.transform));
            SetOn(popup, "_btnRemoveDifficult", _remove);
            SetOn(popup, "_addDifficultLabel", NewText("AddLabel", root.transform));
            SetOn(popup, "_addDifficultIcon", Make("Icon", root.transform).AddComponent<Image>());
            SetOn(popup, "_answerRow", Make("AnswerRow", root.transform));
            SetOn(popup, "_body", scroll);
            SetOn(popup, "_bodyLayout", root.AddComponent<LayoutElement>());
            SetOn(popup, "_bodyContent", content);
            root.SetActive(true); // Awake wires the buttons
            root.SetActive(false);
            return popup;
        }

        private static void SetOn(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

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
