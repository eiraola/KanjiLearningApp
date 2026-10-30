using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Horizontal swipe on a popup card: past the threshold it flies out and raises SwipedGreen (right) or SwipedRed (left);
    /// otherwise it springs back. Mostly-vertical drags are left to the body ScrollRect.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private Image _tintTarget;
        [SerializeField] private Color _greenTint = new Color(0.482f, 0.847f, 0.561f, 1f);
        [SerializeField] private Color _redTint = new Color(0.902f, 0.361f, 0.361f, 1f);
        [SerializeField, Range(0.1f, 0.9f)] private float _thresholdFraction = 0.3f;
        [SerializeField] private float _maxAngle = 8f;
        [SerializeField, Range(0f, 1f)] private float _maxTint = 0.35f;
        [SerializeField] private float _returnSeconds = 0.15f;
        [SerializeField] private float _exitSeconds = 0.15f;

        public event Action SwipedGreen;
        public event Action SwipedRed;

        /// <summary>Gate checked when a drag starts (e.g. "answer row visible").</summary>
        public Func<bool> CanSwipe { get; set; }

        private RectTransform _rect;
        private Vector2 _home;
        private Color _baseColor;
        private Vector2 _startLocal;
        private float _offset;
        private bool _dragging;
        private bool _busy;
        private bool _scrollLocked;

        private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);
        private float Width => Mathf.Max(1f, Rect.rect.width);
        private float Threshold => Width * _thresholdFraction;

        private bool _ready;

        private void Awake() => EnsureReady();

        // ResetPose can run before Awake (KanjiPopup fills itself while still inactive).
        private void EnsureReady()
        {
            if (_ready) return;
            _ready = true;
            _home = Rect.anchoredPosition;
            if (_tintTarget == null) _tintTarget = GetComponent<Image>();
            if (_tintTarget != null) _baseColor = _tintTarget.color;
            if (_scroll != null)
            {
                var relay = _scroll.GetComponent<CardSwipeRelay>();
                if (relay == null) relay = _scroll.gameObject.AddComponent<CardSwipeRelay>();
                relay.Target = this;
            }
        }

        private void OnDisable() => ResetPose();

        /// <summary>Back to the centre, untinted, cancelling any drag or animation (and its pending answer).</summary>
        public void ResetPose()
        {
            EnsureReady();
            StopAllCoroutines();
            _busy = false;
            EndScrollLock();
            _dragging = false;
            Apply(0f);
        }

        public void OnBeginDrag(PointerEventData e) => Begin(e, false);
        public void OnDrag(PointerEventData e) => Move(e);
        public void OnEndDrag(PointerEventData e) => End(e);

        internal void Begin(PointerEventData e, bool fromScroll)
        {
            if (_dragging || _busy || e.button != PointerEventData.InputButton.Left) return;
            if (CanSwipe != null && !CanSwipe()) return;
            var moved = e.position - e.pressPosition;
            if (Mathf.Abs(moved.x) <= Mathf.Abs(moved.y)) return; // vertical: the ScrollRect keeps it
            if (!ToLocal(e.pressPosition, e.pressEventCamera, out _startLocal)) return;
            _dragging = true;
            if (fromScroll && _scroll != null)
            {
                _scrollLocked = _scroll.vertical;
                _scroll.vertical = false; // ScrollRect gets the same events; freeze it for this drag
                _scroll.StopMovement();
            }
            Move(e);
        }

        internal void Move(PointerEventData e)
        {
            if (!_dragging || !ToLocal(e.position, e.pressEventCamera, out var p)) return;
            Apply(p.x - _startLocal.x);
        }

        internal void End(PointerEventData e)
        {
            if (!_dragging) return;
            Move(e);
            _dragging = false;
            EndScrollLock();
            float exitX = Mathf.Sign(_offset) * (ParentWidth() * 0.5f + Width);
            if (Mathf.Abs(_offset) >= Threshold) StartCoroutine(Animate(_offset, exitX, _exitSeconds, true));
            else StartCoroutine(Animate(_offset, 0f, _returnSeconds, false));
        }

        private IEnumerator Animate(float from, float to, float seconds, bool answer)
        {
            _busy = true;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            Apply(to);
            _busy = false;
            if (!answer) yield break;
            bool green = to > 0f;
            if (green) SwipedGreen?.Invoke();
            else SwipedRed?.Invoke();
            // The handler normally re-presents (ResetPose) or closes the popup (stays out until disabled).
            if (isActiveAndEnabled && Mathf.Abs(_offset) > 0f && (CanSwipe == null || CanSwipe())) ResetPose();
        }

        private void Apply(float offset)
        {
            _offset = offset;
            Rect.anchoredPosition = _home + new Vector2(offset, 0f);
            Rect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Clamp(offset / Width, -1f, 1f) * _maxAngle);
            if (_tintTarget == null) return;
            float k = Mathf.Clamp01(Mathf.Abs(offset) / Threshold) * _maxTint;
            _tintTarget.color = Color.Lerp(_baseColor, offset >= 0f ? _greenTint : _redTint, k);
        }

        private void EndScrollLock()
        {
            if (!_scrollLocked) return;
            _scrollLocked = false;
            if (_scroll != null) _scroll.vertical = true;
        }

        private float ParentWidth() => Rect.parent is RectTransform p ? p.rect.width : Width * 2f;

        private bool ToLocal(Vector2 screen, Camera cam, out Vector2 local)
        {
            var space = Rect.parent as RectTransform;
            if (space == null) { local = screen; return true; }
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screen, cam, out local);
        }
    }
}
