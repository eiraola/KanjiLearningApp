using System;
using System.Collections.Generic;
using NUnit.Framework;
using Kanji.UI;

namespace Kanji.UI.Tests
{
    public sealed class ScreenNavigatorTransitionTests
    {
        private sealed class FakeScreen : IScreen
        {
            private readonly string _name;
            private readonly List<string> _log;
            public FakeScreen(ScreenId id, string name, List<string> log) { Id = id; _name = name; _log = log; }
            public ScreenId Id { get; }
            public void OnShow(object args) => _log.Add(_name + ".show");
            public void OnHide() => _log.Add(_name + ".hide");
            public bool HandleBack() => false;
        }

        private sealed class FakeTransition : IScreenTransition
        {
            public readonly List<Action> Swaps = new List<Action>();
            public void Run(Action swap) => Swaps.Add(swap);
        }

        private List<string> _log;
        private FakeTransition _transition;
        private ScreenNavigator _nav;

        private Dictionary<ScreenId, IScreen> MakeScreens() => new Dictionary<ScreenId, IScreen>
        {
            [ScreenId.Home] = new FakeScreen(ScreenId.Home, "A", _log),
            [ScreenId.Reader] = new FakeScreen(ScreenId.Reader, "B", _log),
        };

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _transition = new FakeTransition();
            _nav = new ScreenNavigator(MakeScreens(), _transition);
        }

        [Test]
        public void FirstPush_IsImmediate()
        {
            _nav.Push(ScreenId.Home);
            CollectionAssert.AreEqual(new[] { "A.show" }, _log);
            Assert.AreEqual(0, _transition.Swaps.Count);
        }

        [Test]
        public void Push_ChangesStackNowButDefersView()
        {
            _nav.Push(ScreenId.Home);
            _log.Clear();
            _nav.Push(ScreenId.Reader);
            Assert.AreEqual(ScreenId.Reader, _nav.Current.Id);
            Assert.IsTrue(_nav.CanGoBack);
            CollectionAssert.IsEmpty(_log);
            _transition.Swaps[0]();
            CollectionAssert.AreEqual(new[] { "A.hide", "B.show" }, _log);
        }

        [Test]
        public void ConsecutiveRequests_ApplyInOrder()
        {
            _nav.Push(ScreenId.Home);
            _log.Clear();
            _nav.Push(ScreenId.Reader);
            Assert.IsTrue(_nav.Pop());
            Assert.AreEqual(2, _transition.Swaps.Count);
            foreach (var s in _transition.Swaps) s();
            CollectionAssert.AreEqual(new[] { "A.hide", "B.show", "B.hide", "A.show" }, _log);
        }

        [Test]
        public void WithoutTransition_IsImmediate()
        {
            var nav = new ScreenNavigator(MakeScreens());
            nav.Push(ScreenId.Home);
            nav.Push(ScreenId.Reader);
            CollectionAssert.AreEqual(new[] { "A.show", "A.hide", "B.show" }, _log);
        }
    }
}
