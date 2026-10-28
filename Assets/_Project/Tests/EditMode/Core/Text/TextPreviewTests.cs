using Kanji.Core.Text;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class TextPreviewTests
    {
        [Test]
        public void ShortText_IsKeptWhole()
        {
            var p = TextPreview.Create("漢字", 10);
            Assert.AreEqual("漢字", p.Head);
            Assert.AreEqual(0, p.RemainingChars);
        }

        [Test]
        public void ExactLength_HasNoRemaining()
        {
            var p = TextPreview.Create("abc", 3);
            Assert.AreEqual("abc", p.Head);
            Assert.AreEqual(0, p.RemainingChars);
        }

        [Test]
        public void LongText_IsCutAndCountsRemaining()
        {
            var p = TextPreview.Create("abcdef", 4);
            Assert.AreEqual("abcd", p.Head);
            Assert.AreEqual(2, p.RemainingChars);
        }

        [Test]
        public void SurrogatePair_IsNotSplit_AndCountsAsOne()
        {
            // U+20BB7 is a surrogate pair in UTF-16.
            var p = TextPreview.Create("a\U00020BB7b\U00020BB7", 2);
            Assert.AreEqual("a\U00020BB7", p.Head);
            Assert.AreEqual(2, p.RemainingChars);
        }

        [Test]
        public void EmptyOrNull_GivesEmptyHead()
        {
            Assert.AreEqual(string.Empty, TextPreview.Create("", 5).Head);
            Assert.AreEqual(string.Empty, TextPreview.Create(null, 5).Head);
            Assert.AreEqual(0, TextPreview.Create(null, 5).RemainingChars);
        }
    }
}
