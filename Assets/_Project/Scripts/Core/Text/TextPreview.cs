namespace Kanji.Core.Text
{
    /// <summary>First N code points of a text plus how many remain. Never splits a surrogate pair.</summary>
    public readonly struct TextPreview
    {
        public string Head { get; }
        /// <summary>Remaining characters, counted in code points.</summary>
        public int RemainingChars { get; }

        private TextPreview(string head, int remaining)
        {
            Head = head;
            RemainingChars = remaining;
        }

        public static TextPreview Create(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return new TextPreview(string.Empty, 0);
            if (maxChars < 0) maxChars = 0;

            int index = 0;
            int count = 0;
            while (index < text.Length && count < maxChars)
            {
                index += IsPair(text, index) ? 2 : 1;
                count++;
            }
            if (index >= text.Length) return new TextPreview(text, 0);

            int remaining = 0;
            for (int i = index; i < text.Length; i += IsPair(text, i) ? 2 : 1) remaining++;
            return new TextPreview(text.Substring(0, index), remaining);
        }

        private static bool IsPair(string s, int i) =>
            char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]);
    }
}
