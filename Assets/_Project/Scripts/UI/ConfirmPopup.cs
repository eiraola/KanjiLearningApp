using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Generic confirmation. Outside tap does nothing; Back = cancel. Cancel is focused by default.</summary>
    public sealed class ConfirmPopup : PopupView
    {
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnConfirm;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Outline _cancelFocusOutline;

        private Action<bool> _onResult;
        private bool _resolved;

        public override bool CloseOnOutsideTap => false;

        private void Awake()
        {
            _btnCancel.onClick.AddListener(Cancel);
            _btnConfirm.onClick.AddListener(() => Resolve(true));
        }

        public void Show(string message, string confirmLabel, Action<bool> onResult)
        {
            _onResult = onResult;
            _resolved = false;
            _message.text = message;
            _confirmLabel.text = confirmLabel;
            _btnCancel.interactable = true;
            _btnConfirm.interactable = true;
            _cancelFocusOutline.enabled = true;
            Popups.Open(this);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_btnCancel.gameObject);
        }

        public override void Cancel() => Resolve(false);

        private void Resolve(bool confirmed)
        {
            if (_resolved) return;
            _resolved = true;
            _btnCancel.interactable = false;
            _btnConfirm.interactable = false;
            Popups.Close(this);
            _onResult?.Invoke(confirmed);
        }
    }
}
