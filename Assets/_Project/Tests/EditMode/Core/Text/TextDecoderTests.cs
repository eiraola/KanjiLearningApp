using System.Text;
using Kanji.Core.Text;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    public class TextDecoderTests
    {
        private static readonly byte[] SjisKanji = { 0x8A, 0xBF, 0x8E, 0x9A };

        private static TextDecoder WithSjis()
        {
            var sjis = TextDecoder.TryCreateShiftJis();
            if (sjis == null) Assert.Ignore("Shift-JIS not available in this runtime.");
            return new TextDecoder(sjis);
        }

        [Test]
        public void Utf8WithKanji_DecodesAsUtf8()
        {
            var r = new TextDecoder(null).Decode(Encoding.UTF8.GetBytes("漢字"));
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(TextEncodingKind.Utf8, r.Encoding);
            Assert.AreEqual("漢字", r.Text);
        }

        [Test]
        public void Utf8WithBom_StripsBom()
        {
            var body = Encoding.UTF8.GetBytes("漢字");
            var bytes = new byte[body.Length + 3];
            bytes[0] = 0xEF; bytes[1] = 0xBB; bytes[2] = 0xBF;
            body.CopyTo(bytes, 3);
            var r = new TextDecoder(null).Decode(bytes);
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(TextEncodingKind.Utf8Bom, r.Encoding);
            Assert.AreEqual("漢字", r.Text);
        }

        [Test]
        public void ShiftJis_DecodesKanji()
        {
            var r = WithSjis().Decode(SjisKanji);
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(TextEncodingKind.ShiftJis, r.Encoding);
            Assert.AreEqual("漢字", r.Text);
        }

        [Test]
        public void InvalidInBoth_ReturnsUnsupportedEncoding()
        {
            var r = WithSjis().Decode(new byte[] { 0xFF, 0xFE, 0x81, 0x20 });
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(DecodeError.UnsupportedEncoding, r.Error);
        }

        [Test]
        public void ShiftJisBytesWithoutSjisSupport_ReturnsUnsupportedEncoding()
        {
            var r = new TextDecoder(null).Decode(SjisKanji);
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(DecodeError.UnsupportedEncoding, r.Error);
        }

        [Test]
        public void OverMaxBytes_ReturnsTooLarge()
        {
            var r = new TextDecoder(null).Decode(new byte[TextDecoder.MaxBytes + 1]);
            Assert.AreEqual(DecodeError.TooLarge, r.Error);
        }

        [Test]
        public void ExactlyMaxBytes_IsOk()
        {
            var bytes = new byte[TextDecoder.MaxBytes];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)'a';
            var r = new TextDecoder(null).Decode(bytes);
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(TextDecoder.MaxBytes, r.Text.Length);
        }

        [Test]
        public void Empty_IsOkUtf8()
        {
            var r = new TextDecoder(null).Decode(new byte[0]);
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual("", r.Text);
            Assert.AreEqual(TextEncodingKind.Utf8, r.Encoding);
        }

        [Test]
        public void Null_ReturnsUnsupportedEncoding()
        {
            Assert.AreEqual(DecodeError.UnsupportedEncoding, new TextDecoder(null).Decode(null).Error);
        }
    }
}
