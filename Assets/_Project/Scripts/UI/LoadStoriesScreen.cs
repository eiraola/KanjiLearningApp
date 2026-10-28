using System.Globalization;
using Kanji.Core.Files;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Core.Parsing;
using Kanji.Core.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Lets the user pick a text file and shows name, size, encoding and a preview. Saves nothing.</summary>
    public sealed class LoadStoriesScreen : ScreenView
    {
        private const int PreviewChars = 3000;

        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnPick;
        [SerializeField] private TMP_Text _fileName;
        [SerializeField] private TMP_Text _fileMeta;
        [SerializeField] private GameObject _infoRoot;
        [SerializeField] private GameObject _previewRoot;
        [SerializeField] private ScrollRect _previewScroll;
        [SerializeField] private TMP_Text _previewText;
        [SerializeField] private TMP_Text _moreLabel;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private UiTheme _theme;

        private IFilePicker _picker;
        private TextDecoder _decoder;
        private StoryParser _parser;
        private StoryLibrary _library;
        private NameInputPopup _namePopup;
        private ToastView _toast;
        private Story _pending;
        private ParseReport _pendingReport;

        public void Init(ScreenNavigator navigator, IFilePicker picker, TextDecoder decoder,
            StoryParser parser, StoryLibrary library, NameInputPopup namePopup, ToastView toast)
        {
            if (_theme == null) Debug.LogError("LoadStoriesScreen: UiTheme not assigned.", this);
            _picker = picker;
            _decoder = decoder;
            _parser = parser;
            _library = library;
            _namePopup = namePopup;
            _toast = toast;
            _btnBack.onClick.AddListener(() => navigator.Pop());
            _btnPick.onClick.AddListener(OnPickClicked);
            _previewText.richText = false;
            ShowInitial();
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            _btnPick.interactable = true;
            ShowInitial();
        }

        public override void OnHide()
        {
            _pending = null;
            _pendingReport = null;
            if (_previewText != null) ClearAll();
            base.OnHide();
        }

        private void OnPickClicked()
        {
            _btnPick.interactable = false;
            _picker.PickTextFile(OnPicked);
        }

        private void OnPicked(PickResult result)
        {
            _btnPick.interactable = true;
            switch (result.Status)
            {
                case PickStatus.Cancelled:
                    return;
                case PickStatus.Error:
                    if (result.Error == PickError.Busy) return;
                    ShowError(result.Error == PickError.TooLarge ? UiStrings.ErrorTooLarge : UiStrings.ErrorUnreadable);
                    return;
            }

            var decoded = _decoder.Decode(result.Bytes);
            if (!decoded.IsSuccess)
            {
                ShowError(decoded.Error == DecodeError.TooLarge ? UiStrings.ErrorTooLarge : UiStrings.ErrorEncoding);
                return;
            }
            ShowFile(result.FileName, result.SizeBytes, decoded);
            Import(result.FileName, decoded.Text);
        }

        private void Import(string fileName, string text)
        {
            string fallback = StoryName.FromFileName(fileName);
            ParseResult parsed = _parser.Parse(fallback, text);
            _pending = parsed.Story;
            _pendingReport = parsed.Report;
            var request = new NameInputRequest
            {
                Title = UiStrings.NameTitle,
                Placeholder = fallback,
                Stats = LoadStats.From(parsed.Story, parsed.Report),
                Validate = input => _library.Check(input, fallback, null)
            };
            _namePopup.Show(request, OnNameAccepted);
        }

        // Returns null to close the popup, or the inline error when the safety-net check finds a duplicate.
        private string OnNameAccepted(string name)
        {
            ImportResult r = _library.Import(_pending, name);
            if (r.Status == ImportStatus.Duplicate)
                return string.Format(CultureInfo.InvariantCulture, UiStrings.NameDuplicate, r.ConflictingName);
            FinishImport(r);
            return null;
        }

        private void FinishImport(ImportResult r)
        {
            ParseReport report = _pendingReport;
            _pending = null;
            _pendingReport = null;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Kanji][IO] import status=" + r.Status + " id=" + (r.Story != null ? r.Story.Id : "-") + " name=" + (r.Story != null ? r.Story.Name : "-") + (r.Status == ImportStatus.Failed ? " error=" + r.Error + " msg=" + r.Message : ""));
#endif
            if (r.Status == ImportStatus.Failed) { ShowError(UiStrings.SaveFailed); return; }
            if (r.Status == ImportStatus.Duplicate)
            {
                ShowError(string.Format(CultureInfo.InvariantCulture, UiStrings.NameDuplicate, r.ConflictingName));
                return;
            }
            if (r.Status != ImportStatus.Imported) return;
            string msg = string.Format(CultureInfo.InvariantCulture, UiStrings.StoryLoaded,
                r.Story.Name, UiStrings.KanjiCount(r.Story.Entries.Count));
            if (report != null && report.Warnings.Count > 0)
                msg += "\n" + UiStrings.ParseWarnings(report.Warnings.Count);
            _toast.Show(msg);
        }

        private void ShowFile(string name, long sizeBytes, DecodeResult decoded)
        {
            var preview = TextPreview.Create(decoded.Text, PreviewChars);
            _fileName.text = name ?? string.Empty;
            _fileMeta.text = string.Format(CultureInfo.InvariantCulture, UiStrings.FileMeta,
                FormatKb(sizeBytes), EncodingLabel(decoded.Encoding));
            _previewText.text = preview.Head;
            bool more = preview.RemainingChars > 0;
            _moreLabel.gameObject.SetActive(more);
            _moreLabel.text = more
                ? string.Format(CultureInfo.InvariantCulture, UiStrings.MoreChars, preview.RemainingChars)
                : string.Empty;
            SetPreviewInset(more);
            _infoRoot.SetActive(true);
            _previewRoot.SetActive(true);
            _message.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_previewText.rectTransform);
            _previewScroll.verticalNormalizedPosition = 1f;
        }

        private void ShowError(string text) => ShowMessage(text, _theme.Error);

        private void ShowInitial() => ShowMessage(UiStrings.LoadHint, _theme.TextSecondary);

        private void ShowMessage(string text, Color color)
        {
            ClearPreview();
            _infoRoot.SetActive(false);
            _previewRoot.SetActive(false);
            _message.text = text;
            _message.color = color;
            _message.gameObject.SetActive(true);
        }

        private void ClearAll()
        {
            ClearPreview();
            _message.text = string.Empty;
        }

        private void ClearPreview()
        {
            _fileName.text = string.Empty;
            _fileMeta.text = string.Empty;
            _previewText.text = string.Empty;
            _moreLabel.text = string.Empty;
        }

        // The scroll area leaves 80 px at the bottom for the "more" label only when it is visible.
        private void SetPreviewInset(bool moreVisible)
        {
            var rt = (RectTransform)_previewScroll.transform;
            rt.offsetMin = new Vector2(rt.offsetMin.x, moreVisible ? 80f : 0f);
        }

        public static string FormatKb(long bytes)
        {
            if (bytes <= 0) return "0";
            return ((bytes + 1023) / 1024).ToString(CultureInfo.InvariantCulture);
        }

        private static string EncodingLabel(TextEncodingKind kind)
        {
            switch (kind)
            {
                case TextEncodingKind.Utf8Bom: return UiStrings.EncodingUtf8Bom;
                case TextEncodingKind.ShiftJis: return UiStrings.EncodingShiftJis;
                default: return UiStrings.EncodingUtf8;
            }
        }
    }
}
