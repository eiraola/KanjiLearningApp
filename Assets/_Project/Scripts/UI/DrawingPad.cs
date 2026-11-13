using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kanji.UI
{
    /// <summary>
    /// Finger drawing area (S17, reverse study). Paints round strokes into a texture shown by the RawImage.
    /// It handles its own drags, so drawing never moves the card (CardSwipe) or scrolls the body.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class DrawingPad : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Color _ink = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private float _brushRadius = 5f;   // in texture pixels
        [SerializeField] private int _textureHeight = 360;  // width follows the rect's aspect

        private RawImage _image;
        private Texture2D _texture;
        private Color32[] _pixels;
        private Vector2 _last;
        private bool _hasLast;
        private bool _dirty;

        /// <summary>Number of painted texels (tests).</summary>
        public int InkedPixels
        {
            get
            {
                if (_pixels == null) return 0;
                int n = 0;
                for (int i = 0; i < _pixels.Length; i++) if (_pixels[i].a > 0) n++;
                return n;
            }
        }

        private RawImage Image => _image != null ? _image : (_image = GetComponent<RawImage>());

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }

        /// <summary>Wipes the drawing (between cards and from the "Clear" button).</summary>
        public void Clear()
        {
            _hasLast = false;
            if (!EnsureTexture()) return;
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = clear;
            _dirty = true;
            Flush();
        }

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false; // ink starts at once

        public void OnPointerDown(PointerEventData e)
        {
            if (!TryTexel(e, out Vector2 p)) return;
            Stamp(p);
            _last = p;
            _hasLast = true;
            Flush();
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (!TryTexel(e, out Vector2 p)) return;
            if (_hasLast) Line(_last, p); else Stamp(p);
            _last = p;
            _hasLast = true;
            Flush();
        }

        public void OnEndDrag(PointerEventData e) => _hasLast = false;

        private bool TryTexel(PointerEventData e, out Vector2 texel)
        {
            texel = default;
            if (!EnsureTexture()) return false;
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 local))
                return false;
            Vector2 n = Rect.PointToNormalized(rt.rect, local); // clamped to 0..1
            texel = new Vector2(n.x * (_texture.width - 1), n.y * (_texture.height - 1));
            return true;
        }

        private bool EnsureTexture()
        {
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 1f || rect.height <= 1f) return _texture != null;
            int h = Mathf.Max(16, _textureHeight);
            int w = Mathf.Clamp(Mathf.RoundToInt(h * rect.width / rect.height), 16, 2048);
            if (_texture != null && _texture.width == w && _texture.height == h) return true;

            if (_texture != null) Destroy(_texture);
            _texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "DrawingPad",
            };
            _pixels = new Color32[w * h];
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            Image.texture = _texture;
            Image.color = Color.white;
            return true;
        }

        private void Line(Vector2 from, Vector2 to)
        {
            float step = Mathf.Max(1f, _brushRadius * 0.5f);
            int count = Mathf.CeilToInt(Vector2.Distance(from, to) / step);
            for (int i = 1; i <= count; i++) Stamp(Vector2.Lerp(from, to, i / (float)count));
            if (count == 0) Stamp(to);
        }

        private void Stamp(Vector2 c)
        {
            int w = _texture.width, h = _texture.height;
            int r = Mathf.CeilToInt(_brushRadius);
            float r2 = _brushRadius * _brushRadius;
            Color32 ink = _ink;
            int x0 = Mathf.Max(0, (int)c.x - r), x1 = Mathf.Min(w - 1, (int)c.x + r);
            int y0 = Mathf.Max(0, (int)c.y - r), y1 = Mathf.Min(h - 1, (int)c.y + r);
            for (int y = y0; y <= y1; y++)
            {
                float dy = y - c.y;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - c.x;
                    if (dx * dx + dy * dy <= r2) _pixels[y * w + x] = ink;
                }
            }
            _dirty = true;
        }

        private void Flush()
        {
            if (!_dirty || _texture == null) return;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            _dirty = false;
        }
    }
}
