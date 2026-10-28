using System;
using System.Collections.Generic;
using Kanji.Core.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Library list. Re-renders only when the library revision changed, so scroll survives navigation.</summary>
    public sealed class StoryListScreen : ScreenView
    {
        private const float WarningScrollTop = 136f;

        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private StoryListRow _rowPrefab;
        [SerializeField] private GameObject _warning;
        [SerializeField] private TMP_Text _warningText;
        [SerializeField] private TMP_Text _emptyMessage;
        [SerializeField] private UiTheme _theme;

        private readonly List<StoryListRow> _rows = new List<StoryListRow>();
        private ScreenNavigator _navigator;
        private StoryLibrary _library;
        private DifficultKanjiStore _difficultStore;
        private int _renderedRevision = -1;
        private StoryActionsPopup _actionsPopup;
        private ConfirmPopup _confirmPopup;
        private NameInputPopup _namePopup;
        private ToastView _toast;

        public void Init(ScreenNavigator navigator, StoryLibrary library, DifficultKanjiStore difficultStore,
            StoryActionsPopup actionsPopup, ConfirmPopup confirmPopup, NameInputPopup namePopup, ToastView toast)
        {
            if (_theme == null) Debug.LogError("StoryListScreen: UiTheme not assigned.", this);
            _navigator = navigator;
            _library = library;
            _difficultStore = difficultStore;
            _actionsPopup = actionsPopup;
            _confirmPopup = confirmPopup;
            _namePopup = namePopup;
            _toast = toast;
            _backButton.onClick.AddListener(() => _navigator.Pop());
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            if (_library != null && _library.Revision != _renderedRevision) Render();
        }

        private void Render()
        {
            StoryListing listing;
            try
            {
                listing = _library.List();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowLoadFailed();
                return; // _renderedRevision untouched: retry on next OnShow
            }

            var savedY = _content.anchoredPosition.y;
            var firstRender = _renderedRevision < 0;
            _renderedRevision = _library.Revision;

            var items = listing.Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _content, false));
                var row = _rows[i];
                row.Bind(items[i], OnRowClicked);
                row.gameObject.SetActive(true);
                row.transform.SetSiblingIndex(i);
            }
            for (var i = items.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            var hasWarning = listing.UnreadableCount > 0;
            _warning.SetActive(hasWarning);
            if (hasWarning) _warningText.text = UiStrings.UnreadableStories(listing.UnreadableCount);
            _listScroll.GetComponent<RectTransform>().offsetMax = new Vector2(0f, hasWarning ? -WarningScrollTop : 0f);

            var empty = items.Count == 0;
            _listScroll.gameObject.SetActive(!empty);
            _emptyMessage.gameObject.SetActive(empty);
            _emptyMessage.color = _theme.TextSecondary;
            _emptyMessage.text = UiStrings.EmptyLibrary;

            if (empty) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var max = Mathf.Max(0f, _content.rect.height - _listScroll.viewport.rect.height);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, firstRender ? 0f : Mathf.Clamp(savedY, 0f, max));
        }

        private void ShowLoadFailed()
        {
            _warning.SetActive(false);
            _listScroll.gameObject.SetActive(false);
            for (var i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
            _emptyMessage.gameObject.SetActive(true);
            _emptyMessage.color = _theme.Error;
            _emptyMessage.text = UiStrings.LibraryLoadFailed;
        }

        private void OnRowClicked(StoryInfo info)
        {
            _actionsPopup.Show(info, action =>
            {
                if (action == StoryAction.Read) _navigator.Push(ScreenId.Reader, info);
                else if (action == StoryAction.Edit) AskRename(info);
                else if (action == StoryAction.Delete) AskDelete(info);
            });
        }

        private void AskRename(StoryInfo info)
        {
            var request = new NameInputRequest
            {
                Title = UiStrings.EditNameTitle,
                Placeholder = UiStrings.NameTitle,
                InitialText = info.Name,
                Stats = null,
                Validate = text => _library.Check(text, null, info.Id),
            };
            _namePopup.Show(request, name => ApplyRename(info, name));
        }

        // Returns null to close the edit popup, or an inline error text to keep it open.
        private string ApplyRename(StoryInfo info, string name)
        {
            var result = _library.Rename(info.Id, name);
            if (!result.IsSuccess)
            {
                if (result.Error == RepoError.NameDuplicate)
                    return string.Format(UiStrings.NameDuplicate, result.Message);
                _toast.Show(UiStrings.DifficultSaveFailed);
                return UiStrings.DifficultSaveFailed;
            }
            Render();
            _actionsPopup.Refresh(FindInfo(info.Id) ?? info);
            _toast.Show(UiStrings.NameUpdated);
            return null;
        }

        private StoryInfo FindInfo(string id)
        {
            var items = _library.List().Items;
            for (var i = 0; i < items.Count; i++)
                if (items[i].Id == id) return items[i];
            return null;
        }

        private void AskDelete(StoryInfo info)
        {
            var message = string.Format(UiStrings.ConfirmDelete, info.Name);
            var difficultCount = _difficultStore.Count(info.Id);
            if (difficultCount > 0) message += " " + string.Format(UiStrings.AlsoDeleteDifficult, difficultCount);
            _confirmPopup.Show(message, UiStrings.Delete, confirmed =>
            {
                if (confirmed) DeleteStory(info);
                // Cancel: only the confirmation closed; the story popup stays open.
            });
        }

        private void DeleteStory(StoryInfo info)
        {
            var result = _library.Delete(info.Id);
            _actionsPopup.Close();
            if (result.IsSuccess || result.Error == RepoError.NotFound)
            {
                Render();
                _toast.Show(string.Format(UiStrings.StoryDeleted, info.Name));
            }
            else
            {
                _toast.Show(UiStrings.DeleteFailed);
            }
        }
    }
}
