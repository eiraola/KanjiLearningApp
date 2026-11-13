using System;
using Kanji.Core.Words;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Card of a list word, in two modes.
    /// View: every field (empty ones hidden), "Add to difficult" and Close; outside tap and Back close it.
    /// Study: word + sentence and "Resolve"; resolving shows everything plus green/red (buttons or swipe, S13).
    /// Green/red do not close it: the caller shows the next word with ShowStudy or ends with Close().
    /// Back reports Dismissed; outside tap does nothing.
    /// Reverse study (S16): only the reading first; resolving shows the word, the reading and the meaning.
    /// S17: the card fills the screen; reverse study adds a finger drawing pad (cleared on every new card and by "Borrar").
    /// </summary>
    public sealed class WordCardPopup : PopupView
    {
        [SerializeField] private TMP_Text _word;
        [SerializeField] private TMP_Text _sentence;
        [SerializeField] private GameObject _rowSentenceReading;
        [SerializeField] private TMP_Text _sentenceReading;
        [SerializeField] private GameObject _rowReading;
        [SerializeField] private TMP_Text _reading;
        [SerializeField] private GameObject _rowMeaning;
        [SerializeField] private TMP_Text _meaning;
        [SerializeField] private GameObject _rowSentenceMeaning;
        [SerializeField] private TMP_Text _sentenceMeaning;
        [SerializeField] private Button _btnClose;
        [SerializeField] private ScrollRect _body;
        [SerializeField] private LayoutElement _bodyLayout;
        [SerializeField] private RectTransform _bodyContent;
        [SerializeField] private float _maxBodyHeight = 1100f;

        [Header("S15: difficult + study")]
        [SerializeField] private Button _btnDifficult;
        [SerializeField] private TMP_Text _difficultLabel;
        [SerializeField] private Image _difficultIcon;
        [SerializeField] private Sprite _iconPlus;
        [SerializeField] private Sprite _iconCheck;
        [SerializeField] private Color _iconPlusColor = Color.white;
        [SerializeField] private Color _iconCheckColor = new Color(0.482f, 0.847f, 0.561f, 1f);
        [SerializeField] private Button _btnReveal;
        [SerializeField] private GameObject _answerRow;
        [SerializeField] private Button _btnKnew;
        [SerializeField] private Button _btnDidntKnow;
        [SerializeField] private CardSwipe _swipe;

        [Header("S17: full screen + drawing pad")]
        [SerializeField] private bool _fillScreen;
        [SerializeField] private GameObject _drawArea;
        [SerializeField] private DrawingPad _pad;
        [SerializeField] private Button _btnClearDrawing;

        private bool _open;
        private bool _studyMode;
        private bool _reverse;
        private Action<KanjiPopupResult> _onAnswer;
        private Func<WordEntry, bool, WordEntry> _setDifficult;

        public WordEntry Current { get; private set; }

        public bool IsStudying => _open && _studyMode;

        public override bool CloseOnOutsideTap => !_studyMode;

        private void Awake()
        {
            _btnClose.onClick.AddListener(Close);
            if (_btnDifficult != null) _btnDifficult.onClick.AddListener(ToggleDifficult);
            if (_btnReveal != null) _btnReveal.onClick.AddListener(Reveal);
            if (_btnKnew != null) _btnKnew.onClick.AddListener(() => Answer(KanjiPopupResult.Knew));
            if (_btnDidntKnow != null) _btnDidntKnow.onClick.AddListener(() => Answer(KanjiPopupResult.DidntKnow));
            if (_btnClearDrawing != null) _btnClearDrawing.onClick.AddListener(ClearDrawing);
            if (_swipe != null)
            {
                _swipe.CanSwipe = () => _open && _studyMode && _answerRow != null && _answerRow.activeSelf;
                _swipe.SwipedGreen += () => Answer(KanjiPopupResult.Knew);
                _swipe.SwipedRed += () => Answer(KanjiPopupResult.DidntKnow);
            }
        }

        /// <param name="setDifficult">Saves the difficult flag and returns the updated word (null = save failed). Null hides the button.</param>
        public void Show(WordEntry entry, Func<WordEntry, bool, WordEntry> setDifficult = null)
        {
            _studyMode = false;
            _reverse = false;
            _onAnswer = null;
            _setDifficult = setDifficult;
            Present(entry, true);
        }

        /// <summary>Study mode: word + sentence and "Resolve" first. onAnswer gets Knew / DidntKnow, or Dismissed on Back / Close().</summary>
        /// <param name="reverse">S16: show only the reading first; resolving reveals word + meaning.</param>
        public void ShowStudy(WordEntry entry, Func<WordEntry, bool, WordEntry> setDifficult, Action<KanjiPopupResult> onAnswer,
            bool reverse = false)
        {
            _studyMode = true;
            _reverse = reverse;
            _onAnswer = onAnswer;
            _setDifficult = setDifficult;
            Present(entry, false);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            var callback = _studyMode ? _onAnswer : null;
            _onAnswer = null;
            Popups.Close(this);
            callback?.Invoke(KanjiPopupResult.Dismissed);
        }

        public override void Cancel() => Close();

        private void Present(WordEntry entry, bool details)
        {
            Current = entry;
            _open = true;
            if (_swipe != null) _swipe.ResetPose();
            if (_drawArea != null) _drawArea.SetActive(_studyMode && _reverse);
            SetDetails(details);
            RefreshDifficult();
            _btnClose.gameObject.SetActive(!_studyMode);
            Popups.Open(this); // no-op when already open (next study word)
            FitBody();
            ClearDrawing(); // after layout, so the pad has its final size
        }

        private void ClearDrawing()
        {
            if (_pad != null && _pad.isActiveAndEnabled) _pad.Clear();
        }

        private void SetDetails(bool visible)
        {
            WordEntry e = Current;
            bool reverse = _studyMode && _reverse;
            // Reverse front: the reading takes the big slot (the word itself if a word has no reading).
            bool readingOnTop = reverse && !visible && !string.IsNullOrWhiteSpace(e.Reading);
            _word.text = readingOnTop ? e.Reading : e.Word;
            SetRow(_sentence.gameObject, _sentence, reverse ? null : e.Sentence);
            SetRow(_rowSentenceReading, _sentenceReading, visible && !reverse ? e.SentenceReading : null);
            SetRow(_rowReading, _reading, visible ? e.Reading : null);
            SetRow(_rowMeaning, _meaning, visible ? e.Meaning : null);
            SetRow(_rowSentenceMeaning, _sentenceMeaning, visible && !reverse ? e.SentenceMeaning : null);
            if (_answerRow != null) _answerRow.SetActive(_studyMode && visible);
            if (_btnReveal != null) _btnReveal.gameObject.SetActive(_studyMode && !visible);
        }

        private void Reveal()
        {
            if (!_open || !_studyMode || Current == null) return;
            SetDetails(true);
            FitBody();
        }

        private void Answer(KanjiPopupResult result)
        {
            if (!_open || !_studyMode || _answerRow == null || !_answerRow.activeSelf) return;
            _onAnswer?.Invoke(result); // stays open; the caller re-fills it with ShowStudy or closes it
        }

        private void RefreshDifficult()
        {
            if (_btnDifficult == null) return;
            bool available = _setDifficult != null && Current != null;
            _btnDifficult.gameObject.SetActive(available);
            if (!available) return;
            bool on = Current.Difficult;
            _difficultLabel.text = on ? UiStrings.InDifficultWords : UiStrings.AddToDifficultWords;
            if (_difficultIcon != null)
            {
                _difficultIcon.sprite = on ? _iconCheck : _iconPlus;
                _difficultIcon.color = on ? _iconCheckColor : _iconPlusColor;
            }
        }

        // Toggles: tapping "Ya en complicadas" takes the word out again.
        private void ToggleDifficult()
        {
            if (!_open || Current == null || _setDifficult == null) return;
            WordEntry updated = _setDifficult(Current, !Current.Difficult);
            if (updated == null) return;
            Current = updated;
            RefreshDifficult();
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
            if (_fillScreen) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_body.transform.parent); // body takes what is left
            else _bodyLayout.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(_bodyContent), _maxBodyHeight);
            _body.StopMovement();
            _bodyContent.anchoredPosition = Vector2.zero;
            _body.verticalNormalizedPosition = 1f;
        }
    }
}
