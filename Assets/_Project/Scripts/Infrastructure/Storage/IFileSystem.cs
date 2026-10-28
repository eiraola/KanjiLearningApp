using System;
using System.Collections.Generic;
using System.IO;

namespace Kanji.Infrastructure.Storage
{
    /// <summary>Disk access seam so failures can be simulated in tests. Members may throw IO exceptions.</summary>
    public interface IFileSystem
    {
        bool FileExists(string path);
        byte[] ReadAllBytes(string path);
        void WriteAllBytes(string path, byte[] data);
        void Replace(string tempPath, string destPath);
        void Delete(string path);
        void CreateDirectory(string path);
        IEnumerable<string> ListFiles(string directory, string extension);
    }

    public sealed class DiskFileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public void WriteAllBytes(string path, byte[] data) => File.WriteAllBytes(path, data);

        public void Replace(string tempPath, string destPath)
        {
            if (!File.Exists(destPath))
            {
                File.Move(tempPath, destPath);
                IoLog.Info("replace branch=move dest=" + destPath);
                return;
            }

            try
            {
                File.Replace(tempPath, destPath, null);
                IoLog.Info("replace branch=replace dest=" + destPath);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is NotImplementedException)
            {
                IoLog.Warn("replace branch=fallback-copy dest=" + destPath + " exception=" + e.GetType().FullName + " msg=" + e.Message);
                // Some platforms do not support File.Replace; fall back to copy over the destination.
                File.Copy(tempPath, destPath, true);
                Delete(tempPath);
            }
        }

        public void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public IEnumerable<string> ListFiles(string directory, string extension)
        {
            if (!Directory.Exists(directory)) return Array.Empty<string>();
            return Directory.GetFiles(directory, "*" + extension);
        }
    }
}
