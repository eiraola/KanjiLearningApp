using System;
using System.Collections;
using System.Collections.Generic;
using Kanji.Core.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>Fade-to-black between screens. Queues swaps and blocks input through the root CanvasGroup.</summary>
    public sealed class TransitionOverlay : MonoBehaviour, IScreenTransition
    {
        [SerializeField] private Image _fade;
        [SerializeField] private CanvasGroup _inputGroup;

        private readonly Queue<Action> _pending = new Queue<Action>();
        private InputGate _gate;
        private float _fadeOut;
        private float _fadeIn;
        private bool _running;

        public void Init(InputGate gate, float fadeOutSeconds, float fadeInSeconds)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _fadeOut = Mathf.Max(0f, fadeOutSeconds);
            _fadeIn = Mathf.Max(0f, fadeInSeconds);
            _gate.Blocked += OnBlocked;
            SetAlpha(0f);
        }

        public void Run(Action swap)
        {
            if (swap == null) return;
            _pending.Enqueue(swap);
            if (_running) return;
            _running = true;
            _gate.Hold();
            StartCoroutine(Process());
        }

        private IEnumerator Process()
        {
            while (_pending.Count > 0)
            {
                yield return Fade(0f, 1f, _fadeOut);
                var swap = _pending.Dequeue();
                try { swap(); }
                catch (Exception e) { Debug.LogException(e, this); }
                yield return Fade(1f, 0f, _fadeIn);
            }
            _running = false;
            _gate.Release();
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            if (seconds <= 0f) { SetAlpha(to); yield break; }
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            if (_fade == null) return;
            var c = _fade.color;
            c.a = a;
            _fade.color = c;
        }

        private void OnBlocked()
        {
            if (_inputGroup != null) _inputGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_gate == null || _inputGroup == null) return;
            if (!_inputGroup.blocksRaycasts && !_gate.IsBlocked) _inputGroup.blocksRaycasts = true;
        }

        private void OnDestroy()
        {
            if (_gate != null) _gate.Blocked -= OnBlocked;
        }
    }
}
