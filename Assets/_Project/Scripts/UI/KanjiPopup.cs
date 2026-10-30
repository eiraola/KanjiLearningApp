using System;
using Kanji.Core.Library;
using Kanji.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public enum KanjiPopupResult { Knew, DidntKnow, Dismissed }

    /// <summary>Single-view kanji popup: kanji, sentence, reading, meaning and translation, plus green/red answer buttons.</summary>
    public sealed class KanjiPopup : PopupView
    {
        [SerializeField] private TMP_Text _kanji;
        [SerializeField] private TMP_Text _sentence;
        [SerializeField] private TMP_Text _reading;
        [SerializeField] private TMP_Text _meaning;
        [SerializeField] private TMP_Text _sentenceMeaning;
        [SerializeField] private GameObject _rowReading;
        [SerializeField] private GameObject _rowMeaning;
        [SerializeField] private GameObject _rowSentenceMeaning;
        [SerializeField] private Button _btnKnew;
        [SerializeField] private Button _btnDidntKnow;
        [SerializeField] private RectTransform _secondaryActions;
        [SerializeField] private Button _btnAddDifficult;
        [SerializeField] private TMP_Text _addDifficultLabel;
        [SerializeField] private Image _addDifficultIcon;
        [SerializeField] private Sprite _iconPlus;
        [SerializeField] private Sprite _iconCheck;
        [SerializeField] private Color _iconCheckColor = new Color(0.482f, 0.847f, 0.561f, 1f);
        [SerializeField] private Color _iconPlusColor = Color.white;
        [SerializeField] private ScrollRect _body;
        [SerializeField] private LayoutElement _bodyLayout;
        [SerializeField] private RectTransform _bodyContent;
        [SerializeField] private float _maxBodyHeight = 1100f;

        private Action<KanjiPopupResult> _onClosed;
        private bool _open;
        private DifficultKanjiList _difficult;
        private ToastView _toast;
        private KanjiEntry _entry;
        private bool _difficultMode;
        private Action _onRemoved;

        [SerializeField] private GameObject _answerRow;
        [SerializeField] private Button _btnRemoveDifficult;
        [SerializeField] private Button _btnCloseDifficult;
        [SerializeField] private Button _btnReveal;
        [SerializeField] private CardSwipe _swipe;
        private bool _studyMode;

        private void Awake()
        {
            if (_btnRemoveDifficult != null) _btnRemoveDifficult.onClick.AddListener(RemoveFromDifficult);
            if (_btnCloseDifficult != null) _btnCloseDifficult.onClick.AddListener(Close);
            _btnKnew.onClick.AddListener(AnswerKnew);
            _btnDidntKnow.onClick.AddListener(AnswerDidntKnow);
            if (_swipe != null)
            {
                // Same handlers as the buttons: right = green, left = red, only while that row is shown.
                _swipe.CanSwipe = () => _open && _answerRow != null && _answerRow.activeSelf;
                _swipe.SwipedGreen += AnswerKnew;
                _swipe.SwipedRed += AnswerDidntKnow;
            }
            if (_btnAddDifficult != null) _btnAddDifficult.onClick.AddListener(AddToDifficult);
            if (_btnReveal != null) _btnReveal.onClick.AddListener(Reveal);
        }

        private void AnswerKnew() => Resolve(KanjiPopupResult.Knew);

        private void AnswerDidntKnow() => Resolve(KanjiPopupResult.DidntKnow);

        private DifficultKanjiList _fixedList; // fallback list when no story id is given (unit tests only)
        private DifficultKanjiStore _store;

        public void Init(PopupService popups, DifficultKanjiList difficult, ToastView toast, DifficultKanjiStore store = null)
        {
            Init(popups);
            _difficult = difficult;
            _fixedList = difficult;
            _store = store;
            _toast = toast;
        }

        private void RefreshDifficultButton()
        {
            if (_secondaryActions == null || _btnAddDifficult == null) return;
            bool available = _difficult != null;
            _secondaryActions.gameObject.SetActive(available);
            _btnAddDifficult.gameObject.SetActive(!_difficultMode);
            if (_btnRemoveDifficult != null) _btnRemoveDifficult.gameObject.SetActive(_difficultMode);
            if (_btnCloseDifficult != null) _btnCloseDifficult.gameObject.SetActive(_difficultMode);
            if (_answerRow != null) _answerRow.SetActive(!_difficultMode);
            if (available && !_difficultMode) SetDifficultState(_difficult.Contains(_entry));
        }

        /// <summary>Difficult-list mode: offers removal (no confirmation) instead of add/answer buttons.</summary>
        public void ShowDifficult(KanjiEntry entry, string storyId, Action onRemoved)
        {
            _difficult = _store != null && storyId != null ? _store.For(storyId) : _fixedList;
            _difficultMode = true;
            _studyMode = false;
            _onRemoved = onRemoved;
            Present(entry, null);
        }

        private void RemoveFromDifficult()
        {
            if (!_open || !_difficultMode || _difficult == null || _entry == null) return;
            if (!_difficult.Remove(_entry))
            {
                if (_toast != null) _toast.Show(UiStrings.RemoveFailed);
                return;
            }
            var callback = _onRemoved;
            Resolve(KanjiPopupResult.Dismissed);
            if (_toast != null) _toast.Show(UiStrings.RemovedFromDifficult);
            callback?.Invoke();
        }

        private void SetDifficultState(bool already)
        {
            _btnAddDifficult.interactable = !already;
            _addDifficultLabel.text = already ? UiStrings.InDifficult : UiStrings.AddToDifficult;
            _addDifficultIcon.sprite = already ? _iconCheck : _iconPlus;
            _addDifficultIcon.color = already ? _iconCheckColor : _iconPlusColor;
        }

        private void AddToDifficult()
        {
            if (!_open || _difficult == null || _entry == null) return;
            switch (_difficult.Add(_entry))
            {
                case DifficultAddResult.Added:
                    SetDifficultState(true);
                    if (_toast != null) _toast.Show(UiStrings.AddedToDifficult);
                    break;
                case DifficultAddResult.AlreadyPresent:
                    SetDifficultState(true);
                    break;
                default:
                    if (_toast != null) _toast.Show(UiStrings.DifficultSaveFailed);
                    break;
            }
        }

        /// <summary>Opens with the body scrolled to the top. Empty fields are hidden.</summary>
        public void Show(KanjiEntry entry, Action<KanjiPopupResult> onClosed = null)
        {
            _difficult = _fixedList;
            _difficultMode = false;
            _studyMode = false;
            _onRemoved = null;
            Present(entry, onClosed);
        }

        /// <summary>Reader mode bound to the open story: add/"already" state uses that story's list.</summary>
        public void Show(KanjiEntry entry, string storyId, Action<KanjiPopupResult> onClosed = null)
        {
            _difficult = _store != null && storyId != null ? _store.For(storyId) : _fixedList;
            _difficultMode = false;
            _studyMode = false;
            _onRemoved = null;
            Present(entry, onClosed);
        }

        /// <summary>
        /// Study mode: kanji + sentence and only "Resolve"; resolving shows everything plus green/red.
        /// Green/red do not close the popup: the caller shows the next kanji with ShowStudy or ends with Close().
        /// Back closes it and reports Dismissed.
        /// </summary>
        public void ShowStudy(KanjiEntry entry, Action<KanjiPopupResult> onClosed)
        {
            _difficult = null;
            _difficultMode = false;
            _studyMode = true;
            _onRemoved = null;
            Present(entry, onClosed);
        }

        private void Reveal()
        {
            if (!_open || !_studyMode || _entry == null) return;
            SetDetails(_entry, true);
            FitBody();
        }

        // Hidden details = study mode before "Resolve": only kanji, sentence and the Resolve button.
        private void SetDetails(KanjiEntry entry, bool visible)
        {
            SetRow(_rowReading, _reading, visible ? entry.Reading : null);
            SetRow(_rowMeaning, _meaning, visible ? entry.Meaning : null);
            SetRow(_rowSentenceMeaning, _sentenceMeaning, visible ? entry.SentenceMeaning : null);
            if (!_studyMode) return;
            if (_answerRow != null) _answerRow.SetActive(visible);
            if (_btnReveal != null) _btnReveal.gameObject.SetActive(!visible);
        }

        private void Present(KanjiEntry entry, Action<KanjiPopupResult> onClosed)
        {
            _onClosed = onClosed;
            _open = true;
            _entry = entry;
            if (_swipe != null) _swipe.ResetPose();
            RefreshDifficultButton();
            _kanji.text = entry.Kanji;
            SetRow(_sentence.gameObject, _sentence, entry.Sentence);
            if (_btnReveal != null) _btnReveal.gameObject.SetActive(false);
            SetDetails(entry, !_studyMode);

            Popups.Open(this);
            FitBody();
        }

        public void Close() => Resolve(KanjiPopupResult.Dismissed);

        public override void Cancel() => Resolve(KanjiPopupResult.Dismissed);

        // Tapping outside must not end a study session by accident.
        public override bool CloseOnOutsideTap => !_studyMode;

        private void Resolve(KanjiPopupResult result)
        {
            if (!_open) return;
            if (_studyMode && result != KanjiPopupResult.Dismissed)
            {
                _onClosed?.Invoke(result); // stays open; Present() re-fills it in place (Popups.Open ignores an open popup)
                return;
            }
            _open = false;
            var callback = _onClosed;
            _onClosed = null;
            Popups.Close(this);
            callback?.Invoke(result);
        }

        private static void SetRow(GameObject row, TMP_Text value, string text)
        {
            bool has = !string.IsNullOrWhiteSpace(text);
            value.text = has ? text : string.Empty;
            row.SetActive(has);
        }

        private void FitBody()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyContent);
            _bodyLayout.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(_bodyContent), _maxBodyHeight);
            _body.StopMovement();
            _bodyContent.anchoredPosition = Vector2.zero;
            _body.verticalNormalizedPosition = 1f;
        }
    }
}
