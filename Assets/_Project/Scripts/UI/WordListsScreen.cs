using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using Kanji.Core.Words;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>All word lists (names only) plus "Add list". Re-renders only when the library revision changed.</summary>
    public sealed class WordListsScreen : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _addButton;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private SimpleListRow _rowPrefab;
        [SerializeField] private TMP_Text _emptyMessage;

        private readonly List<SimpleListRow> _rows = new List<SimpleListRow>();
        private ScreenNavigator _navigator;
        private WordListLibrary _library;
        private NameInputPopup _namePopup;
        private ToastView _toast;
        private int _renderedRevision = -1;

        public void Init(ScreenNavigator navigator, WordListLibrary library, NameInputPopup namePopup, ToastView toast)
        {
            _navigator = navigator;
            _library = library;
            _namePopup = namePopup;
            _toast = toast;
            _backButton.onClick.AddListener(() => _navigator.Pop());
            _addButton.onClick.AddListener(AskCreate);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            if (_library != null && _library.Revision != _renderedRevision) Render();
        }

        private void Render()
        {
            var savedY = _content.anchoredPosition.y;
            var firstRender = _renderedRevision < 0;
            _renderedRevision = _library.Revision;

            IReadOnlyList<WordListInfo> items = _library.List().Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _content, false));
                var info = items[i];
                _rows[i].Bind(info.Name, UiStrings.WordCount(info.WordCount), () => Open(info));
                _rows[i].gameObject.SetActive(true);
                _rows[i].transform.SetSiblingIndex(i);
            }
            for (var i = items.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            var empty = items.Count == 0;
            _listScroll.gameObject.SetActive(!empty);
            _emptyMessage.gameObject.SetActive(empty);
            _emptyMessage.text = UiStrings.EmptyWordLists;
            if (empty) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var max = Mathf.Max(0f, _content.rect.height - _listScroll.viewport.rect.height);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, firstRender ? 0f : Mathf.Clamp(savedY, 0f, max));
        }

        private void Open(WordListInfo info) => _navigator.Push(ScreenId.WordList, info.Id);

        private void AskCreate()
        {
            var request = new NameInputRequest
            {
                Title = UiStrings.NewWordListTitle,
                Placeholder = UiStrings.WordListNamePlaceholder,
                InitialText = string.Empty,
                Stats = null,
                Validate = text => _library.Check(text),
                DuplicateFormat = UiStrings.WordListNameDuplicate,
            };
            _namePopup.Show(request, Create);
        }

        // Returns null to close the popup, or an inline error to keep it open.
        private string Create(string name)
        {
            RepoResult<WordListInfo> result = _library.Create(name);
            if (!result.IsSuccess)
            {
                return result.Error == RepoError.NameDuplicate
                    ? string.Format(UiStrings.WordListNameDuplicate, result.Message)
                    : UiStrings.DifficultSaveFailed;
            }
            Render();
            _toast.Show(string.Format(UiStrings.WordListCreated, result.Value.Name));
            return null;
        }
    }
}
