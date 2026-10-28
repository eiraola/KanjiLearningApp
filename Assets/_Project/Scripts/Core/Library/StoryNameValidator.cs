using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    public enum NameCheck { Ok, Empty, Duplicate }

    public readonly struct NameResult
    {
        public NameResult(NameCheck check, string name, string conflictingName)
        {
            Check = check;
            Name = name ?? string.Empty;
            ConflictingName = conflictingName ?? string.Empty;
        }

        public NameCheck Check { get; }

        /// <summary>Trimmed name, already resolved with the fallback when the input was empty.</summary>
        public string Name { get; }

        /// <summary>Name of the existing story that clashes; empty unless Check is Duplicate.</summary>
        public string ConflictingName { get; }
    }

    public static class StoryNameValidator
    {
        /// <param name="fallbackName">File name used when input is empty; null when renaming.</param>
        /// <param name="ownId">Id of the story being renamed; null when loading.</param>
        public static NameResult Validate(string input, IReadOnlyList<StoryInfo> existing,
            string fallbackName, string ownId)
        {
            string name = (input ?? string.Empty).Trim();
            if (name.Length == 0) name = (fallbackName ?? string.Empty).Trim();
            if (name.Length == 0) return new NameResult(NameCheck.Empty, string.Empty, null);

            if (existing != null)
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    StoryInfo info = existing[i];
                    if (info.Id == ownId) continue;
                    if (StoryName.AreSame(info.Name, name))
                        return new NameResult(NameCheck.Duplicate, name, info.Name);
                }
            }
            return new NameResult(NameCheck.Ok, name, null);
        }
    }
}
