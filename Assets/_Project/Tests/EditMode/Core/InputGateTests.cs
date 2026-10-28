using Kanji.Core.Navigation;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public sealed class InputGateTests
    {
        private double _t;
        private InputGate _gate;

        [SetUp]
        public void SetUp()
        {
            _t = 10;
            _gate = new InputGate(() => _t);
        }

        [Test]
        public void StartsFree() => Assert.IsFalse(_gate.IsBlocked);

        [Test]
        public void BlockFor_BlocksUntilDeadline()
        {
            _gate.BlockFor(0.15);
            Assert.IsTrue(_gate.IsBlocked);
            _t += 0.14;
            Assert.IsTrue(_gate.IsBlocked);
            _t += 0.02;
            Assert.IsFalse(_gate.IsBlocked);
        }

        [Test]
        public void ShorterBlockDoesNotShortenExisting()
        {
            _gate.BlockFor(1.0);
            _gate.BlockFor(0.1);
            _t += 0.5;
            Assert.IsTrue(_gate.IsBlocked);
        }

        [Test]
        public void Hold_BlocksUntilRelease()
        {
            _gate.Hold();
            _t += 100;
            Assert.IsTrue(_gate.IsBlocked);
            _gate.Release();
            Assert.IsFalse(_gate.IsBlocked);
        }

        [Test]
        public void NestedHolds_NeedMatchingReleases()
        {
            _gate.Hold();
            _gate.Hold();
            _gate.Release();
            Assert.IsTrue(_gate.IsBlocked);
            _gate.Release();
            Assert.IsFalse(_gate.IsBlocked);
        }

        [Test]
        public void Blocked_RaisedOnlyOnFreeToBlockedEdge()
        {
            int count = 0;
            _gate.Blocked += () => count++;
            _gate.BlockFor(1);
            _gate.Hold();
            _gate.BlockFor(2);
            Assert.AreEqual(1, count);
            _gate.Release();
            _t += 5;
            _gate.Hold();
            Assert.AreEqual(2, count);
        }
    }
}
