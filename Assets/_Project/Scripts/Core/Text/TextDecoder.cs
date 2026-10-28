using System;
using System.Text;

namespace Kanji.Core.Text
{
    /// <summary>Decodes raw file bytes as UTF-8 (with or without BOM) or Shift-JIS. Never throws.</summary>
    public sealed class TextDecoder
    {
        public const long MaxBytes = 1024 * 1024;

        private const int ShiftJisCodePage = 932;
        private const char ReplacementChar = '�';

        private static bool s_providerRegistered;

        private readonly Encoding _shiftJis;

        public TextDecoder(Encoding shiftJis)
        {
            _shiftJis = shiftJis;
        }

        public bool SupportsShiftJis => _shiftJis != null;

        /// <summary>Returns a strict Shift-JIS encoding, or null if the platform lacks code page 932.</summary>
        public static Encoding TryCreateShiftJis()
        {
            RegisterCodePagesProvider();
            try
            {
                return Encoding.GetEncoding(ShiftJisCodePage,
                    EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
        }

        public DecodeResult Decode(byte[] bytes)
        {
            if (bytes == null) return DecodeResult.Fail(DecodeError.UnsupportedEncoding);
            if (bytes.Length > MaxBytes) return DecodeResult.Fail(DecodeError.TooLarge);
            if (bytes.Length == 0) return DecodeResult.Ok(string.Empty, TextEncodingKind.Utf8);

            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            int offset = hasBom ? 3 : 0;

            if (TryDecode(new UTF8Encoding(false, true), bytes, offset, out string text))
                return DecodeResult.Ok(text, hasBom ? TextEncodingKind.Utf8Bom : TextEncodingKind.Utf8);

            if (!hasBom && _shiftJis != null && TryDecode(_shiftJis, bytes, 0, out text)
                && IsLossless(_shiftJis, text, bytes))
                return DecodeResult.Ok(text, TextEncodingKind.ShiftJis);

            return DecodeResult.Fail(DecodeError.UnsupportedEncoding);
        }

        // Some runtimes (Mono I18N) ignore the exception fallback, so verify by re-encoding to the same bytes.
        private static bool IsLossless(Encoding encoding, string text, byte[] original)
        {
            if (text.IndexOf(ReplacementChar) >= 0) return false;
            try
            {
                byte[] again = encoding.GetBytes(text);
                if (again.Length != original.Length) return false;
                for (int i = 0; i < again.Length; i++)
                    if (again[i] != original[i]) return false;
                return true;
            }
            catch (EncoderFallbackException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool TryDecode(Encoding encoding, byte[] bytes, int offset, out string text)
        {
            try
            {
                text = encoding.GetString(bytes, offset, bytes.Length - offset);
                return true;
            }
            catch (DecoderFallbackException) { }
            catch (ArgumentException) { }
            text = null;
            return false;
        }

        // CodePagesEncodingProvider is not in netstandard 2.1 at compile time; register it by reflection if present.
        private static void RegisterCodePagesProvider()
        {
            if (s_providerRegistered) return;
            s_providerRegistered = true;
            try
            {
                var type = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                var instance = type?.GetProperty("Instance")?.GetValue(null) as EncodingProvider;
                if (instance != null) Encoding.RegisterProvider(instance);
            }
            catch (Exception) { /* provider unavailable: rely on built-in code pages */ }
        }
    }
}
