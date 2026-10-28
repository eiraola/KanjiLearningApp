using System.Collections.Generic;
using Kanji.Core.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Stack of popups in PopupLayer. A single full-screen blocker (scrim) is kept just below the top popup.
    /// Popups below the top one are hidden (not deactivated) and come back intact when it closes.
    /// </summary>
    public sealed class PopupService
    {
        private readonly List<PopupView> _stack = new List<PopupView>(4);
        private readonly GameObject _blocker;
        private readonly CanvasGroup _blockerGroup;
        private readonly Transform _keepOnTop;
        private readonly InputGate _gate;
        private readonly float _fade;

        /// <param name="blocker">Full-screen scrim with an Image and a Button (and optionally a CanvasGroup to fade it).</param>
        /// <param name="keepOnTop">Optional transform (toast) that must stay above popups and blocker.</param>
        /// <param name="gate">Optional: input is blocked while a popup fades.</param>
        /// <param name="fadeSeconds">Fade duration; 0 makes everything immediate.</param>
        public PopupService(GameObject blocker, Transform keepOnTop = null, InputGate gate = null, float fadeSeconds = 0f)
        {
            _blocker = blocker;
            _blockerGroup = blocker.GetComponent<CanvasGroup>();
            _keepOnTop = keepOnTop;
            _gate = gate;
            _fade = Mathf.Max(0f, fadeSeconds);
            _blocker.SetActive(false);
            var button = _blocker.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnBlockerTapped);
        }

        public bool HasOpen => _stack.Count > 0;

        public void Open(PopupView popup)
        {
            if (popup == null || _stack.Contains(popup)) return;
            _gate?.BlockFor(_fade);
            bool first = _stack.Count == 0;
            if (_stack.Count > 0) _stack[_stack.Count - 1].SetCovered(true);
            _stack.Add(popup);
            popup.gameObject.SetActive(true);
            if (first && !_blocker.activeSelf)
            {
                if (_blockerGroup != null) _blockerGroup.alpha = 0f;
                _blocker.SetActive(true);
            }
            Reorder();
            popup.FadeIn(_fade, first ? _blockerGroup : null);
        }

        public void Close(PopupView popup)
        {
            int index = _stack.IndexOf(popup);
            if (popup == null || index < 0) return;
            bool wasTop = index == _stack.Count - 1;
            _stack.RemoveAt(index);
            _gate?.BlockFor(_fade);
            bool last = _stack.Count == 0;
            if (wasTop && !last) _stack[_stack.Count - 1].SetCovered(false, _fade);
            Reorder();
            popup.transform.SetAsLastSibling();
            if (_keepOnTop != null) _keepOnTop.SetAsLastSibling();
            popup.FadeOut(_fade, last ? _blockerGroup : null, () => OnClosed(popup));
        }

        /// <summary>Consumes Back for exactly one level: cancels the top popup.</summary>
        public bool HandleBack()
        {
            if (_stack.Count == 0) return false;
            if (_gate != null && _gate.IsBlocked) return true;
            _stack[_stack.Count - 1].Cancel();
            return true;
        }

        private void OnClosed(PopupView popup)
        {
            popup.gameObject.SetActive(false);
            if (_stack.Count == 0) _blocker.SetActive(false);
        }

        private void OnBlockerTapped()
        {
            if (_stack.Count == 0) return;
            if (_gate != null && _gate.IsBlocked) return;
            var top = _stack[_stack.Count - 1];
            if (top.CloseOnOutsideTap) top.Cancel();
        }

        private void Reorder()
        {
            if (_stack.Count > 0)
            {
                // Order: ... blocker, popup(n-2), ..., popup(n-1) ; blocker sits below the top popup only.
                // Push lower popups, then blocker, then top popup to the end, in that order.
                for (int i = 0; i < _stack.Count - 1; i++)
                    _stack[i].transform.SetAsLastSibling();
                _blocker.transform.SetAsLastSibling();
                _stack[_stack.Count - 1].transform.SetAsLastSibling();
            }
            if (_keepOnTop != null) _keepOnTop.SetAsLastSibling();
        }
    }
}
