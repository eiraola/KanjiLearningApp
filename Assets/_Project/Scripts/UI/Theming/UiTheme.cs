using TMPro;
using UnityEngine;

namespace Kanji.UI
{
    /// <summary>Single dark visual theme: palette, sprites, fonts, sizes and motion. Defaults ARE the dark theme.</summary>
    [CreateAssetMenu(menuName = "Kanji/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Colors")]
        [SerializeField] private Color _background = new Color32(0x14, 0x17, 0x1C, 0xFF);
        [SerializeField] private Color _surface = new Color32(0x1D, 0x22, 0x2A, 0xFF);
        [SerializeField] private Color _surfaceRaised = new Color32(0x27, 0x2D, 0x38, 0xFF);
        [SerializeField] private Color _border = new Color32(0x3A, 0x41, 0x50, 0xFF);
        [SerializeField] private Color _textPrimary = new Color32(0xE8, 0xEA, 0xF0, 0xFF);
        [SerializeField] private Color _textSecondary = new Color32(0xA3, 0xA9, 0xB8, 0xFF);
        [SerializeField] private Color _textOnAccent = Color.white;
        [SerializeField] private Color _primary = new Color32(0xC4, 0x30, 0x2B, 0xFF);
        [SerializeField] private Color _primaryPressed = new Color32(0xA3, 0x25, 0x20, 0xFF);
        [SerializeField] private Color _secondary = new Color32(0x27, 0x2D, 0x38, 0xFF);
        [SerializeField] private Color _secondaryPressed = new Color32(0x3A, 0x41, 0x50, 0xFF);
        [SerializeField] private Color _success = new Color32(0x1B, 0x7A, 0x3A, 0xFF);
        [SerializeField] private Color _successPressed = new Color32(0x14, 0x63, 0x2E, 0xFF);
        [SerializeField] private Color _danger = new Color32(0xC4, 0x30, 0x2B, 0xFF);
        [SerializeField] private Color _dangerPressed = new Color32(0xA3, 0x25, 0x20, 0xFF);
        [SerializeField] private Color _disabled = new Color32(0x27, 0x2D, 0x38, 0xFF);
        [SerializeField] private Color _disabledText = new Color32(0x6B, 0x71, 0x80, 0xFF);
        [SerializeField] private Color _link = new Color32(0x22, 0xD3, 0xEE, 0xFF);
        [SerializeField] private Color _error = new Color32(0xFF, 0x8A, 0x80, 0xFF);
        [SerializeField] private Color _warningSurface = new Color32(0x4B, 0x34, 0x10, 0xFF);
        [SerializeField] private Color _scrim = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color _fadeColor = Color.black;

        [Header("Sprites")]
        [SerializeField] private Sprite _buttonSprite;
        [SerializeField] private Sprite _panelSprite;
        [SerializeField] private Sprite _inputSprite;
        [SerializeField] private Sprite _dividerSprite;
        [SerializeField] private Sprite _circleSprite;
        [SerializeField] private Sprite _iconBack;
        [SerializeField] private Sprite _iconClose;
        [SerializeField] private Sprite _iconTrash;
        [SerializeField] private Sprite _iconCheck;
        [SerializeField] private Sprite _iconCross;
        [SerializeField] private Sprite _iconPlus;
        [SerializeField] private Sprite _iconBooks;
        [SerializeField] private Sprite _iconBook;
        [SerializeField] private float _slicedPpuMultiplier = 0.8f;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset _regular;
        [SerializeField] private TMP_FontAsset _bold;

        [Header("Sizes (px at 1080 reference)")]
        [SerializeField] private float _displaySize = 96f;
        [SerializeField] private float _titleSize = 56f;
        [SerializeField] private float _buttonSize = 52f;
        [SerializeField] private float _readingSize = 48f;
        [SerializeField] private float _bodySize = 44f;
        [SerializeField] private float _captionSize = 36f;
        [SerializeField] private float _kanjiMinSize = 96f;
        [SerializeField] private float _kanjiMaxSize = 220f;

