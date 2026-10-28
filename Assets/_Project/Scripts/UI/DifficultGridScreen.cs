using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Vertical list for one story's difficult list (kanji + progress circle). Tap a row to open the popup with "remove".
    /// "Study" shows every kanji in random order in the popup's study mode, then the stats screen.
    /// </summary>
    public sealed class DifficultGridScreen : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private DifficultKanjiCell _cellPrefab;
        [SerializeField] private TMP_Text _emptyMessage;
        [SerializeField] private Button _studyButton;

        private readonly List<DifficultKanjiCell> _cells = new List<DifficultKanjiCell>();
        private ScreenNavigator _navigator;
        private StoryLibrary _library;
        private DifficultKanjiStore _store;
        private KanjiPopup _popup;
        private string _storyId;
        private DifficultStudySession _session;

        internal DifficultStudySession Session => _session;

        public void Init(ScreenNavigator navigator, StoryLibrary library, DifficultKanjiStore store, KanjiPopup popup)
        {
            _navigator = navigator;
            _library = library;
            _store = store;
            _popup = popup;
            _backButton.onClick.AddListener(() => _navigator.Pop());
            if (_studyButton != null) _studyButton.onClick.AddListener(StartStudy);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            if (args is string id) _storyId = id;
            _title.text = StoryName(_storyId);
            Render(true);
        }

        private string StoryName(string id)
        {
            if (_library == null) return string.Empty;
            List<DifficultStoryItem> items = DifficultStorySelector.Build(_library, _store);
            for (var i = 0; i < items.Count; i++)
                if (items[i].Story.Id == id) return items[i].Story.Name;
            return string.Empty;
        }

        private void Render(bool resetScroll)
        {
            var savedY = _content.anchoredPosition.y;
            DifficultKanjiList list = _store.For(_storyId);
            IReadOnlyList<KanjiEntry> items = list.Items;
            var count = items.Count;

            // Newest first: the list appends, so iterate backwards.
            for (var i = 0; i < count; i++)
            {
                if (i == _cells.Count) _cells.Add(Instantiate(_cellPrefab, _content, false));
                var cell = _cells[i];
                KanjiEntry entry = items[count - 1 - i];
                cell.Bind(entry, list.ProgressOf(entry), OnCellClicked);
                cell.gameObject.SetActive(true);
                cell.transform.SetSiblingIndex(i);
            }
            for (var i = count; i < _cells.Count; i++) _cells[i].gameObject.SetActive(false);

            var empty = count == 0;
            _scroll.gameObject.SetActive(!empty);
            _emptyMessage.gameObject.SetActive(empty);
            _emptyMessage.text = UiStrings.EmptyDifficult;
            if (_studyButton != null) _studyButton.gameObject.SetActive(!empty);
            if (empty) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var max = Mathf.Max(0f, _content.rect.height - _scroll.viewport.rect.height);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, resetScroll ? 0f : Mathf.Clamp(savedY, 0f, max));
        }

        private void OnCellClicked(KanjiEntry entry) => _popup.ShowDifficult(entry, _storyId, OnRemoved);

        private void StartStudy()
        {
            if (_session != null) return;
            DifficultKanjiList list = _store.For(_storyId);
            if (list.Items.Count == 0) return;
            _session = new DifficultStudySession(list);
            _popup.ShowStudy(_session.Current, OnStudyAnswer);
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
                Debug.LogWarning("[Kanji] Could not save study progress.");
            if (_session.IsFinished)
            {
                FinishStudy();
                _popup.Close();
                return;
            }
            _popup.ShowStudy(_session.Current, OnStudyAnswer);
        }

        // Back mid-session also lands here: shows the stats of what was answered, or just refreshes the list.
        private void FinishStudy()
        {
            DifficultStudySession session = _session;
            _session = null; // the popup's own Dismissed callback is ignored from here
            if (session.Answered == 0)
            {
                Render(false);
                return;
            }
            _navigator.Push(ScreenId.Stats, session.BuildStats());
        }

        private void OnRemoved()
        {
            if (_store.For(_storyId).Items.Count == 0)
            {
                _navigator.Pop();
                return;
            }
            Render(false);
        }
    }
}
