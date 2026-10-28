using System;
using System.Collections.Generic;
using Kanji.Core.Model;

namespace Kanji.Core.Library
{
    public enum RepoError { None, NotFound, Corrupt, UnsupportedVersion, InvalidId, IoError, NameInvalid, NameDuplicate }

    public readonly struct RepoResult
    {
        private RepoResult(RepoError error, string message)
        {
            Error = error;
            Message = message ?? string.Empty;
        }

        public bool IsSuccess => Error == RepoError.None;
        public RepoError Error { get; }
        public string Message { get; }

        public static RepoResult Ok() => new RepoResult(RepoError.None, string.Empty);
        public static RepoResult Fail(RepoError error, string message) => new RepoResult(error, message);
    }

    public readonly struct RepoResult<T>
    {
        private RepoResult(RepoError error, string message, T value)
        {
            Error = error;
            Message = message ?? string.Empty;
            Value = value;
        }

        public bool IsSuccess => Error == RepoError.None;
        public RepoError Error { get; }
        public string Message { get; }
        public T Value { get; }

        public static RepoResult<T> Ok(T value) => new RepoResult<T>(RepoError.None, string.Empty, value);
        public static RepoResult<T> Fail(RepoError error, string message) => new RepoResult<T>(error, message, default);
    }

    public sealed class StoryInfo
    {
        public StoryInfo(string id, string name, int entryCount, DateTime importedAtUtc)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            EntryCount = entryCount;
            ImportedAtUtc = importedAtUtc;
        }

        public string Id { get; }
        public string Name { get; }
        public int EntryCount { get; }
        public DateTime ImportedAtUtc { get; }
    }

    public sealed class StoryListing
    {
        public StoryListing(IReadOnlyList<StoryInfo> items, int unreadableCount)
        {
            Items = items ?? Array.Empty<StoryInfo>();
            UnreadableCount = unreadableCount;
        }

        public IReadOnlyList<StoryInfo> Items { get; }

        /// <summary>Number of .json files skipped (corrupt, unsupported version, I/O error).</summary>
        public int UnreadableCount { get; }
    }

    public interface IStoryRepository
    {
        StoryListing List();
        RepoResult<Story> Load(string id);
        RepoResult Save(Story story);
        RepoResult Delete(string id);
    }
}
