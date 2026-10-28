using System;
using System.Collections.Generic;
using System.Text;

namespace Kanji.Core.Tests
{
    /// <summary>Simulates the TMP parser for the tags StoryRichTextBuilder emits (test helper only).</summary>
    public static class TmpVisibleText
    {
        private const string Open = "<noparse>";
        private const string Close = "</noparse>";

        public static string Strip(string markup)
        {
            var sb = new StringBuilder(markup.Length);
            bool inNoParse = false;
            int i = 0;
            while (i < markup.Length)
            {
                if (inNoParse)
                {
                    if (string.Compare(markup, i, Close, 0, Close.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        inNoParse = false;
                        i += Close.Length;
                    }
                    else sb.Append(markup[i++]);
                    continue;
                }

                if (markup[i] == '<')
                {
                    if (string.Compare(markup, i, Open, 0, Open.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        inNoParse = true;
                        i += Open.Length;
                        continue;
                    }
                    int end = markup.IndexOf('>', i);
                    if (end >= 0 && IsEmittedTag(markup, i, end)) { i = end + 1; continue; }
                }
                sb.Append(markup[i++]);
            }
            return sb.ToString();
        }

        /// <summary>Ids of the link tags found outside noparse sections.</summary>
        public static IReadOnlyList<string> LinkIds(string markup)
        {
            var ids = new List<string>();
            bool inNoParse = false;
            const string key = "<link=\"";
            int i = 0;
            while (i < markup.Length)
            {
                if (inNoParse)
                {
                    if (string.Compare(markup, i, Close, 0, Close.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        inNoParse = false;
                        i += Close.Length;
                    }
                    else i++;
                    continue;
                }
                if (string.Compare(markup, i, Open, 0, Open.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    inNoParse = true;
                    i += Open.Length;
                }
                else if (string.CompareOrdinal(markup, i, key, 0, key.Length) == 0)
                {
                    int s = i + key.Length;
                    int e = markup.IndexOf('"', s);
                    ids.Add(markup.Substring(s, e - s));
                    i = e;
                }
                else i++;
            }
            return ids;
        }

        private static bool IsEmittedTag(string m, int start, int end)
        {
            string tag = m.Substring(start, end - start + 1);
            return tag.StartsWith("<link=", StringComparison.Ordinal) || tag == "</link>"
                || tag.StartsWith("<color=", StringComparison.Ordinal) || tag == "</color>";
        }
    }
}
