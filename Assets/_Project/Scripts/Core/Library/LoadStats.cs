using Kanji.Core.Model;
using Kanji.Core.Parsing;

namespace Kanji.Core.Library
{
    /// <summary>Statistics shown in the load popup: kanji blocks, visible characters and parse warnings.</summary>
    public readonly struct LoadStats
    {
        public LoadStats(int kanjis, int characters, int warnings)
        {
            Kanjis = kanjis;
            Characters = characters;
            Warnings = warnings;
        }

        public int Kanjis { get; }
        public int Characters { get; }
        public int Warnings { get; }

        public static LoadStats From(Story story, ParseReport report)
        {
            int warnings = report?.Warnings.Count ?? 0;
            if (story == null) return new LoadStats(0, 0, warnings);
            return new LoadStats(story.Entries.Count, CountCharacters(story), warnings);
        }

        private static int CountCharacters(Story story)
        {
            int total = 0;
            var segments = story.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                string text = segments[i].Text;
                for (int c = 0; c < text.Length; c++)
                {
                    char ch = text[c];
                    if (char.IsLowSurrogate(ch)) continue; // a surrogate pair counts once (on its high half)
                    if (!char.IsWhiteSpace(ch)) total++;
                }
            }
            return total;
        }
    }
}
