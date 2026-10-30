using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Kanji.Core.Model;
using Kanji.UI;

namespace Kanji.UI.Tests
{
    public sealed class CardSwipeTests
    {
        private const float CardWidth = 1000f; // threshold = 30 % = 300
        private readonly List<GameObject> _created = new List<GameObject>();
        private readonly List<KanjiPopupResult> _results = new List<KanjiPopupResult>();
        private readonly KanjiEntry _entry = new KanjiEntry("山", "やま", "montaña", "山が高い。", "La montaña es alta.");
        private KanjiPopup _popup;
        private RectTransform _panel;
        private Image _panelImage;
        private ScrollRect _scroll;
        private Button _reveal;

        [SetUp]
        public void SetUp()
        {
            _results.Clear();
            var layer = Make("Layer", null);
            var blocker = Make("Blocker", layer.transform);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            blocker.AddComponent<CanvasGroup>();
            var service = new PopupService(blocker);

            var root = Make("Popup", layer.transform);
            root.SetActive(false); // keep Awake pending until fields are wired
            _popup = root.AddComponent<KanjiPopup>();
            _panel = Make("Panel", root.transform).GetComponent<RectTransform>();
            _panel.sizeDelta = new Vector2(CardWidth, 1200f);
            _panelImage = _panel.gameObject.AddComponent<Image>();
            var swipe = _panel.gameObject.AddComponent<CardSwipe>();

            var content = Make("Content", _panel).GetComponent<RectTransform>();
            _scroll = Make("Body", _panel).AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.content = content;
            _reveal = NewButton("Reveal", _panel);

            Set("_kanji", NewText("Kanji", content));
            Set("_sentence", NewText("Sentence", content));
            Set("_reading", NewText("Reading", content));
            Set("_meaning", NewText("Meaning", content));
            Set("_sentenceMeaning", NewText("SentenceMeaning", content));
            Set("_rowReading", Make("RowReading", content));
            Set("_rowMeaning", Make("RowMeaning", content));
            Set("_rowSentenceMeaning", Make("RowSentenceMeaning", content));
            Set("_btnKnew", NewButton("Knew", _panel));
            Set("_btnDidntKnow", NewButton("DidntKnow", _panel));
            Set("_answerRow", Make("AnswerRow", _panel));
            Set("_btnReveal", _reveal);
            Set("_body", _scroll);
            Set("_bodyLayout", Make("BodyLayout", _panel).AddComponent<LayoutElement>());
            Set("_bodyContent", content);
            Set("_swipe", swipe);
            typeof(CardSwipe).GetField("_scroll", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(swipe, _scroll);

            root.SetActive(true);
            _popup.Init(service);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        [UnityTest]
        public IEnumerator ShortDrag_ReturnsWithoutAnswering()
        {
            _popup.Show(_entry, _results.Add);
            var baseColor = _panelImage.color;

            Drag(_panel.gameObject, 150f, 0f);
            yield return Settle();

            Assert.IsEmpty(_results);
            Assert.AreEqual(0f, _panel.anchoredPosition.x, 0.01f);
            Assert.AreEqual(0f, _panel.localEulerAngles.z, 0.01f);
            Assert.AreEqual(baseColor, _panelImage.color);
            Assert.IsTrue(_popup.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator LongDragRight_CountsAsGreen()
        {
            _popup.Show(_entry, _results.Add);
            Drag(_panel.gameObject, 400f, 30f);
            yield return Settle();
            CollectionAssert.AreEqual(new[] { KanjiPopupResult.Knew }, _results);
        }

        [UnityTest]
        public IEnumerator LongDragLeft_CountsAsRed()
        {
            _popup.Show(_entry, _results.Add);
            Drag(_panel.gameObject, -400f, -30f);
            yield return Settle();
            CollectionAssert.AreEqual(new[] { KanjiPopupResult.DidntKnow }, _results);
        }

        [UnityTest]
        public IEnumerator Study_NoEffectBeforeResolve_ThenSwipeAnswersAndStaysOpen()
        {
            _popup.ShowStudy(_entry, _results.Add);
            Drag(_panel.gameObject, 400f, 0f);
            yield return Settle();
            Assert.IsEmpty(_results);
            Assert.AreEqual(0f, _panel.anchoredPosition.x, 0.01f);

            _reveal.onClick.Invoke();
            Drag(_panel.gameObject, -400f, 0f);
            yield return Settle();

            CollectionAssert.AreEqual(new[] { KanjiPopupResult.DidntKnow }, _results);
            Assert.IsTrue(_popup.gameObject.activeSelf);
            Assert.AreEqual(0f, _panel.anchoredPosition.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator OverBody_VerticalDragScrolls_HorizontalDragSwipes()
        {
            _popup.Show(_entry, _results.Add);
            var relay = _scroll.GetComponent<CardSwipeRelay>();
            Assert.IsNotNull(relay);

            Drag(_scroll.gameObject, 40f, 400f);
            yield return Settle();
            Assert.IsEmpty(_results);
            Assert.AreEqual(0f, _panel.anchoredPosition.x, 0.01f);
            Assert.IsTrue(_scroll.vertical);

            var e = Begin(_scroll.gameObject, 400f, 20f);
            Assert.IsFalse(_scroll.vertical, "body scroll frozen during a horizontal swipe");
            relay.OnEndDrag(e);
            Assert.IsTrue(_scroll.vertical);
            yield return Settle();
            CollectionAssert.AreEqual(new[] { KanjiPopupResult.Knew }, _results);
        }

        private static IEnumerator Settle() => new WaitForSecondsRealtime(0.4f);

        private static PointerEventData Begin(GameObject target, float dx, float dy)
        {
            var press = new Vector2(500f, 800f);
            var e = new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left,
                pressPosition = press,
                position = press + new Vector2(dx, dy),
            };
            ExecuteEvents.Execute(target, e, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(target, e, ExecuteEvents.dragHandler);
            return e;
        }

        private static void Drag(GameObject target, float dx, float dy)
        {
            var e = Begin(target, dx, dy);
            ExecuteEvents.Execute(target, e, ExecuteEvents.endDragHandler);
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
