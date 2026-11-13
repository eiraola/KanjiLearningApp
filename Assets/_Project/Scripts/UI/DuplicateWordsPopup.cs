using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kanji.Core.Words;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public enum DuplicateChoice { Cancel, Ignore, Update }

    /// <summary>Lists the repeated words of a paste and asks whether to ignore or update them. Back = cancel.</summary>
    public sealed class DuplicateWordsPopup : PopupView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _list;
        [SerializeField] private ScrollRect _body;
        [SerializeField] private LayoutElement _bodyLayout;
        [SerializeField] private RectTransform _bodyContent;
        [SerializeField] private float _maxBodyHeight = 720f;
        [SerializeField] private Button _btnIgnore;
        [SerializeField] private Button _btnUpdate;
        [SerializeField] private Button _btnCancel;

        private Action<DuplicateChoice> _onChoice;
        private bool _open;

        public override bool CloseOnOutsideTap => false;

        private void Awake()
        {
            _btnIgnore.onClick.AddListener(() => Resolve(DuplicateChoice.Ignore));
            _btnUpdate.onClick.AddListener(() => Resolve(DuplicateChoice.Update));
            _btnCancel.onClick.AddListener(Cancel);
        }

        public void Show(IReadOnlyList<WordEntry> duplicates, Action<DuplicateChoice> onChoice)
        {
            _onChoice = onChoice;
            _open = true;
            _title.text = string.Format(CultureInfo.InvariantCulture, UiStrings.DuplicatesTitle, duplicates.Count);
            var sb = new StringBuilder();
            for (int i = 0; i < duplicates.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(duplicates[i].Word);
                if (!string.IsNullOrEmpty(duplicates[i].Reading)) sb.Append("  ·  ").Append(duplicates[i].Reading);
            }
            _list.text = sb.ToString();
            Popups.Open(this);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyContent);
            _bodyLayout.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(_bodyContent), _maxBodyHeight);
            _bodyContent.anchoredPosition = Vector2.zero;
            _body.verticalNormalizedPosition = 1f;
        }

        public override void Cancel() => Resolve(DuplicateChoice.Cancel);

        private void Resolve(DuplicateChoice choice)
        {
            if (!_open) return;
            _open = false;
            var callback = _onChoice;
            _onChoice = null;
            Popups.Close(this);
            callback?.Invoke(choice);
        }
    }
}
