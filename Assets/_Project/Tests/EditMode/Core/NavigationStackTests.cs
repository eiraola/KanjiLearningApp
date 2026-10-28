using System;
using Kanji.Core.Navigation;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class NavigationStackTests
    {
        [Test]
        public void Push_MakesItemCurrent()
        {
            var s = new NavigationStack<string>();
            s.Push("A");
            s.Push("B");
            Assert.AreEqual("B", s.Current);
            Assert.AreEqual(2, s.Count);
        }

        [Test]
        public void TryPop_ReturnsToPrevious()
        {
            var s = new NavigationStack<string>();
            s.Push("A");
            s.Push("B");
            Assert.IsTrue(s.TryPop(out var popped));
            Assert.AreEqual("B", popped);
            Assert.AreEqual("A", s.Current);
        }

        [Test]
        public void TryPop_OnRoot_ReturnsFalseAndKeepsRoot()
        {
            var s = new NavigationStack<string>();
            s.Push("A");
            Assert.IsFalse(s.CanPop);
            Assert.IsFalse(s.TryPop(out _));
            Assert.AreEqual("A", s.Current);
        }

        [Test]
        public void Replace_KeepsCount()
        {
            var s = new NavigationStack<string>();
            s.Push("A");
            s.Push("B");
            s.Replace("C");
            Assert.AreEqual(2, s.Count);
            Assert.AreEqual("C", s.Current);
        }

        [Test]
        public void Current_OnEmpty_Throws()
        {
            var s = new NavigationStack<string>();
            Assert.Throws<InvalidOperationException>(() => { var _ = s.Current; });
        }
    }
}
