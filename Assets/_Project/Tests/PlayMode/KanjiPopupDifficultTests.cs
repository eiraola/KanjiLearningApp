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
    public sealed class KanjiPopupDifficultTests
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

        private readonly List<GameObject> _created = new List<GameObject>();
        private KanjiPopup _popup;
        private Button _add;
        private Button _remove;
        private TMP_Text _addLabel;
        private DifficultKanjiList _list;
        private readonly KanjiEntry _entry = new KanjiEntry("山", "やま", "montaña", "山が高い。", "La montaña es alta.");

        [SetUp]
        public void SetUp()
        {
            _list = new DifficultKanjiList(new MemoryRepo(), "s");
            var layer = Make("Layer", null);
            var blocker = Make("Blocker", layer.transform);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            blocker.AddComponent<CanvasGroup>();
            var service = new PopupService(blocker);

            var root = Make("Popup", layer.transform);
            root.SetActive(false); // keep Awake pending until fields are wired
            _popup = root.AddComponent<KanjiPopup>();
            var content = Make("Content", root.transform).GetComponent<RectTransform>();
            var scroll = Make("Body", root.transform).AddComponent<ScrollRect>();
            scroll.content = content;
            _add = NewButton("Add", root.transform);
            _remove = NewButton("Remove", root.transform);
            _addLabel = NewText("AddLabel", root.transform);

            Set("_kanji", NewText("Kanji", content));
            Set("_sentence", NewText("Sentence", content));
            Set("_reading", NewText("Reading", content));
            Set("_meaning", NewText("Meaning", content));
            Set("_sentenceMeaning", NewText("SentenceMeaning", content));
            Set("_rowReading", Make("RowReading", content));
            Set("_rowMeaning", Make("RowMeaning", content));
            Set("_rowSentenceMeaning", Make("RowSentenceMeaning", content));
            Set("_btnKnew", NewButton("Knew", root.transform));
            Set("_btnDidntKnow", NewButton("DidntKnow", root.transform));
            Set("_secondaryActions", Make("Secondary", root.transform).GetComponent<RectTransform>());
            Set("_btnAddDifficult", _add);
            Set("_btnRemoveDifficult", _remove);
            Set("_addDifficultLabel", _addLabel);
            Set("_addDifficultIcon", Make("Icon", root.transform).AddComponent<Image>());
            Set("_answerRow", Make("AnswerRow", root.transform));
            Set("_body", scroll);
            Set("_bodyLayout", root.AddComponent<LayoutElement>());
            Set("_bodyContent", content);

            root.SetActive(true);
            _popup.Init(service, _list, null);
            root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        [Test]
        public void ReaderMode_AddSwitchesButtonToAlreadyInDifficult()
        {
            _popup.Show(_entry);
            Assert.AreEqual(UiStrings.AddToDifficult, _addLabel.text);
            Assert.IsTrue(_add.gameObject.activeSelf);
            Assert.IsFalse(_remove.gameObject.activeSelf);

            _add.onClick.Invoke();

            Assert.IsTrue(_list.Contains(_entry));
            Assert.AreEqual(UiStrings.InDifficult, _addLabel.text);
            Assert.IsFalse(_add.interactable);
        }

        [Test]
        public void SameKanjiInTwoStories_AlreadyStateIsPerStory()
        {
            var store = new DifficultKanjiStore(new MemoryRepo());
            _popup.Init(new PopupService(_popup.transform.parent.GetChild(0).gameObject), _list, null, store);

            _popup.Show(_entry, "A");
            _add.onClick.Invoke();
            Assert.IsTrue(store.For("A").Contains(_entry));
            Assert.AreEqual(UiStrings.InDifficult, _addLabel.text);
            _popup.Close();

            _popup.Show(_entry, "B");
            Assert.IsFalse(store.For("B").Contains(_entry));
            Assert.AreEqual(UiStrings.AddToDifficult, _addLabel.text);
            Assert.IsTrue(_add.interactable);
        }

        [Test]
        public void DifficultMode_RemoveDeletesClosesAndNotifiesList()
        {
            _list.Add(_entry);
            var refreshed = 0;
            _popup.ShowDifficult(_entry, "s", () => refreshed++);
            Assert.IsFalse(_add.gameObject.activeSelf);
            Assert.IsTrue(_remove.gameObject.activeSelf);

            _remove.onClick.Invoke();

            Assert.IsFalse(_list.Contains(_entry));
            Assert.AreEqual(0, _list.Items.Count);
            Assert.AreEqual(1, refreshed);
            Assert.IsFalse(_popup.gameObject.activeSelf);
        }

        private void Set(string field, object value) =>
            typeof(KanjiPopup).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_popup, value);

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
