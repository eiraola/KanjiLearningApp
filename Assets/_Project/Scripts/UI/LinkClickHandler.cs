using System;
using Kanji.Core.Reading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kanji.UI
{
    /// <summary>Raises the entry index of the tapped kanji link of a TMP chunk; ignores drags and non-link taps.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _text;

        public event Action<int> EntryTapped;

        private void Awake() => _text = GetComponent<TMP_Text>();

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            var es = EventSystem.current;
            float threshold = es != null ? es.pixelDragThreshold : 10f;
            if ((e.position - e.pressPosition).sqrMagnitude > threshold * threshold) return;

            if (_text == null) _text = GetComponent<TMP_Text>();
            int link = TMP_TextUtilities.FindIntersectingLink(_text, e.position, e.pressEventCamera);
            if (link < 0 || link >= _text.textInfo.linkCount) return;
            if (StoryRichTextBuilder.TryParseLinkId(_text.textInfo.linkInfo[link].GetLinkID(), out int index))
                EntryTapped?.Invoke(index);
        }
    }
}
