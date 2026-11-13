using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public enum WordListMenuAction { AddWords, Study, StudyReverse }

    /// <summary>Settings menu of one word list: "Add words", "Study" (S15) and "Reverse study" (S16); more options will be added here.</summary>
    public sealed class WordListMenuPopup : PopupView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _btnAddWords;
        [SerializeField] private Button _btnStudy;
        [SerializeField] private Button _btnStudyReverse;
        [SerializeField] private Button _btnClose;

        private Action<WordListMenuAction> _onPick;
        private bool _open;

        private void Awake()
        {
            _btnAddWords.onClick.AddListener(() => Pick(WordListMenuAction.AddWords));
            if (_btnStudy != null) _btnStudy.onClick.AddListener(() => Pick(WordListMenuAction.Study));
            if (_btnStudyReverse != null) _btnStudyReverse.onClick.AddListener(() => Pick(WordListMenuAction.StudyReverse));
            _btnClose.onClick.AddListener(Cancel);
        }

        /// <param name="canStudy">False (empty list) disables "Study".</param>
        public void Show(string listName, bool canStudy, Action<WordListMenuAction> onPick)
        {
            _title.text = listName ?? string.Empty;
            if (_btnStudy != null) _btnStudy.interactable = canStudy;
            if (_btnStudyReverse != null) _btnStudyReverse.interactable = canStudy;
            _onPick = onPick;
            _open = true;
            Popups.Open(this);
        }

        public override void Cancel()
        {
            if (!_open) return;
            _open = false;
            _onPick = null;
            Popups.Close(this);
        }

        // Closes first so the chosen option's popup opens on its own, not stacked over the menu.
        private void Pick(WordListMenuAction action)
        {
            if (!_open) return;
            var callback = _onPick;
            Cancel();
            callback?.Invoke(action);
        }
    }
}
