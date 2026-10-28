using System;
using Kanji.Core.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kanji.UI
{
    /// <summary>Routes the Android Back button (Escape in Input System): screen, then Pop, then quit.</summary>
    public sealed class BackButtonRouter : MonoBehaviour
    {
        private ScreenNavigator _navigator;
        private PopupService _popups;
        private Action _quit;
        private InputGate _gate;

        public void Init(ScreenNavigator navigator, PopupService popups, Action quit, InputGate gate)
        {
            _navigator = navigator;
            _popups = popups;
            _quit = quit;
            _gate = gate;
        }

        private void Update()
        {
            if (_navigator == null) return;
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (_gate != null && _gate.IsBlocked) return;

            if (_popups != null && _popups.HandleBack()) return;
            if (_navigator.Current != null && _navigator.Current.HandleBack()) return;
            if (!_navigator.Pop()) _quit?.Invoke();
        }
    }
}
