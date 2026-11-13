using System.Collections.Generic;
using System.Globalization;
using Kanji.Core.Library;
using Kanji.Core.Words;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Words of one list (args: list id), with an All / Difficult filter. Tapping a word opens its card; the top-right
    /// menu offers "Add words" (paste, then ignore/update repeated words) and "Study" (every word, random order).
    /// </summary>
    public sealed class WordListScreen : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private WordListRow _rowPrefab;
        [SerializeField] private TMP_Text _emptyMessage;
        [SerializeField] private UiTheme _theme;
        [SerializeField] private GameObject _filterBar;
        [SerializeField] private Button _filterAll;
        [SerializeField] private Button _filterDifficult;
        [SerializeField] private TMP_Text _filterAllLabel;
        [SerializeField] private TMP_Text _filterDifficultLabel;

        private readonly List<WordListRow> _rows = new List<WordListRow>();
        private ScreenNavigator _navigator;
        private WordListLibrary _library;
        private WordCardPopup _card;
        private WordListMenuPopup _menu;
        private AddWordsPopup _addWords;
        private DuplicateWordsPopup _duplicates;
        private ToastView _toast;
        private string _listId;
        private string _listName;
        private int _wordCount;
        private string _renderedId;
        private int _renderedRevision = -1;
        private bool _onlyDifficult;
        private WordStudySession _session;

        internal WordStudySession Session => _session;
        internal bool OnlyDifficult => _onlyDifficult;

        public void Init(ScreenNavigator navigator, WordListLibrary library, WordCardPopup card, WordListMenuPopup menu,
            AddWordsPopup addWords, DuplicateWordsPopup duplicates, ToastView toast)
        {
            _navigator = navigator;
            _library = library;
            _card = card;
            _menu = menu;
            _addWords = addWords;
            _duplicates = duplicates;
            _toast = toast;
            _backButton.onClick.AddListener(() => _navigator.Pop());
            _menuButton.onClick.AddListener(OpenMenu);
            if (_filterAll != null) _filterAll.onClick.AddListener(() => SetFilter(false));
            if (_filterDifficult != null) _filterDifficult.onClick.AddListener(() => SetFilter(true));
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            if (args is string id && id != _listId)
            {
                _listId = id; // Pop() passes null: keep the current list
                _onlyDifficult = false;
            }
            if (_library == null || _listId == null) return;
            if (_listId != _renderedId || _library.Revision != _renderedRevision) Render();
        }

        private void SetFilter(bool onlyDifficult)
        {
            if (_onlyDifficult == onlyDifficult) return;
            _onlyDifficult = onlyDifficult;
            Render(resetScroll: true);
        }

        private void Render(bool resetScroll = false)
        {
            bool sameList = _listId == _renderedId && !resetScroll;
            var savedY = _content.anchoredPosition.y;
            RepoResult<WordList> loaded = _library.Load(_listId);
            _renderedId = _listId;
            _renderedRevision = _library.Revision;
            if (!loaded.IsSuccess)
            {
                _listName = string.Empty;
                _wordCount = 0;
                _title.text = string.Empty;
                _menuButton.gameObject.SetActive(false);
                if (_filterBar != null) _filterBar.SetActive(false);
                ShowMessage(UiStrings.WordListOpenFailed, _theme != null ? _theme.Error : Color.red);
                return;
            }

            WordList list = loaded.Value;
            _listName = list.Name;
            _wordCount = list.Words.Count;
            _title.text = list.Name;
            _menuButton.gameObject.SetActive(true);

            var shown = new List<WordEntry>(list.Words.Count);
            int difficult = 0;
            foreach (WordEntry w in list.Words)
            {
                if (w.Difficult) difficult++;
                if (!_onlyDifficult || w.Difficult) shown.Add(w);
            }
            RefreshFilter(list.Words.Count, difficult);

            for (var i = 0; i < shown.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _content, false));
                var word = shown[i];
                _rows[i].Bind(word, () => _card.Show(word, SetDifficult));
                _rows[i].gameObject.SetActive(true);
                _rows[i].transform.SetSiblingIndex(i);
            }
            for (var i = shown.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            if (shown.Count == 0)
            {
                ShowMessage(list.Words.Count == 0 ? UiStrings.EmptyWordList : UiStrings.EmptyDifficultWords,
                    _theme != null ? _theme.TextSecondary : Color.gray);
                return;
            }
            _emptyMessage.gameObject.SetActive(false);
            _listScroll.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var max = Mathf.Max(0f, _content.rect.height - _listScroll.viewport.rect.height);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, sameList ? Mathf.Clamp(savedY, 0f, max) : 0f);
        }

        private void RefreshFilter(int total, int difficult)
        {
            if (_filterBar == null) return;
            _filterBar.SetActive(total > 0);
            _filterAllLabel.text = string.Format(CultureInfo.InvariantCulture, UiStrings.FilterAll, total);
            _filterDifficultLabel.text = string.Format(CultureInfo.InvariantCulture, UiStrings.FilterDifficult, difficult);
            if (_theme == null) return;
            Paint(_filterAll, _filterAllLabel, !_onlyDifficult);
            Paint(_filterDifficult, _filterDifficultLabel, _onlyDifficult);
        }

        private void Paint(Button button, TMP_Text label, bool selected)
        {
            var colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = selected ? _theme.Primary : _theme.Secondary;
            colors.pressedColor = selected ? _theme.PrimaryPressed : _theme.SecondaryPressed;
            button.colors = colors;
            label.color = selected ? _theme.TextOnAccent : _theme.TextPrimary;
        }

        private void ShowMessage(string text, Color color)
        {
            for (var i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
            _listScroll.gameObject.SetActive(false);
            _emptyMessage.gameObject.SetActive(true);
            _emptyMessage.color = color;
            _emptyMessage.text = text;
        }

        // Card callback: saves the flag and re-renders the rows under the popup (mark + filter counts).
        private WordEntry SetDifficult(WordEntry word, bool difficult)
        {
            RepoResult<WordEntry> result = _library.SetDifficult(_listId, word.Key, difficult);
            if (!result.IsSuccess)
            {
                _toast.Show(UiStrings.DifficultSaveFailed);
                return null;
            }
            if (_session == null) Render();
            return result.Value;
        }

        private void OpenMenu()
        {
            if (_listId == null) return;
            _menu.Show(_listName, _wordCount > 0, action =>
            {
                if (action == WordListMenuAction.AddWords) _addWords.Show(OnPasteSubmitted);
                else if (action == WordListMenuAction.Study) StartStudy(false);
                else if (action == WordListMenuAction.StudyReverse) StartStudy(true);
            });
        }

        // ---------- Study (S15) ----------

        private void StartStudy(bool reverse)
        {
            if (_session != null) return;
            RepoResult<WordList> loaded = _library.Load(_listId);
            if (!loaded.IsSuccess || loaded.Value.Words.Count == 0) return;
            _session = new WordStudySession(_library, loaded.Value, null, reverse);
            _card.ShowStudy(_session.Current, SetDifficult, OnStudyAnswer, reverse);
        }

        private void OnStudyAnswer(KanjiPopupResult result)
        {
            if (_session == null) return;
            if (result == KanjiPopupResult.Dismissed)
            {
                FinishStudy();
                return;
            }
            if (!_session.Record(result == KanjiPopupResult.Knew))
                _toast.Show(UiStrings.StudySaveFailed);
            if (_session.IsFinished)
            {
                FinishStudy();
                _card.Close(); // its Dismissed callback is ignored: _session is already null
                return;
            }
            _card.ShowStudy(Fresh(_session.Current), SetDifficult, OnStudyAnswer, _session.Reverse);
        }

        // The session shuffled a snapshot: re-read the word so a difficult mark set earlier in this pass shows.
        private WordEntry Fresh(WordEntry word)
        {
            RepoResult<WordList> loaded = _library.Load(_listId);
            if (!loaded.IsSuccess) return word;
            foreach (WordEntry w in loaded.Value.Words)
                if (w.Key == word.Key) return w;
            return word;
        }

        // Back mid-session also lands here: results of what was answered, or just a refresh.
        private void FinishStudy()
        {
            WordStudySession session = _session;
            _session = null;
            if (session.Answered == 0)
            {
                Render();
                return;
            }
            _navigator.Push(ScreenId.Stats, session.BuildStats());
        }

        // ---------- Add words (S14) ----------

        private void OnPasteSubmitted(string text)
        {
            RepoResult<WordImportPlan> prepared = _library.PrepareImport(_listId, text);
            if (!prepared.IsSuccess)
            {
                _addWords.ShowError(UiStrings.WordListOpenFailed);
                return;
            }

            WordImportPlan plan = prepared.Value;
            switch (plan.Error)
            {
                case WordParseError.Empty: _addWords.ShowError(UiStrings.PasteEmpty); return;
                case WordParseError.NoTabs: _addWords.ShowError(UiStrings.PasteNoTabs); return;
            }
            if (plan.Incoming.Count == 0)
            {
                _addWords.ShowError(UiStrings.PasteNoWords(plan.InvalidLines));
                return;
            }

            if (!plan.HasDuplicates)
            {
                Apply(plan, false);
                return;
            }
            // Stacked over the paste popup: Cancel goes back to it with the text intact.
            _duplicates.Show(plan.Duplicates, choice =>
            {
                if (choice == DuplicateChoice.Cancel) return;
                Apply(plan, choice == DuplicateChoice.Update);
            });
        }

        private void Apply(WordImportPlan plan, bool update)
        {
            RepoResult<WordImportOutcome> result = _library.ApplyImport(_listId, plan, update);
            if (!result.IsSuccess)
            {
                _addWords.ShowError(UiStrings.DifficultSaveFailed);
                return;
            }
            _addWords.Close();
            Render();
            _toast.Show(UiStrings.ImportSummary(result.Value.Added, result.Value.Updated, result.Value.Ignored, result.Value.InvalidLines));
        }
    }
}
