using System;
using System.IO;
using System.Text;
using Kanji.Core.Library;
using Kanji.Infrastructure.Storage;
using NUnit.Framework;

namespace Kanji.Infrastructure.Tests
{
    public class AtomicFileWriterTests
    {
        private string _dir;
        private FaultyFileSystem _fs;
        private AtomicFileWriter _writer;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _fs = new FaultyFileSystem();
            _writer = new AtomicFileWriter(_fs);
            _path = Path.Combine(_dir, "a.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);

        [Test]
        public void Write_CreatesFile()
        {
            Assert.IsTrue(_writer.Write(_path, B("one")).IsSuccess);
            Assert.AreEqual("one", File.ReadAllText(_path));
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void Write_Overwrites()
        {
            _writer.Write(_path, B("one"));
            Assert.IsTrue(_writer.Write(_path, B("two")).IsSuccess);
            Assert.AreEqual("two", File.ReadAllText(_path));
        }

        [Test]
        public void Write_CreatesMissingDirectory()
        {
            string deep = Path.Combine(_dir, "x", "y", "a.json");
            Assert.IsTrue(_writer.Write(deep, B("v")).IsSuccess);
            Assert.IsTrue(File.Exists(deep));
        }

        [Test]
        public void WriteFails_OnTempWrite_LeavesOldFileIntact()
        {
            _writer.Write(_path, B("old"));
            _fs.FailOnWrite = true;
            RepoResult r = _writer.Write(_path, B("new"));
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual("old", File.ReadAllText(_path));
        }

        [Test]
        public void ReplaceFails_LeavesOldFileIntact_AndReturnsIoError()
        {
            _writer.Write(_path, B("old"));
            _fs.FailOnReplace = true;
            RepoResult r = _writer.Write(_path, B("new"));
            Assert.AreEqual(RepoError.IoError, r.Error);
            Assert.AreEqual("old", File.ReadAllText(_path));
        }

        [Test]
        public void ReplaceFails_KeepsTemp_AsOnlyIntactCopy()
        {
            _writer.Write(_path, B("old"));
            _fs.FailOnReplace = true;
            _writer.Write(_path, B("new"));
            Assert.IsTrue(File.Exists(_path + ".tmp"));
            Assert.AreEqual("new", File.ReadAllText(_path + ".tmp"));
        }

        [Test]
        public void TempWriteFails_RemovesTemp()
        {
            _writer.Write(_path, B("old"));
            _fs.FailOnWrite = true;
            _writer.Write(_path, B("new"));
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void NullData_ReturnsIoError_NoThrow()
        {
            RepoResult r = default;
            Assert.DoesNotThrow(() => r = _writer.Write(_path, null));
            Assert.AreEqual(RepoError.IoError, r.Error);
        }

        [Test]
        public void Write_NeverThrows()
        {
            _fs.FailOnWrite = true;
            _fs.FailOnReplace = true;
            _fs.FailOnDelete = true;
            Assert.DoesNotThrow(() => _writer.Write(_path, B("x")));
        }
    }
}