        [Header("Motion (seconds)")]
        [SerializeField] private float _screenFadeOutSeconds = 0.2f;
        [SerializeField] private float _screenFadeInSeconds = 0.2f;
        [SerializeField] private float _popupFadeSeconds = 0.15f;

        public Color Background => _background;
        public Color Surface => _surface;
        public Color SurfaceRaised => _surfaceRaised;
        public Color Border => _border;
        public Color TextPrimary => _textPrimary;
        public Color TextSecondary => _textSecondary;
        public Color TextOnAccent => _textOnAccent;
        public Color Primary => _primary;
        public Color PrimaryPressed => _primaryPressed;
        public Color Secondary => _secondary;
        public Color SecondaryPressed => _secondaryPressed;
        public Color Success => _success;
        public Color SuccessPressed => _successPressed;
        public Color Danger => _danger;
        public Color DangerPressed => _dangerPressed;
        public Color Disabled => _disabled;
        public Color DisabledText => _disabledText;
        public Color Link => _link;
        public Color Error => _error;
        public Color WarningSurface => _warningSurface;
        public Color Scrim => _scrim;
        public Color FadeColor => _fadeColor;

        public Sprite ButtonSprite => _buttonSprite;
        public Sprite PanelSprite => _panelSprite;
        public Sprite InputSprite => _inputSprite;
        public Sprite DividerSprite => _dividerSprite;
        public Sprite CircleSprite => _circleSprite;
        public Sprite IconBack => _iconBack;
        public Sprite IconClose => _iconClose;
        public Sprite IconTrash => _iconTrash;
        public Sprite IconCheck => _iconCheck;
        public Sprite IconCross => _iconCross;
        public Sprite IconPlus => _iconPlus;
        public Sprite IconBooks => _iconBooks;
        public Sprite IconBook => _iconBook;
        public float SlicedPpuMultiplier => _slicedPpuMultiplier;

        public TMP_FontAsset Regular => _regular;
        public TMP_FontAsset Bold => _bold;

        public float DisplaySize => _displaySize;
        public float TitleSize => _titleSize;
        public float ButtonSize => _buttonSize;
        public float ReadingSize => _readingSize;
        public float BodySize => _bodySize;
        public float CaptionSize => _captionSize;
        public float KanjiMinSize => _kanjiMinSize;
        public float KanjiMaxSize => _kanjiMaxSize;

        public float ScreenFadeOutSeconds => _screenFadeOutSeconds;
        public float ScreenFadeInSeconds => _screenFadeInSeconds;
        public float PopupFadeSeconds => _popupFadeSeconds;

        /// <summary>Link color as #RRGGBB for TMP rich text.</summary>
        public string LinkHex => "#" + ColorUtility.ToHtmlStringRGB(_link);

#if UNITY_EDITOR
        private void OnValidate()
        {
            _screenFadeOutSeconds = Mathf.Max(0f, _screenFadeOutSeconds);
            _screenFadeInSeconds = Mathf.Max(0f, _screenFadeInSeconds);
            _popupFadeSeconds = Mathf.Max(0f, _popupFadeSeconds);
            WarnIfNull(_buttonSprite, nameof(ButtonSprite));
            WarnIfNull(_panelSprite, nameof(PanelSprite));
            WarnIfNull(_inputSprite, nameof(InputSprite));
            WarnIfNull(_dividerSprite, nameof(DividerSprite));
            WarnIfNull(_circleSprite, nameof(CircleSprite));
            WarnIfNull(_iconBack, nameof(IconBack));
            WarnIfNull(_iconClose, nameof(IconClose));
            WarnIfNull(_iconTrash, nameof(IconTrash));
            WarnIfNull(_iconCheck, nameof(IconCheck));
            WarnIfNull(_iconCross, nameof(IconCross));
            WarnIfNull(_regular, nameof(Regular));
            WarnIfNull(_bold, nameof(Bold));
        }

        private void WarnIfNull(Object value, string field)
        {
            if (value == null) Debug.LogWarning("[UiTheme] " + field + " is not assigned.", this);
        }
#endif
    }
}
