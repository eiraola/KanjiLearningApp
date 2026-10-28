using System;
using System.Globalization;
using Kanji.Core.Library;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Immutable description of what <see cref="NameInputPopup"/> shows and how it validates.</summary>
    public sealed class NameInputRequest
    {
        public string Title;
        public string Placeholder;
        /// <summary>Text preloaded in the field. Non-empty (edit mode): Accept stays disabled while the text equals it.</summary>
        public string InitialText = string.Empty;
        /// <summary>Null hides the statistics block (edit mode).</summary>
        public LoadStats? Stats;
        public Func<string, NameResult> Validate;
    }

    /// <summary>
    /// Single popup used to name a story on load and to rename it later. No domain logic: validation comes
    /// from the request. Outside tap does nothing; Back / Cancel closes without calling the accept callback.
    /// </summary>
    public sealed class NameInputPopup : PopupView
    {
        [SerializeField] private UiTheme _theme;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_InputField _nameField;
        [SerializeField] private TMP_Text _placeholder;
        [SerializeField] private Outline _fieldBorder;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private GameObject _statsRoot;
        [SerializeField] private TMP_Text _statKanjis;
        [SerializeField] private TMP_Text _statCharacters;
        [SerializeField] private TMP_Text _statWarnings;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnAccept;
        [SerializeField] private TMP_Text _acceptLabel;

        private NameInputRequest _request;
        private Func<string, string> _onAccept;
        private bool _open;
        private bool _acceptEnabled;
        private Canvas _canvas;
        private readonly Vector3[] _corners = new Vector3[4];

        public override bool CloseOnOutsideTap => false;

        private void Awake()
        {
            _title.richText = false;
            _errorText.richText = false;
            _nameField.characterLimit = 60;
            _nameField.lineType = TMP_InputField.LineType.SingleLine;
            _nameField.onValueChanged.AddListener(OnTextChanged);
            _nameField.onSubmit.AddListener(OnSubmit);
            _btnCancel.onClick.AddListener(Cancel);
            _btnAccept.onClick.AddListener(Accept);
        }

        /// <summary>
        /// Opens the popup. <paramref name="onAccept"/> receives the resolved name and returns null to close,
        /// or an error text to show inline and keep the popup open.
        /// </summary>
        public void Show(NameInputRequest request, Func<string, string> onAccept)
        {
            _request = request;
            _onAccept = onAccept;
            _open = true;
            _title.text = request.Title;
            _placeholder.text = request.Placeholder ?? string.Empty;
            _nameField.SetTextWithoutNotify(request.InitialText ?? string.Empty);
            ShowStats(request.Stats);
            SetButtons(true);
            Refresh(_nameField.text);
            Popups.Open(this);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public override void Cancel()
        {
            if (!_open) return;
            Close();
        }

        private void Close()
        {
            _open = false;
            _onAccept = null;
            _nameField.DeactivateInputField();
            SetButtons(false);
            Popups.Close(this);
            _panel.anchoredPosition = Vector2.zero;
        }

        private void Accept()
        {
            if (!_open || !_acceptEnabled) return;
            string name = _request.Validate(_nameField.text).Name;
            string error = _onAccept?.Invoke(name);
            if (error == null) Close();
            else ShowError(error);
        }

        private void OnSubmit(string _)
        {
            if (_acceptEnabled) Accept();
            else _nameField.DeactivateInputField();
        }

        private void OnTextChanged(string text) => Refresh(text);

        private void Refresh(string text)
        {
            NameResult r = _request.Validate(text);
            bool unchanged = !string.IsNullOrEmpty(_request.InitialText) && text == _request.InitialText;
            if (r.Check == NameCheck.Duplicate)
                ShowError(string.Format(CultureInfo.InvariantCulture, UiStrings.NameDuplicate, r.ConflictingName));
            else
                HideError();
            SetAccept(r.Check == NameCheck.Ok && !unchanged);
        }

        private void ShowError(string text)
        {
            _errorText.text = text;
            _errorText.gameObject.SetActive(true);
            _fieldBorder.effectColor = _theme.Error;
            SetAccept(false);
        }

        private void HideError()
        {
            _errorText.gameObject.SetActive(false);
            _fieldBorder.effectColor = _theme.Border;
        }

        private void SetAccept(bool enabled)
        {
            _acceptEnabled = enabled;
            _btnAccept.interactable = enabled && _open;
            _acceptLabel.color = enabled ? _theme.TextOnAccent : _theme.DisabledText;
        }

        private void SetButtons(bool on)
        {
            _btnCancel.interactable = on;
            _btnAccept.interactable = on && _acceptEnabled;
        }

        private void ShowStats(LoadStats? stats)
        {
            _statsRoot.SetActive(stats.HasValue);
            if (!stats.HasValue) return;
            LoadStats s = stats.Value;
            _statKanjis.text = string.Format(CultureInfo.InvariantCulture, UiStrings.StatKanjis, s.Kanjis);
            _statCharacters.text = string.Format(CultureInfo.InvariantCulture, UiStrings.StatCharacters, s.Characters);
            _statWarnings.transform.parent.gameObject.SetActive(s.Warnings > 0);
            _statWarnings.text = string.Format(CultureInfo.InvariantCulture, UiStrings.StatWarnings, s.Warnings);
        }

        // Lifts the panel just enough to keep field, error and buttons above the soft keyboard.
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
