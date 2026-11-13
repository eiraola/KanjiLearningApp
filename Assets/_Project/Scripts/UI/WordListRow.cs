using System;
using Kanji.Core.Words;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Row of a word list: word, reading, difficult mark and two progress circles (study / reverse study, successes of 5).</summary>
    public sealed class WordListRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _word;
        [SerializeField] private TMP_Text _reading;
        [SerializeField] private GameObject _difficultMark;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Image _reverseFill;
        [SerializeField] private Color _progressColor = new Color(0.133f, 0.827f, 0.933f, 1f);
        [SerializeField] private Color _reverseColor = new Color(0.761f, 0.6f, 0.988f, 1f);
        [SerializeField] private Color _fullColor = new Color(0.482f, 0.847f, 0.561f, 1f);

        private Action _onClick;

        internal float ProgressFill => _progressFill != null ? _progressFill.fillAmount : 0f;
        internal float ReverseFill => _reverseFill != null ? _reverseFill.fillAmount : 0f;
        internal bool DifficultShown => _difficultMark != null && _difficultMark.activeSelf;

        private void Awake() => _button.onClick.AddListener(() => _onClick?.Invoke());

        public void Bind(WordEntry entry, Action onClick)
        {
            _onClick = onClick;
            _word.text = entry.Word;
            _reading.text = entry.Reading;
            _reading.gameObject.SetActive(!string.IsNullOrEmpty(entry.Reading));
            if (_difficultMark != null) _difficultMark.SetActive(entry.Difficult);
            Fill(_progressFill, entry.Progress, _progressColor);
            Fill(_reverseFill, entry.ReverseProgress, _reverseColor);
        }

        private void Fill(Image fill, int progress, Color color)
        {
            if (fill == null) return;
            int max = WordEntry.MaxProgress;
            fill.fillAmount = Mathf.Clamp(progress, 0, max) / (float)max;
            fill.color = progress >= max ? _fullColor : color;
        }
    }
}
