using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Left side drawer: slides the panel in over a scrim; tapping the scrim closes it.</summary>
    public sealed class SideMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _scrim;
        [SerializeField] private Button _scrimButton;
        [SerializeField] private float _duration = 0.2f;

        private Coroutine _anim;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _scrimButton.onClick.AddListener(Close);
            Apply(0f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            gameObject.SetActive(true);
            Animate(1f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Animate(0f);
        }

        /// <summary>Closes without animation (e.g. when leaving the screen).</summary>
        public void CloseImmediate()
        {
            IsOpen = false;
            if (_anim != null) StopCoroutine(_anim);
            _anim = null;
            Apply(0f);
            gameObject.SetActive(false);
        }

        private void Animate(float target)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Slide(target));
        }

        private IEnumerator Slide(float target)
        {
            float from = _scrim.alpha;
            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                Apply(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / _duration)));
                yield return null;
            }
            Apply(target);
            _anim = null;
            if (target <= 0f) gameObject.SetActive(false);
        }

        // 0 = hidden (panel off-screen to the left), 1 = fully open.
        private void Apply(float k)
        {
            _scrim.alpha = k;
            _scrim.blocksRaycasts = k > 0f;
            _panel.anchoredPosition = new Vector2(-_panel.rect.width * (1f - k), 0f);
        }
    }
}
