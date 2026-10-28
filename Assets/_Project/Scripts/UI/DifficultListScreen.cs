using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Selector: stories with at least one difficult kanji. Re-renders only when something changed.</summary>
    public sealed class DifficultListScreen : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private StoryListRow _rowPrefab;
        [SerializeField] private TMP_Text _emptyMessage;

        private readonly List<StoryListRow> _rows = new List<StoryListRow>();
        private ScreenNavigator _navigator;
        private StoryLibrary _library;
        private DifficultKanjiStore _store;
        private Action<string> _onStorySelected;
        private int _renderedRevision = -1;
        private int _renderedLibraryRevision = -1;

        /// <param name="onStorySelected">Opens the grid for a story id (wired by T42c).</param>
        public void Init(ScreenNavigator navigator, StoryLibrary library, DifficultKanjiStore store, Action<string> onStorySelected = null)
        {
            _navigator = navigator;
            _library = library;
            _store = store;
            _onStorySelected = onStorySelected;
            _backButton.onClick.AddListener(() => _navigator.Pop());
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            Refresh();
        }

        /// <summary>Re-renders when the lists or the library changed while this screen was hidden.</summary>
        public void Refresh()
        {
            if (_store == null) return;
            if (_store.Revision == _renderedRevision && _library.Revision == _renderedLibraryRevision) return;
            Render();
        }

        private void Render()
        {
            var savedY = _content.anchoredPosition.y;
            var firstRender = _renderedRevision < 0;
            _renderedRevision = _store.Revision;
            _renderedLibraryRevision = _library.Revision;

            List<DifficultStoryItem> items = DifficultStorySelector.Build(_library, _store);
            var count = items.Count;
            for (var i = 0; i < count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _content, false));
                var row = _rows[i];
                row.Bind(items[i].Story, UiStrings.DifficultCount(items[i].Count), OnRowClicked);
                row.gameObject.SetActive(true);
                row.transform.SetSiblingIndex(i);
            }
            for (var i = count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            var empty = count == 0;
            _listScroll.gameObject.SetActive(!empty);
            _emptyMessage.gameObject.SetActive(empty);
            _emptyMessage.text = UiStrings.EmptyDifficult;
            if (empty) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var max = Mathf.Max(0f, _content.rect.height - _listScroll.viewport.rect.height);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, firstRender ? 0f : Mathf.Clamp(savedY, 0f, max));
        }

        private void OnRowClicked(StoryInfo story) => _onStorySelected?.Invoke(story.Id);
    }
}
