using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Kanji.Core.Navigation;
using Kanji.UI;

namespace Kanji.UI.Tests
{
    public sealed class PopupServiceTests
    {
        private sealed class TestPopup : PopupView
        {
            public override void Cancel() { }
        }

        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        private GameObject Make(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _created.Add(go);
            return go;
        }

        [Test]
        public void TwoPopups_BlockerSitsBetweenLowerAndTop()
        {
            var layer = Make("Layer", null);
            var a = Make("A", layer.transform).AddComponent<TestPopup>();
            var blocker = Make("Blocker", layer.transform);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            var c = Make("C", layer.transform).AddComponent<TestPopup>();
            var service = new PopupService(blocker);
            a.Init(service);
            c.Init(service);

            service.Open(a);
            service.Open(c);

            Assert.Less(a.transform.GetSiblingIndex(), blocker.transform.GetSiblingIndex());
            Assert.Less(blocker.transform.GetSiblingIndex(), c.transform.GetSiblingIndex());

            service.Close(c);
            // A becomes the top popup again: scrim must sit below it.
            Assert.Less(blocker.transform.GetSiblingIndex(), a.transform.GetSiblingIndex());
        }

        private (PopupService service, GameObject blocker, TestPopup a, TestPopup c) Setup(InputGate gate = null, float fade = 0f)
        {
            var layer = Make("Layer", null);
            var a = Make("A", layer.transform).AddComponent<TestPopup>();
            var blocker = Make("Blocker", layer.transform);
            blocker.AddComponent<Image>();
            blocker.AddComponent<Button>();
            blocker.AddComponent<CanvasGroup>();
            var c = Make("C", layer.transform).AddComponent<TestPopup>();
            var service = new PopupService(blocker, null, gate, fade);
            a.Init(service);
            c.Init(service);
            return (service, blocker, a, c);
        }

        [Test]
        public void Stacked_LowerIsHiddenAndNotInteractable()
        {
            var (service, _, a, c) = Setup();
            service.Open(a);
            service.Open(c);
            var group = a.GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, group.alpha);
            Assert.IsFalse(group.blocksRaycasts);
            Assert.IsTrue(a.gameObject.activeSelf);
        }

        [Test]
        public void CloseTop_LowerReappearsIntact()
        {
            var (service, blocker, a, c) = Setup();
            service.Open(a);
            service.Open(c);
            service.Close(c);
            var group = a.GetComponent<CanvasGroup>();
            Assert.AreEqual(1f, group.alpha);
            Assert.IsTrue(group.blocksRaycasts);
            Assert.IsTrue(group.interactable);
            Assert.IsFalse(c.gameObject.activeSelf);
            Assert.IsTrue(blocker.activeSelf);
        }

        [Test]
        public void CloseLast_HidesBlocker()
        {
            var (service, blocker, a, _) = Setup();
            service.Open(a);
            Assert.IsTrue(blocker.activeSelf);
            service.Close(a);
            Assert.IsFalse(blocker.activeSelf);
            Assert.IsFalse(a.gameObject.activeSelf);
            Assert.IsFalse(service.HasOpen);
        }

        [Test]
        public void OpenAndClose_BlockInput()
        {
            double t = 0;
            var gate = new InputGate(() => t);
            var (service, _, a, _) = Setup(gate, 0.15f);
            service.Open(a);
            Assert.IsTrue(gate.IsBlocked);
            t = 0.2;
            Assert.IsFalse(gate.IsBlocked);
            service.Close(a);
            Assert.IsTrue(gate.IsBlocked);
        }
    }
}
