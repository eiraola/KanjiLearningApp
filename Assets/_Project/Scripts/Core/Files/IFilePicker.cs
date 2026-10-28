using System;

namespace Kanji.Core.Files
{
    public enum PickStatus { Picked, Cancelled, Error }

    public enum PickError { None, Busy, TooLarge, Unreadable }

    public readonly struct PickResult
    {
        public PickStatus Status { get; }
        public PickError Error { get; }
        /// <summary>File name without extension.</summary>
        public string FileName { get; }
        /// <summary>Size in bytes; also set for TooLarge.</summary>
        public long SizeBytes { get; }
        /// <summary>Null unless Status is Picked.</summary>
        public byte[] Bytes { get; }

        private PickResult(PickStatus status, PickError error, string fileName, long sizeBytes, byte[] bytes)
        {
            Status = status;
            Error = error;
            FileName = fileName;
            SizeBytes = sizeBytes;
            Bytes = bytes;
        }

        public static PickResult Picked(string name, byte[] bytes) =>
            new PickResult(PickStatus.Picked, PickError.None, name, bytes?.LongLength ?? 0, bytes);

        public static PickResult Cancelled() =>
            new PickResult(PickStatus.Cancelled, PickError.None, null, 0, null);

        public static PickResult Failed(PickError error, string name = null, long size = 0) =>
            new PickResult(PickStatus.Error, error, name, size, null);
    }

    /// <summary>Port for choosing a text file. Always invokes the callback exactly once, on the main thread.</summary>
    public interface IFilePicker
    {
        void PickTextFile(Action<PickResult> onCompleted);
    }
}
