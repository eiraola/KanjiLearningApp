using System;
using System.Collections;
using UnityEngine;

namespace Kanji.UI
{
    /// <summary>
    /// Base class for popups living in PopupLayer. Activated/deactivated by <see cref="PopupService"/>,
    /// which also drives its fades (unscaled time) and hides it while another popup is stacked on top.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupView : MonoBehaviour
    {
        private CanvasGroup _group;
        private Coroutine _fade;
        private CanvasGroup _companion;
        private float _companionTarget;
        private float _target = 1f;
        private Action _onDone;

        protected PopupService Popups { get; private set; }

        /// <summary>When false, tapping the scrim does nothing (only Back cancels).</summary>
        public virtual bool CloseOnOutsideTap => true;

        /// <summary>Result "cancel" plus close.</summary>
        public abstract void Cancel();

        public void Init(PopupService popups)
        {
            Popups = popups;
            gameObject.SetActive(false);
        }

        private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        /// <summary>Alpha 0 -> 1, interactable. <paramref name="companion"/> (scrim) fades along. seconds &lt;= 0: immediate.</summary>
        internal void FadeIn(float seconds, CanvasGroup companion)
        {
            CancelFade();
            Group.interactable = true;
            Group.blocksRaycasts = true;
            Run(0f, 1f, seconds, companion, 1f, null);
        }

        /// <summary>Alpha -> 0 with no interaction, then <paramref name="onDone"/> (also if the object gets disabled meanwhile).</summary>
        internal void FadeOut(float seconds, CanvasGroup companion, Action onDone)
        {
            CancelFade();
            Group.interactable = false;
            Group.blocksRaycasts = false;
            Run(Group.alpha, 0f, seconds, companion, 0f, onDone);
        }

        /// <summary>Hides (without deactivating) or restores a popup that has another one stacked above it.</summary>
        internal void SetCovered(bool covered, float seconds = 0f)
        {
            if (covered)
            {
                CancelFade();
                Group.alpha = 0f;
                Group.interactable = false;
                Group.blocksRaycasts = false;
            }
            else
            {
                FadeIn(seconds, null);
            }
        }

        private void Run(float from, float to, float seconds, CanvasGroup companion, float companionTo, Action onDone)
        {
            _target = to;
            _companion = companion;
            _companionTarget = companionTo;
            _onDone = onDone;
            Group.alpha = from;
            if (seconds <= 0f || !isActiveAndEnabled)
            {
                Finish();
                return;
            }
            _fade = StartCoroutine(FadeRoutine(from, to, seconds));
        }

        private IEnumerator FadeRoutine(float from, float to, float seconds)
        {
            float companionFrom = _companion != null ? _companion.alpha : 0f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                Group.alpha = Mathf.Lerp(from, to, k);
                if (_companion != null) _companion.alpha = Mathf.Lerp(companionFrom, _companionTarget, k);
                yield return null;
            }
            _fade = null;
            Finish();
        }

        private void Finish()
        {
            Group.alpha = _target;
            if (_companion != null) _companion.alpha = _companionTarget;
            _companion = null;
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        /// <summary>Stops a running fade and drops its pending callback (e.g. reopening during a fade-out).</summary>
        private void CancelFade()
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = null;
            _onDone = null;
            _companion = null;
        }

        private void OnDisable()
        {
            if (_fade == null) return;
            _fade = null;
            Finish();
        }
    }
}
