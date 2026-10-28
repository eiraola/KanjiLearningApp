using System.Collections;
using System.Collections.Generic;
using Kanji.Core.Model;
using Kanji.Core.Reading;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kanji.UI.Tests
{
    /// <summary>Checks the noparse escape rule of StoryRichTextBuilder against the real TMP parser.</summary>
    public class RichTextTmpEscapeTests
    {
        private GameObject _canvasGo;
        private TextMeshProUGUI _text;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas", typeof(Canvas));
            _canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_canvasGo.transform, false);
            _text = textGo.GetComponent<TextMeshProUGUI>();
            _text.parseCtrlCharacters = false;
            _text.richText = true;
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_canvasGo);

        private static readonly string[] Hostile =
        {
            "<b>x</b>", "</noparse>", "</NoParse>", "<noparse>", "<link=\"k0\">", "<<", "final<",
            "a\\nb \\u0041 \\t", "\\U0001F600 \\\\u0041 \\u", "x\\", "1 < 2 > 1 <color=#FF0000>red</color>", "<size=50>big</size> <br> <sprite=0>",
        };

        [UnityTest]
        public IEnumerator HostileStrings_AreShownLiterally_AndOnlyRealLinksCount()
        {
            foreach (string hostile in Hostile)
            {
                var segments = new List<Segment>
                {
                    Segment.Plain("ab" + hostile + "cd"),
                    Segment.ForKanji("猫", 0),
                    Segment.Plain(hostile),
                };
                var entries = new List<KanjiEntry> { new KanjiEntry("猫", "r", "m", "s", "sm") };
                var story = new Story("id", "n", segments, entries);
                var chunks = new StoryRichTextBuilder("#1E6FFF").Build(story);
                Assert.AreEqual(1, chunks.Count);

                _text.text = chunks[0].Markup;
                _text.ForceMeshUpdate();
                yield return null;

                Assert.AreEqual("ab" + hostile + "cd猫" + hostile, _text.GetParsedText(), "hostile: " + hostile);
                Assert.AreEqual(1, _text.textInfo.linkCount, "links for: " + hostile);
                Assert.AreEqual("k0", _text.textInfo.linkInfo[0].GetLinkID(), "link id for: " + hostile);
            }
        }
    }
}
