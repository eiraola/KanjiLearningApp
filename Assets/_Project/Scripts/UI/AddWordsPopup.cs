using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Multi-line box to paste tab-separated words, plus "Paste" (reads the clipboard directly, so tabs survive
    /// even if the soft keyboard drops them). "Add" hands the text to the caller, which closes the popup or shows
    /// an inline error. Outside tap does nothing; Back / Cancel closes.
    /// </summary>
    public sealed class AddWordsPopup : PopupView
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_InputField _field;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _btnPaste;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnAdd;

        private Action<string> _onSubmit;
        private bool _open;
        private Canvas _canvas;
        private readonly Vector3[] _corners = new Vector3[4];

        public override bool CloseOnOutsideTap => false;

        /// <summary>Clipboard source; tests replace it.</summary>
        public Func<string> ReadClipboard { get; set; } = () => GUIUtility.systemCopyBuffer;

        public string Text => _field.text;

        private void Awake()
        {
            _field.lineType = TMP_InputField.LineType.MultiLineNewline;
            _field.characterLimit = 0;
            _field.richText = false;
            _field.onValueChanged.AddListener(_ => HideError());
            _btnPaste.onClick.AddListener(Paste);
            _btnCancel.onClick.AddListener(Cancel);
            _btnAdd.onClick.AddListener(Submit);
        }

        public void Show(Action<string> onSubmit)
        {
            _onSubmit = onSubmit;
            _open = true;
            _field.SetTextWithoutNotify(string.Empty);
            HideError();
            SetButtons(true);
            Popups.Open(this);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _onSubmit = null;
            _field.DeactivateInputField();
            SetButtons(false);
            Popups.Close(this);
            _panel.anchoredPosition = Vector2.zero;
        }

        public override void Cancel() => Close();

        private void Paste()
        {
            if (!_open) return;
            string clip = null;
            try { clip = ReadClipboard?.Invoke(); }
            catch (Exception e) { Debug.LogWarning("[Kanji] Clipboard read failed: " + e.Message); }
            if (string.IsNullOrEmpty(clip))
            {
                ShowError(UiStrings.ClipboardEmpty);
                return;
            }
            _field.DeactivateInputField();
            _field.text = clip;
            _field.MoveTextStart(false);
        }

        private void Submit()
        {
            if (!_open) return;
            _field.DeactivateInputField();
            _onSubmit?.Invoke(_field.text);
        }

        private void HideError() => _errorText.gameObject.SetActive(false);

        private void SetButtons(bool on)
        {
            _btnPaste.interactable = on;
            _btnCancel.interactable = on;
            _btnAdd.interactable = on;
        }

        // Same as the name popup: lift the panel above the soft keyboard while it is visible.
        private void Update()
        {
            if (!_open) return;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float target = 0f;
            if (TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible)
            {
                _panel.GetWorldCorners(_corners);
                float bottomAtRest = _corners[0].y - _panel.anchoredPosition.y * scale;
                target = Mathf.Max(0f, TouchScreenKeyboard.area.height - bottomAtRest) / scale;
            }
            if (!Mathf.Approximately(_panel.anchoredPosition.y, target))
                _panel.anchoredPosition = new Vector2(0f, target);
        }
    }
}
