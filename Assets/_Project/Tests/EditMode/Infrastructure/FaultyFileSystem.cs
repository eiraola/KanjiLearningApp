using System.Collections.Generic;
using System.IO;
using Kanji.Infrastructure.Storage;

namespace Kanji.Infrastructure.Tests
{
    /// <summary>DiskFileSystem decorator that throws IOException on demand.</summary>
    public sealed class FaultyFileSystem : IFileSystem
    {
        private readonly DiskFileSystem _inner = new DiskFileSystem();

        public bool FailOnWrite { get; set; }
        public bool FailOnReplace { get; set; }
        public bool FailOnDelete { get; set; }

        public bool FileExists(string path) => _inner.FileExists(path);
        public byte[] ReadAllBytes(string path) => _inner.ReadAllBytes(path);
        public void CreateDirectory(string path) => _inner.CreateDirectory(path);
        public IEnumerable<string> ListFiles(string directory, string extension) => _inner.ListFiles(directory, extension);

        public void WriteAllBytes(string path, byte[] data)
        {
            if (FailOnWrite) throw new IOException("Simulated write failure.");
            _inner.WriteAllBytes(path, data);
        }

        public void Replace(string tempPath, string destPath)
        {
            if (FailOnReplace) throw new IOException("Simulated replace failure.");
            _inner.Replace(tempPath, destPath);
        }

        public void Delete(string path)
        {
            if (FailOnDelete) throw new IOException("Simulated delete failure.");
            _inner.Delete(path);
        }
    }
}
