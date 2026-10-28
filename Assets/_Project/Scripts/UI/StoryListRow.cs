using System;
using Kanji.Core.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>One row of the library list. Thin view: name, kanji count, tap.</summary>
    public sealed class StoryListRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _count;

        private StoryInfo _info;
        private Action<StoryInfo> _onClick;

        private void Awake() => _button.onClick.AddListener(HandleClick);

        public void Bind(StoryInfo info, Action<StoryInfo> onClick)
        {
            _info = info;
            _onClick = onClick;
            _name.text = info.Name;
            _count.text = UiStrings.KanjiCount(info.EntryCount);
        }

        /// <summary>Same row with a custom subtitle (e.g. "N complicados").</summary>
        public void Bind(StoryInfo info, string subtitle, Action<StoryInfo> onClick)
        {
            Bind(info, onClick);
            _count.text = subtitle;
        }

        private void HandleClick() => _onClick?.Invoke(_info);
    }
}
