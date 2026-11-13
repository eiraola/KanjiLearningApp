using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kanji.UI;

namespace Kanji.UI.Tests
{
    public sealed class DrawingPadTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private DrawingPad _pad;
        private EventSystem _events;

        [SetUp]
        public void SetUp()
        {
            var es = new GameObject("Events");
            _created.Add(es);
            _events = es.AddComponent<EventSystem>();
            var go = new GameObject("Pad", typeof(RectTransform));
            _created.Add(go);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(400f, 200f); // centred on the origin: x -200..200, y -100..100
            go.AddComponent<RawImage>();
            _pad = go.AddComponent<DrawingPad>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        private PointerEventData At(float x, float y) => new PointerEventData(_events) { position = new Vector2(x, y) };

        [Test]
        public void Clear_StartsEmpty()
        {
            _pad.Clear();
            Assert.AreEqual(0, _pad.InkedPixels);
            Assert.IsNotNull(_pad.GetComponent<RawImage>().texture);
        }

        [Test]
        public void Tap_PaintsADot()
        {
            _pad.OnPointerDown(At(0f, 0f));
            Assert.Greater(_pad.InkedPixels, 0);
        }

        [Test]
        public void Drag_PaintsAContinuousStroke_ClearWipesIt()
        {
            _pad.OnPointerDown(At(-150f, 0f));
            int dot = _pad.InkedPixels;
            _pad.OnBeginDrag(At(-150f, 0f));
            _pad.OnDrag(At(150f, 0f));
            _pad.OnEndDrag(At(150f, 0f));
            Assert.Greater(_pad.InkedPixels, dot * 10, "the stroke joins both points");
            _pad.Clear();
            Assert.AreEqual(0, _pad.InkedPixels);
        }

        [Test]
        public void Drag_DisablesDragThreshold()
        {
            var e = At(0f, 0f);
            e.useDragThreshold = true;
            _pad.OnInitializePotentialDrag(e);
            Assert.IsFalse(e.useDragThreshold);
        }
    }
}
