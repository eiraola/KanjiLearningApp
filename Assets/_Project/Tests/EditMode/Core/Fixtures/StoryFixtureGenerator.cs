using System.Collections.Generic;
using System.Text;

namespace Kanji.Core.Tests
{
    public sealed class GeneratedFixture
    {
        public string Text;
        /// <summary>Distinct kanjis in order of first appearance.</summary>
        public IReadOnlyList<string> ExpectedKanji;
        public int BlockCount;
    }

    /// <summary>Deterministic story generator (own xorshift32, no System.Random).</summary>
    public static class StoryFixtureGenerator
    {
        private static readonly string[] Fillers =
        {
            "これはてすとのぶんです.",
            "あしたはがっこうにいきます.\n",
            "Time 10:30 at the station.\r\n",
            "　きょうはいいてんきですね.　",
            "ねこがすきです. Ver 1.2 ok.\n",
            "ほんをよみます.\r\n",
            "Hello world. Next 12:45.",
            "みずをのみます.　\n",
        };

        private struct XorShift32
        {
            private uint _s;
            public XorShift32(int seed) { _s = seed == 0 ? 2463534242u : (uint)seed; }
            public int Next(int max)
            {
                _s ^= _s << 13;
                _s ^= _s >> 17;
                _s ^= _s << 5;
                return (int)(_s % (uint)max);
            }
        }

        public static GeneratedFixture Generate(int seed = 12345, int minChars = 50000, int kanjiCount = 200)
        {
            var rng = new XorShift32(seed);
            var kanjis = new List<string>(kanjiCount);
            for (int i = 0; i < kanjiCount; i++) kanjis.Add(((char)(0x4E00 + i * 37)).ToString());

            int repeats = kanjiCount / 4;
            int totalBlocks = kanjiCount + repeats;
            var order = new List<string>(totalBlocks);
            // Repeats are only placed after at least one first appearance, keeping first-appearance order.
            for (int i = 0; i < kanjiCount; i++)
            {
                order.Add(kanjis[i]);
                if (i >= 3 && (i % 4) == 3) order.Add(kanjis[rng.Next(i)]);
            }

            var blocks = new List<string>(order.Count);
            int blocksLength = 0;
            var tmp = new StringBuilder();
            for (int b = 0; b < order.Count; b++)
            {
                tmp.Clear();
                AppendBlock(tmp, order[b], b);
                blocks.Add(tmp.ToString());
                blocksLength += tmp.Length;
            }

            var sb = new StringBuilder(minChars + 600);
            int fillerTotal = System.Math.Max(0, minChars - blocksLength);
            int blocksSoFar = 0;
            for (int b = 0; b < blocks.Count; b++)
            {
                // Absolute target per gap so the per-sentence overshoot does not accumulate.
                int target = blocksSoFar + fillerTotal * (b + 1) / (blocks.Count + 1);
                AppendFiller(sb, rng, target - sb.Length);
                sb.Append(blocks[b]);
                blocksSoFar += blocks[b].Length;
            }
            AppendFiller(sb, rng, minChars - sb.Length);

            return new GeneratedFixture { Text = sb.ToString(), ExpectedKanji = kanjis, BlockCount = order.Count };
        }

        private static void AppendFiller(StringBuilder sb, XorShift32 rng, int targetLength)
        {
            int end = sb.Length + targetLength;
            while (sb.Length < end) sb.Append(Fillers[rng.Next(Fillers.Length)]);
        }

        private static void AppendBlock(StringBuilder sb, string kanji, int n)
        {
            sb.Append("{Kanji: ").Append(kanji)
              .Append("; Reading: よみ").Append(n)
              .Append("; Meaning: 10:30\n  meaning ").Append(n)
              .Append("; Sentence: ").Append(kanji).Append("をみる")
              .Append("; SentenceMeaning: see it}");
        }
    }
}
