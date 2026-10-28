using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    public sealed class HomeScreen : ScreenView
    {
        [SerializeField] private Button _storyListButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private SideMenu _sideMenu;
        [SerializeField] private Button _loadStoriesButton;
        [SerializeField] private Button _difficultListButton;
        [SerializeField] private Button _exitButton;

        public void Init(ScreenNavigator navigator, Action quit)
        {
            _storyListButton.onClick.AddListener(() => navigator.Push(ScreenId.StoryList));
            _menuButton.onClick.AddListener(_sideMenu.Open);
            _loadStoriesButton.onClick.AddListener(() => FromMenu(() => navigator.Push(ScreenId.LoadStories)));
            _difficultListButton.onClick.AddListener(() => FromMenu(() => navigator.Push(ScreenId.DifficultList)));
            _exitButton.onClick.AddListener(() => FromMenu(quit));
        }

        public override void OnHide()
        {
            _sideMenu.CloseImmediate();
            base.OnHide();
        }

        public override bool HandleBack()
        {
            if (!_sideMenu.IsOpen) return false;
            _sideMenu.Close();
            return true;
        }

        private void FromMenu(Action action)
        {
            _sideMenu.CloseImmediate();
            action?.Invoke();
        }
    }
}
