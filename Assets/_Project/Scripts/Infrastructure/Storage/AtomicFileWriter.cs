using System;
using System.Diagnostics;
using System.IO;
using Kanji.Core.Library;

namespace Kanji.Infrastructure.Storage
{
    /// <summary>[Kanji][IO] diagnostics; calls are stripped from release builds.</summary>
    public static class IoLog
    {
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Info(string message) => UnityEngine.Debug.Log("[Kanji][IO] " + message);

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Warn(string message) => UnityEngine.Debug.LogWarning("[Kanji][IO] " + message);
    }

    /// <summary>Writes to "path.tmp" and swaps it in, so the destination is never left half-written.</summary>
    public sealed class AtomicFileWriter
    {
        private readonly IFileSystem _fs;

        public AtomicFileWriter(IFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public RepoResult Write(string path, byte[] data)
        {
            string tmp = path + ".tmp";
            bool replaceStarted = false;
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) _fs.CreateDirectory(dir);
                _fs.WriteAllBytes(tmp, data);
                replaceStarted = true;
                _fs.Replace(tmp, path);
                IoLog.Info("write ok path=" + path + " bytes=" + data.Length + " ms=" + ElapsedMs(t0));
                return RepoResult.Ok();
            }
            catch (Exception e)
            {
                // If Replace failed, dest may be damaged and the .tmp is the only intact copy: keep it.
                bool deleted = !replaceStarted && TryDelete(tmp);
                IoLog.Warn("write FAILED path=" + path + " bytes=" + (data?.Length ?? -1) + " ms=" + ElapsedMs(t0) +
                           " exception=" + e.GetType().FullName + " msg=" + e.Message + " tmpDeleted=" + deleted);
                return RepoResult.Fail(RepoError.IoError, e.Message);
            }
        }

        private bool TryDelete(string path)
        {
            try { _fs.Delete(path); return true; }
            catch (Exception) { return false; /* best effort: an orphan .tmp is overwritten by the next write */ }
        }

        private static long ElapsedMs(long startTimestamp) =>
            (Stopwatch.GetTimestamp() - startTimestamp) * 1000 / Stopwatch.Frequency;
    }
}
