namespace Kanji.Core.Text
{
    public enum TextEncodingKind { Utf8, Utf8Bom, ShiftJis }

    public enum DecodeError { None, TooLarge, UnsupportedEncoding }

    public readonly struct DecodeResult
    {
        private DecodeResult(bool ok, DecodeError error, TextEncodingKind encoding, string text)
        {
            IsSuccess = ok;
            Error = error;
            Encoding = encoding;
            Text = text;
        }

        public bool IsSuccess { get; }
        public DecodeError Error { get; }
        public TextEncodingKind Encoding { get; }
        public string Text { get; }

        public static DecodeResult Ok(string text, TextEncodingKind enc) =>
            new DecodeResult(true, DecodeError.None, enc, text);

        public static DecodeResult Fail(DecodeError e) =>
            new DecodeResult(false, e, TextEncodingKind.Utf8, null);
    }
}
