using System.Collections;
using TMPro;
using UnityEngine;

namespace Kanji.UI
{
    /// <summary>Non-blocking message toast. Shared; a new toast replaces the current one.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ToastView : MonoBehaviour
    {
        private const float FadeIn = 0.15f;
        private const float Visible = 3f;
        private const float FadeOut = 0.3f;

        [SerializeField] private TextMeshProUGUI _text;

        private CanvasGroup _group;
        private Coroutine _routine;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            gameObject.SetActive(true);
            _text.text = message;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            yield return Ramp(0f, 1f, FadeIn);
            float t = 0f;
            while (t < Visible) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Ramp(1f, 0f, FadeOut);
            gameObject.SetActive(false);
            _routine = null;
        }

        private IEnumerator Ramp(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
