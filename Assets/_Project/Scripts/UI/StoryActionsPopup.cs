using System;
using Kanji.Core.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public enum StoryAction { Read, Edit, Delete, Dismiss }

    /// <summary>Popup with the actions of a story. Read/Dismiss close it; Delete leaves it open under the confirmation.</summary>
    public sealed class StoryActionsPopup : PopupView
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private Button _btnRead;
        [SerializeField] private Button _btnEdit;
        [SerializeField] private Button _btnDelete;
        [SerializeField] private Button _btnClose;

        private Action<StoryAction> _onResult;
        private bool _resolved;

        private void Awake()
        {
            _btnRead.onClick.AddListener(() => Resolve(StoryAction.Read));
            _btnEdit.onClick.AddListener(() => Resolve(StoryAction.Edit));
            _btnDelete.onClick.AddListener(() => Resolve(StoryAction.Delete));
            _btnClose.onClick.AddListener(Cancel);
        }

        public void Show(StoryInfo story, Action<StoryAction> onResult)
        {
            _onResult = onResult;
            _resolved = false;
            Refresh(story);
            Popups.Open(this);
        }

        /// <summary>Updates the shown story data in place (e.g. after a rename).</summary>
        public void Refresh(StoryInfo story)
        {
            _name.text = story.Name;
            _count.text = UiStrings.KanjiCount(story.EntryCount);
        }

        public void Close() => Popups.Close(this);

        public override void Cancel() => Resolve(StoryAction.Dismiss);

        private void Resolve(StoryAction action)
        {
            if (action == StoryAction.Delete || action == StoryAction.Edit)
            {
                _onResult?.Invoke(action); // stays open; the dialog stacks above
                return;
            }
            if (_resolved) return;
            _resolved = true;
            Close();
            _onResult?.Invoke(action);
        }
    }
}
