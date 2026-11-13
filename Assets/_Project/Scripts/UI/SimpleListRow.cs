using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Generic tappable row: title on the left, subtitle on the right.</summary>
    public sealed class SimpleListRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;

        private Action _onClick;

        private void Awake() => _button.onClick.AddListener(() => _onClick?.Invoke());

        public void Bind(string title, string subtitle, Action onClick)
        {
            _title.text = title ?? string.Empty;
            _subtitle.text = subtitle ?? string.Empty;
            _subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            _onClick = onClick;
        }
    }
}
