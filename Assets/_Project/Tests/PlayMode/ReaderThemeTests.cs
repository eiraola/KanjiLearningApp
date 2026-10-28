using Kanji.Core.Parsing;
using NUnit.Framework;
using UnityEngine;

namespace Kanji.UI.Tests
{
    public class ReaderThemeTests
    {
        private UiTheme _theme;

        [SetUp]
        public void SetUp() => _theme = ScriptableObject.CreateInstance<UiTheme>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_theme);

        [Test]
        public void DefaultTheme_LinkHex_IsCyan() => Assert.AreEqual("#22D3EE", _theme.LinkHex);

        [Test]
        public void ReaderBuilder_UsesThemeLinkColor()
        {
            var story = new StoryParser().Parse("t", "a {Kanji: 水; Reading: みず} b").Story;
            var chunks = ReaderScreen.CreateBuilder(_theme, 4000).Build(story);
            StringAssert.Contains("<color=#22D3EE>", chunks[0].Markup);
        }
    }
}
