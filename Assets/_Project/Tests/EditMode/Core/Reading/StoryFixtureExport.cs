using System.IO;
using System.Text;
using NUnit.Framework;

namespace Kanji.Core.Tests
{
    /// <summary>Writes the files used by the on-device test (S05 T24). Run manually.</summary>
    public class StoryFixtureExport
    {
        [Test, Explicit("Writes Assets/_Project/Tests/Fixtures/Text/perf-50k.txt for the phone test")]
        public void ExportPerfFixture()
        {
            // The Editor's working directory is the project root.
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_Project", "Tests", "Fixtures", "Text", "perf-50k.txt");
            File.WriteAllText(path, StoryFixtureGenerator.Generate().Text, new UTF8Encoding(false));
            Assert.IsTrue(File.Exists(path));
        }
    }
}
