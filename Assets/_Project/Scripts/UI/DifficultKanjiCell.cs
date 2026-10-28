using System;
using Kanji.Core.Library;
using Kanji.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>One row of the difficult-kanji list: the kanji and a progress circle (study successes / 5); tap opens the popup.</summary>
    public sealed class DifficultKanjiCell : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _kanji;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Color _progressColor = new Color(0.133f, 0.827f, 0.933f, 1f);
        [SerializeField] private Color _fullColor = new Color(0.482f, 0.847f, 0.561f, 1f);

        private KanjiEntry _entry;
        private Action<KanjiEntry> _onClick;

        internal float ProgressFill => _progressFill != null ? _progressFill.fillAmount : 0f;

        private void Awake() => _button.onClick.AddListener(HandleClick);

        public void Bind(KanjiEntry entry, int progress, Action<KanjiEntry> onClick)
        {
            _entry = entry;
            _onClick = onClick;
            _kanji.text = entry.Kanji;
            if (_progressFill == null) return;
            int max = DifficultKanjiList.MaxProgress;
            _progressFill.fillAmount = Mathf.Clamp(progress, 0, max) / (float)max;
            _progressFill.color = progress >= max ? _fullColor : _progressColor;
        }

        private void HandleClick() => _onClick?.Invoke(_entry);
    }
}
