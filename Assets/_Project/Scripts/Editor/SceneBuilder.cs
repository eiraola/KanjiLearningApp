using System.IO;
using Kanji.App;
using Kanji.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kanji.Editor
{
    /// <summary>Builds the S01 scene and screen prefabs from code (reproducible).</summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ScreensDir = "Assets/_Project/Prefabs/Screens";
        internal const string CoverPath = "Assets/_Project/Art/Cover/KanjiBook_Cutout.png";


        private enum ButtonStyle { Primary, Secondary, Success, Danger }

        private static UiTheme _theme;
        private static TMP_FontAsset _regular, _bold;

        [MenuItem("Kanji/Build S01 Scene")]
        public static void Build()
        {
            EnsureFolders();
            FontSetup.Setup();
            DarkUiAssetSetup.Setup();
            _theme = AssetDatabase.LoadAssetAtPath<UiTheme>(DarkUiAssetSetup.ThemePath);
            if (_theme == null)
            {
                Debug.LogError("[Kanji] UiTheme not found at " + DarkUiAssetSetup.ThemePath);
                return;
            }
            _regular = _theme.Regular;
            _bold = _theme.Bold;

            var home = BuildHomePrefab();
            var load = BuildLoadStoriesPrefab();
            var list = BuildStoryListPrefab();
            var difficult = BuildDifficultListPrefab();
            var difficultGrid = BuildDifficultGridPrefab();

            var reader = BuildReaderPrefab();
            var stats = BuildStatsPrefab();

            BuildScene(new ScreenView[] { home, load, list, difficult, difficultGrid, reader, stats });

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Kanji] S01 scene built: " + ScenePath);
        }

        // ---------- Scene ----------

        private static void BuildScene(ScreenView[] screenPrefabs)
        {
            // NewScene unloads unused assets, which invalidates the prefab handles: keep paths and reload.
            var prefabPaths = new string[screenPrefabs.Length];
            for (int i = 0; i < screenPrefabs.Length; i++)
                prefabPaths[i] = AssetDatabase.GetAssetPath(screenPrefabs[i]);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < prefabPaths.Length; i++)
                screenPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]).GetComponent<ScreenView>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _theme.Background;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var inputGroup = canvasGo.AddComponent<CanvasGroup>();

            var screenLayer = NewRect("ScreenLayer", canvasGo.transform);
            Stretch(screenLayer);
            var popupLayer = NewRect("PopupLayer", canvasGo.transform);
            Stretch(popupLayer);

            var blocker = NewRect("PopupBlocker", popupLayer);
            Stretch(blocker);
            blocker.gameObject.AddComponent<Image>().color = _theme.Scrim;
            blocker.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            blocker.gameObject.AddComponent<CanvasGroup>();
            blocker.gameObject.SetActive(false);

            var storyPopup = BuildStoryPopup(popupLayer);
            var kanjiPopup = BuildKanjiPopup(popupLayer);
            var confirmPopup = BuildConfirmPopup(popupLayer);
            var nameInputPopup = BuildNameInputPopup(popupLayer);
            var toast = BuildToast(popupLayer);

            var transition = BuildTransitionOverlay(canvasGo.transform, inputGroup); // last child: above popups

            var bootGo = new GameObject("AppBootstrap");
            var router = bootGo.AddComponent<BackButtonRouter>();
            var boot = bootGo.AddComponent<AppBootstrap>();

            var so = new SerializedObject(boot);
            so.FindProperty("_screenLayer").objectReferenceValue = screenLayer;
            so.FindProperty("_popupLayer").objectReferenceValue = popupLayer;
            so.FindProperty("_popupBlocker").objectReferenceValue = blocker.gameObject;
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.FindProperty("_transition").objectReferenceValue = transition;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_storyPopup").objectReferenceValue = storyPopup;
            so.FindProperty("_kanjiPopup").objectReferenceValue = kanjiPopup;
            so.FindProperty("_confirmPopup").objectReferenceValue = confirmPopup;
            so.FindProperty("_nameInputPopup").objectReferenceValue = nameInputPopup;
            so.FindProperty("_backButtonRouter").objectReferenceValue = router;
            var arr = so.FindProperty("_screenPrefabs");
            arr.arraySize = screenPrefabs.Length;
            for (int i = 0; i < screenPrefabs.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = screenPrefabs[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static TransitionOverlay BuildTransitionOverlay(Transform canvas, CanvasGroup inputGroup)
        {
            var rt = NewRect("TransitionOverlay", canvas);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            var c = _theme.FadeColor;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = false;
            var overlay = rt.gameObject.AddComponent<TransitionOverlay>();
            var so = new SerializedObject(overlay);
            so.FindProperty("_fade").objectReferenceValue = img;
            so.FindProperty("_inputGroup").objectReferenceValue = inputGroup;
            so.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
        }

        private static RectTransform NewPopupRoot(string name, RectTransform popupLayer, out RectTransform panel)
        {
            var root = NewRect(name, popupLayer);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            panel = NewRect("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(984, 0);
            var img = panel.gameObject.AddComponent<Image>();
            img.sprite = _theme.PanelSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = _theme.Surface;
            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 48, 48);
            vlg.spacing = 32;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.SetActive(false);
            return root;
        }

        private static StoryActionsPopup BuildStoryPopup(RectTransform popupLayer)
        {
            var root = NewPopupRoot("StoryPopup", popupLayer, out var panel);
            var popup = root.gameObject.AddComponent<StoryActionsPopup>();
            var name = NewLabel("Name", panel, "", _theme.TitleSize, FontStyles.Bold, _theme.TextPrimary);
            name.maxVisibleLines = 2;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.raycastTarget = false;
            var count = NewLabel("Count", panel, "", _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            count.raycastTarget = false;
            var edit = NewButton("BtnEdit", panel, UiStrings.EditName, 144, ButtonStyle.Secondary);
            var read = NewButton("BtnRead", panel, UiStrings.Read, 160, ButtonStyle.Primary);
            var del = NewButton("BtnDelete", panel, UiStrings.Delete, 160, ButtonStyle.Danger);
            var close = NewButton("BtnClose", panel, UiStrings.Close, 160, ButtonStyle.Secondary);
            var so = new SerializedObject(popup);
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_count").objectReferenceValue = count;
            so.FindProperty("_btnRead").objectReferenceValue = read;
            so.FindProperty("_btnEdit").objectReferenceValue = edit;
            so.FindProperty("_btnDelete").objectReferenceValue = del;
            so.FindProperty("_btnClose").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static KanjiPopup BuildKanjiPopup(RectTransform popupLayer)
        {
            var root = NewPopupRoot("KanjiPopup", popupLayer, out var panel);
            var popup = root.gameObject.AddComponent<KanjiPopup>();

            var body = NewRect("Body", panel);
            var bodyLayout = body.gameObject.AddComponent<LayoutElement>();
            bodyLayout.preferredHeight = 400;
            var scroll = body.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var viewport = NewRect("Viewport", body);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("BodyContent", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 32;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var kanji = NewLabel("Kanji", content, "", _theme.KanjiMaxSize, FontStyles.Normal, _theme.Link);
            kanji.enableAutoSizing = true;
            kanji.fontSizeMin = _theme.KanjiMinSize;
            kanji.fontSizeMax = _theme.KanjiMaxSize;
            kanji.textWrappingMode = TextWrappingModes.NoWrap;
            kanji.richText = false;
            kanji.parseCtrlCharacters = false;
            kanji.raycastTarget = false;
            kanji.gameObject.AddComponent<LayoutElement>().preferredHeight = 280;
            var sentence = NewLabel("Sentence", content, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            sentence.textWrappingMode = TextWrappingModes.Normal;
            sentence.lineSpacing = 10;
            sentence.richText = false;
            sentence.parseCtrlCharacters = false;
            sentence.raycastTarget = false;

            var rowReading = BuildDetailRow(content, "RowReading", UiStrings.FieldReading, _theme.ReadingSize, _theme.TextPrimary, out var reading);
            var rowMeaning = BuildDetailRow(content, "RowMeaning", UiStrings.FieldMeaning, _theme.BodySize, _theme.TextPrimary, out var meaning);
            var rowSentence = BuildDetailRow(content, "RowSentenceMeaning", UiStrings.FieldSentenceMeaning, _theme.BodySize, _theme.TextSecondary, out var sentenceMeaning);

            var secondary = NewRect("SecondaryActions", panel);
            var svlg = secondary.gameObject.AddComponent<VerticalLayoutGroup>();
            svlg.spacing = 24;
            svlg.childControlWidth = true;
            svlg.childControlHeight = true;
            svlg.childForceExpandWidth = true;
            svlg.childForceExpandHeight = false;
            var addDifficult = BuildAnswerButton("BtnAddDifficult", secondary, UiStrings.AddToDifficult, _theme.IconPlus, ButtonStyle.Secondary);
            addDifficult.GetComponent<LayoutElement>().preferredHeight = 144;
            var addIcon = addDifficult.transform.Find("Icon").GetComponent<Image>();
            addIcon.color = _theme.TextPrimary;
            var addLabel = addDifficult.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            addLabel.color = _theme.TextPrimary;
            var addColors = addDifficult.colors;
            addColors.disabledColor = _theme.Disabled;
            addDifficult.colors = addColors;
            var removeDifficult = BuildAnswerButton("BtnRemoveDifficult", secondary, UiStrings.RemoveFromDifficult, _theme.IconCross, ButtonStyle.Danger);
            removeDifficult.GetComponent<LayoutElement>().preferredHeight = 160;
            removeDifficult.gameObject.SetActive(false);
            var closeDifficult = NewButton("BtnCloseDifficult", secondary, UiStrings.Close, 144, ButtonStyle.Secondary);
            closeDifficult.gameObject.SetActive(false);
            secondary.gameObject.SetActive(false);

            var answerRow = NewRect("AnswerRow", panel);
            var hlg = answerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 32;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            answerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 160;
            var didntKnow = BuildAnswerButton("BtnDidntKnow", answerRow, UiStrings.DidntKnow, _theme.IconCross, ButtonStyle.Danger);
            var knew = BuildAnswerButton("BtnKnew", answerRow, UiStrings.Knew, _theme.IconCheck, ButtonStyle.Success);
            var reveal = NewButton("BtnReveal", panel, UiStrings.Resolve, 160, ButtonStyle.Primary);
            reveal.gameObject.SetActive(false);

            var swipe = panel.gameObject.AddComponent<CardSwipe>();
            var swipeSo = new SerializedObject(swipe);
            swipeSo.FindProperty("_scroll").objectReferenceValue = scroll;
            swipeSo.FindProperty("_tintTarget").objectReferenceValue = panel.GetComponent<Image>();
            swipeSo.FindProperty("_greenTint").colorValue = _theme.Success;
            swipeSo.FindProperty("_redTint").colorValue = _theme.Danger;
            swipeSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(popup);
            so.FindProperty("_kanji").objectReferenceValue = kanji;
            so.FindProperty("_sentence").objectReferenceValue = sentence;
            so.FindProperty("_rowReading").objectReferenceValue = rowReading;
            so.FindProperty("_rowMeaning").objectReferenceValue = rowMeaning;
            so.FindProperty("_rowSentenceMeaning").objectReferenceValue = rowSentence;
            so.FindProperty("_reading").objectReferenceValue = reading;
            so.FindProperty("_meaning").objectReferenceValue = meaning;
            so.FindProperty("_sentenceMeaning").objectReferenceValue = sentenceMeaning;
            so.FindProperty("_btnKnew").objectReferenceValue = knew;
            so.FindProperty("_btnDidntKnow").objectReferenceValue = didntKnow;
            so.FindProperty("_secondaryActions").objectReferenceValue = secondary;
            so.FindProperty("_btnAddDifficult").objectReferenceValue = addDifficult;
            so.FindProperty("_btnRemoveDifficult").objectReferenceValue = removeDifficult;
            so.FindProperty("_btnCloseDifficult").objectReferenceValue = closeDifficult;
            so.FindProperty("_answerRow").objectReferenceValue = answerRow.gameObject;
            so.FindProperty("_btnReveal").objectReferenceValue = reveal;
            so.FindProperty("_swipe").objectReferenceValue = swipe;
            so.FindProperty("_addDifficultLabel").objectReferenceValue = addLabel;
            so.FindProperty("_addDifficultIcon").objectReferenceValue = addIcon;
            so.FindProperty("_iconPlus").objectReferenceValue = _theme.IconPlus;
            so.FindProperty("_iconCheck").objectReferenceValue = _theme.IconCheck;
            so.FindProperty("_iconPlusColor").colorValue = _theme.TextPrimary;
            so.FindProperty("_body").objectReferenceValue = scroll;
            so.FindProperty("_bodyLayout").objectReferenceValue = bodyLayout;
            so.FindProperty("_bodyContent").objectReferenceValue = content;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static Button BuildAnswerButton(string name, Transform parent, string label, Sprite icon, ButtonStyle style)
        {
            var button = NewButton(name, parent, label, 160, style);
            var rt = (RectTransform)button.transform;
            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var text = rt.Find("Label").GetComponent<TextMeshProUGUI>();
            text.font = _bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 40;
            text.fontSizeMax = _theme.ButtonSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var iconRt = NewRect("Icon", rt);
            iconRt.SetAsFirstSibling();
            var img = iconRt.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.color = _theme.TextOnAccent;
            img.raycastTarget = false;
            var le = iconRt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 64;
            le.preferredHeight = 64;
            return button;
        }

        private static GameObject BuildDetailRow(RectTransform parent, string name, string label, float valueSize, Color valueColor, out TMP_Text value)
        {
            var row = NewRect(name, parent);
            var vlg = row.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var l = NewLabel("Label", row, label, _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            l.alignment = TextAlignmentOptions.TopLeft;
            l.richText = false;
            l.raycastTarget = false;
            value = NewLabel("Value", row, "", valueSize, FontStyles.Normal, valueColor);
            value.alignment = TextAlignmentOptions.TopLeft;
            value.textWrappingMode = TextWrappingModes.Normal;
            value.richText = false;
            value.parseCtrlCharacters = false;
            value.raycastTarget = false;
            return row.gameObject;
        }

        private static ConfirmPopup BuildConfirmPopup(RectTransform popupLayer)
        {
            var root = NewPopupRoot("ConfirmDeletePopup", popupLayer, out var panel);
            var popup = root.gameObject.AddComponent<ConfirmPopup>();
            var message = NewLabel("Message", panel, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            message.maxVisibleLines = 3;
            message.overflowMode = TextOverflowModes.Ellipsis;
            message.raycastTarget = false;
            var cancel = NewButton("BtnCancel", panel, UiStrings.Cancel, 160, ButtonStyle.Secondary);
            var confirm = NewButton("BtnConfirm", panel, UiStrings.Delete, 160, ButtonStyle.Danger);
            var outline = cancel.gameObject.AddComponent<Outline>();
            outline.effectColor = _theme.TextPrimary;
            outline.effectDistance = new Vector2(6, -6);
            var so = new SerializedObject(popup);
            so.FindProperty("_message").objectReferenceValue = message;
            so.FindProperty("_btnCancel").objectReferenceValue = cancel;
            so.FindProperty("_btnConfirm").objectReferenceValue = confirm;
            so.FindProperty("_confirmLabel").objectReferenceValue = confirm.GetComponentInChildren<TMP_Text>();
            so.FindProperty("_cancelFocusOutline").objectReferenceValue = outline;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static NameInputPopup BuildNameInputPopup(RectTransform popupLayer)
        {
            var root = NewPopupRoot("NameInputPopup", popupLayer, out var panel);
            panel.sizeDelta = new Vector2(888, 0);
            var popup = root.gameObject.AddComponent<NameInputPopup>();
            var title = NewLabel("Title", panel, "", _theme.TitleSize, FontStyles.Bold, _theme.TextPrimary);
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            var field = BuildNameField(panel, out var placeholder, out var border);

            var error = NewLabel("ErrorText", panel, "", _theme.CaptionSize, FontStyles.Normal, _theme.Error);
            error.alignment = TextAlignmentOptions.TopLeft;
            error.textWrappingMode = TextWrappingModes.Normal;
            error.maxVisibleLines = 3;
            error.overflowMode = TextOverflowModes.Ellipsis;
            error.richText = false;
            error.raycastTarget = false;
            error.gameObject.SetActive(false);

            var stats = NewRect("Stats", panel);
            var svlg = stats.gameObject.AddComponent<VerticalLayoutGroup>();
            svlg.spacing = 8;
            svlg.childControlWidth = true;
            svlg.childControlHeight = true;
            svlg.childForceExpandWidth = true;
            svlg.childForceExpandHeight = false;
            var kanjis = NewStatLabel("StatKanjis", stats);
            var chars = NewStatLabel("StatCharacters", stats);
            var warnBox = NewRect("WarningBox", stats);
            warnBox.gameObject.AddComponent<Image>().color = _theme.WarningSurface;
            var wvlg = warnBox.gameObject.AddComponent<VerticalLayoutGroup>();
            wvlg.padding = new RectOffset(16, 16, 16, 16);
            wvlg.childControlWidth = true;
            wvlg.childControlHeight = true;
            wvlg.childForceExpandWidth = true;
            wvlg.childForceExpandHeight = false;
            var warnings = NewStatLabel("StatWarnings", warnBox); // popup toggles its parent, the WarningBox

            var buttons = NewRect("Buttons", panel);
            var hlg = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 32;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 160;
            var cancel = NewButton("BtnCancel", buttons, UiStrings.Cancel, 160, ButtonStyle.Secondary);
            var accept = NewButton("BtnAccept", buttons, UiStrings.Accept, 160, ButtonStyle.Primary);

            var so = new SerializedObject(popup);
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_nameField").objectReferenceValue = field;
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_fieldBorder").objectReferenceValue = border;
            so.FindProperty("_errorText").objectReferenceValue = error;
            so.FindProperty("_statsRoot").objectReferenceValue = stats.gameObject;
            so.FindProperty("_statKanjis").objectReferenceValue = kanjis;
            so.FindProperty("_statCharacters").objectReferenceValue = chars;
            so.FindProperty("_statWarnings").objectReferenceValue = warnings;
            so.FindProperty("_btnCancel").objectReferenceValue = cancel;
            so.FindProperty("_btnAccept").objectReferenceValue = accept;
            so.FindProperty("_acceptLabel").objectReferenceValue = accept.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static TextMeshProUGUI NewStatLabel(string name, Transform parent)
        {
            var l = NewLabel(name, parent, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            l.alignment = TextAlignmentOptions.Left;
            l.richText = false;
            l.raycastTarget = false;
            return l;
        }

        private static TMP_InputField BuildNameField(RectTransform parent, out TMP_Text placeholder, out Outline border)
        {
            var rt = NewRect("NameField", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _theme.InputSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = _theme.SurfaceRaised;
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 144;
            border = rt.gameObject.AddComponent<Outline>();
            border.effectColor = _theme.Border;
            border.effectDistance = new Vector2(3, -3);

            var viewport = NewRect("TextArea", rt);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(32, 8);
            viewport.offsetMax = new Vector2(-32, -8);
            viewport.gameObject.AddComponent<RectMask2D>();

            var ph = NewLabel("Placeholder", viewport, "", 44, FontStyles.Normal, _theme.TextSecondary);
            ph.alignment = TextAlignmentOptions.Left;
            ph.raycastTarget = false;
            Stretch(ph.rectTransform);
            var text = NewLabel("Text", viewport, "", 44, FontStyles.Normal, _theme.TextPrimary);
            text.alignment = TextAlignmentOptions.Left;
            text.richText = false;
            text.raycastTarget = false;
            Stretch(text.rectTransform);

            var field = rt.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = img;
            field.transition = Selectable.Transition.None;
            field.textViewport = viewport;
            field.textComponent = text;
            field.placeholder = ph;
            field.fontAsset = _regular;
            field.pointSize = 44;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = TMP_InputField.ContentType.Standard;
            field.keyboardType = TouchScreenKeyboardType.Default;
            field.inputType = TMP_InputField.InputType.Standard;
            field.characterLimit = 60;
            field.richText = false;
            field.shouldHideMobileInput = false;
            field.resetOnDeActivation = false;
            placeholder = ph;
            return field;
        }

        private static ToastView BuildToast(RectTransform popupLayer)
        {
            var area = NewRect("ToastArea", popupLayer);
            Stretch(area);
            area.gameObject.AddComponent<SafeAreaFitter>();
            var rt = NewRect("Toast", area);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(48, 48);
            rt.offsetMax = new Vector2(-48, 48);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = _theme.SurfaceRaised;
            bg.raycastTarget = false;
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var label = NewLabel("Text", rt, string.Empty, _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maxVisibleLines = 2;
            label.gameObject.AddComponent<LayoutElement>().minHeight = 96; // 144 total with padding
            var toast = rt.gameObject.AddComponent<ToastView>();
            var so = new SerializedObject(toast);
            so.FindProperty("_text").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        // ---------- Prefabs ----------

        private static ScreenView BuildHomePrefab()
        {
            var root = NewScreenRoot("Home", ScreenId.Home, out HomeScreen screen);

            var topBar = NewRect("TopBar", root);
            TopStretch(topBar, 0, 160);
            var bMenu = NewMenuButton(topBar);

            // Cover image and "Leer historias", centred as one block below the top bar.
            var hero = NewRect("Hero", root);
            Stretch(hero);
            hero.offsetMin = new Vector2(48, 64);
            hero.offsetMax = new Vector2(-48, -160);
            var layout = hero.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 64;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var cover = LoadCoverSprite();
            var coverRt = NewRect("Cover", hero);
            var coverImg = coverRt.gameObject.AddComponent<Image>();
            coverImg.sprite = cover;
            coverImg.preserveAspect = true;
            coverImg.raycastTarget = false;
            var coverLe = coverRt.gameObject.AddComponent<LayoutElement>();
            float aspect = cover != null ? cover.rect.height / cover.rect.width : 1f;
            coverLe.minHeight = 480;
            coverLe.preferredHeight = 984 * aspect; // full content width at the 1080 reference

            var bRead = NewButton("BtnReadStories", hero, UiStrings.ReadStories, 176, ButtonStyle.Primary);

            var menu = BuildSideMenu(root, out var bLoad, out var bDiff, out var bQuit);

            var so = new SerializedObject(screen);
            so.FindProperty("_storyListButton").objectReferenceValue = bRead;
            so.FindProperty("_menuButton").objectReferenceValue = bMenu;
            so.FindProperty("_sideMenu").objectReferenceValue = menu;
            so.FindProperty("_loadStoriesButton").objectReferenceValue = bLoad;
            so.FindProperty("_difficultListButton").objectReferenceValue = bDiff;
            so.FindProperty("_exitButton").objectReferenceValue = bQuit;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "Home");
        }

        private static Sprite LoadCoverSprite()
        {
            var importer = AssetImporter.GetAtPath(CoverPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[Kanji] Cover image not found at " + CoverPath);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath);
        }

        private static Button NewMenuButton(Transform parent)
        {
            var rt = NewRect("BtnMenu", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(144, 144);
            rt.anchoredPosition = new Vector2(24, 0);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // touch area only
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            // Hamburger icon: three bars
            for (int i = -1; i <= 1; i++)
            {
                var bar = NewRect("Bar", rt);
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(64, 8);
                bar.anchoredPosition = new Vector2(0, i * 22);
                var img = bar.gameObject.AddComponent<Image>();
                img.color = _theme.TextPrimary;
                img.raycastTarget = false;
            }
            return button;
        }

        private static SideMenu BuildSideMenu(RectTransform screenRoot, out Button load, out Button difficult, out Button quit)
        {
            var root = NewRect("SideMenu", screenRoot);
            Stretch(root);

            var scrimRt = NewRect("Scrim", root);
            Stretch(scrimRt);
            var scrimImg = scrimRt.gameObject.AddComponent<Image>();
            scrimImg.color = _theme.Scrim;
            var scrim = scrimRt.gameObject.AddComponent<CanvasGroup>();
            var scrimButton = scrimRt.gameObject.AddComponent<Button>();
            scrimButton.targetGraphic = scrimImg;
            scrimButton.transition = Selectable.Transition.None;

            var panel = NewRect("Panel", root);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0.78f, 1f);
            panel.pivot = new Vector2(0, 0.5f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = _theme.Surface;
            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 64, 64);
            vlg.spacing = 32;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = NewLabel("Title", panel, UiStrings.AppTitle, _theme.TitleSize, FontStyles.Bold, _theme.TextPrimary);
            title.alignment = TextAlignmentOptions.Left;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 128;

            load = NewButton("BtnLoadStories", panel, UiStrings.LoadStories, 160, ButtonStyle.Primary);
            difficult = NewButton("BtnDifficult", panel, UiStrings.DifficultKanjis, 160, ButtonStyle.Primary);
            var spacer = NewRect("Spacer", panel);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            quit = NewButton("BtnQuit", panel, UiStrings.Quit, 144, ButtonStyle.Secondary);

            var menu = root.gameObject.AddComponent<SideMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_scrim").objectReferenceValue = scrim;
            so.FindProperty("_scrimButton").objectReferenceValue = scrimButton;
            so.FindProperty("_duration").floatValue = _theme.ScreenFadeOutSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            return menu;
        }

        private static ScreenView BuildEmptyPrefab(string name, ScreenId id, string title)
        {
            var root = NewScreenRoot(name, id, out EmptyScreen screen);

            var back = BuildTopBar(root, title, out var titleText);

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 48);
            content.offsetMax = new Vector2(-48, -144);
            var placeholder = NewLabel("PlaceholderText", content, UiStrings.UnderConstruction, 44,
                FontStyles.Normal, _theme.TextSecondary);
            Stretch(placeholder.rectTransform);

            var so = new SerializedObject(screen);
            so.FindProperty("_title").objectReferenceValue = titleText;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, name);
        }

        private static ScreenView BuildReaderPrefab()
        {
            var root = NewScreenRoot("StoryReader", ScreenId.Reader, out ReaderScreen screen);
            var back = BuildTopBar(root, "", out var title);
            title.fontSize = _theme.BodySize;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.richText = false;
            title.parseCtrlCharacters = false;
            title.raycastTarget = false;
            title.rectTransform.offsetMin = new Vector2(184, 0);
            title.rectTransform.offsetMax = new Vector2(-48, 0);

            var scrollRt = NewRect("Scroll", root);
            Stretch(scrollRt);
            scrollRt.offsetMax = new Vector2(0, -144);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbar = null;

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = Color.clear; // catches drags in gaps
            viewport.gameObject.AddComponent<Canvas>(); // own sub-canvas: scrolling only re-batches this one
            viewport.gameObject.AddComponent<GraphicRaycaster>();

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 32, 96);
            vlg.spacing = 0;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var chunk = NewLabel("ChunkTemplate", content, "", _theme.ReadingSize, FontStyles.Normal, _theme.TextPrimary);
            chunk.alignment = TextAlignmentOptions.TopLeft;
            chunk.richText = true;
            chunk.parseCtrlCharacters = false;
            chunk.textWrappingMode = TextWrappingModes.Normal;
            chunk.overflowMode = TextOverflowModes.Overflow;
            chunk.enableAutoSizing = false;
            chunk.extraPadding = false;
            chunk.lineSpacing = 10;
            chunk.paragraphSpacing = 0;
            chunk.characterSpacing = 0;
            chunk.raycastTarget = true;
            chunk.isTextObjectScaleStatic = true;
            chunk.gameObject.AddComponent<LinkClickHandler>();
            chunk.gameObject.SetActive(false);

            var loading = NewLabel("LoadingText", root, UiStrings.ReaderLoading, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(loading.rectTransform);
            loading.rectTransform.offsetMax = new Vector2(0, -144);
            loading.raycastTarget = false;
            loading.richText = false;
            loading.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_scroll").objectReferenceValue = scroll;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_chunkTemplate").objectReferenceValue = chunk;
            so.FindProperty("_loadingText").objectReferenceValue = loading;
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.FindProperty("_maxChunkChars").intValue = 4000;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "StoryReader");
        }

        private static ScreenView BuildStatsPrefab()
        {
            var root = NewScreenRoot("Stats", ScreenId.Stats, out StatsScreen screen);
            var back = BuildTopBar(root, UiStrings.StatsTitle, out _);

            var accept = NewButton("BtnAccept", root, UiStrings.Accept, 160, ButtonStyle.Primary);
            var acceptRt = (RectTransform)accept.transform;
            acceptRt.anchorMin = new Vector2(0, 0);
            acceptRt.anchorMax = new Vector2(1, 0);
            acceptRt.pivot = new Vector2(0.5f, 0);
            acceptRt.offsetMin = new Vector2(48, 48);
            acceptRt.offsetMax = new Vector2(-48, 48 + 160);

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 48 + 160 + 32);
            content.offsetMax = new Vector2(-48, -144 - 32);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var percent = NewLabel("Percent", content, "", _theme.DisplaySize * 1.5f, FontStyles.Bold, _theme.TextPrimary);
            percent.textWrappingMode = TextWrappingModes.NoWrap;
            percent.raycastTarget = false;
            percent.gameObject.AddComponent<LayoutElement>().preferredHeight = 200;

            var cells = NewRect("Summary", content);
            cells.gameObject.AddComponent<LayoutElement>().preferredHeight = 200;
            var hlg = cells.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            var total = BuildStatCell(cells, "Total", UiStrings.StatsTotal, _theme.TextPrimary);
            var passed = BuildStatCell(cells, "Passed", UiStrings.StatsPassed, _theme.Success);
            var failed = BuildStatCell(cells, "Failed", UiStrings.StatsFailed, _theme.Danger);

            var header = NewLabel("FailedHeader", content, UiStrings.StatsFailedKanjis, _theme.BodySize, FontStyles.Bold, _theme.TextSecondary);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.raycastTarget = false;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;

            var listRt = NewRect("FailedList", content);
            var listLe = listRt.gameObject.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1;
            listLe.minHeight = 200;
            var listImg = listRt.gameObject.AddComponent<Image>();
            listImg.sprite = _theme.PanelSprite;
            listImg.type = Image.Type.Sliced;
            listImg.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            listImg.color = _theme.Surface;
            var scroll = listRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            var viewport = NewRect("Viewport", listRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var rows = NewRect("Content", viewport);
            rows.anchorMin = new Vector2(0, 1);
            rows.anchorMax = new Vector2(1, 1);
            rows.pivot = new Vector2(0.5f, 1);
            rows.sizeDelta = Vector2.zero;
            rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rowsVlg = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsVlg.padding = new RectOffset(32, 32, 16, 16);
            rowsVlg.childControlWidth = true;
            rowsVlg.childControlHeight = true;
            rowsVlg.childForceExpandWidth = true;
            rowsVlg.childForceExpandHeight = false;
            scroll.viewport = viewport;
            scroll.content = rows;

            var rowTemplate = NewLabel("FailedRowTemplate", rows, "", 56f, FontStyles.Normal, _theme.TextPrimary);
            rowTemplate.alignment = TextAlignmentOptions.MidlineLeft;
            rowTemplate.textWrappingMode = TextWrappingModes.NoWrap;
            rowTemplate.overflowMode = TextOverflowModes.Ellipsis;
            rowTemplate.richText = false;
            rowTemplate.parseCtrlCharacters = false;
            rowTemplate.raycastTarget = false;
            rowTemplate.gameObject.AddComponent<LayoutElement>().minHeight = 120;
            rowTemplate.gameObject.SetActive(false);

            var noFailed = NewLabel("NoFailedMessage", listRt, UiStrings.StatsNoFailed, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(noFailed.rectTransform);
            noFailed.raycastTarget = false;
            noFailed.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_acceptButton").objectReferenceValue = accept;
            so.FindProperty("_total").objectReferenceValue = total;
            so.FindProperty("_passed").objectReferenceValue = passed;
            so.FindProperty("_failed").objectReferenceValue = failed;
            so.FindProperty("_percent").objectReferenceValue = percent;
            so.FindProperty("_failedScroll").objectReferenceValue = scroll;
            so.FindProperty("_failedContent").objectReferenceValue = rows;
            so.FindProperty("_failedRowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("_noFailedMessage").objectReferenceValue = noFailed.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "Stats");
        }

        private static TMP_Text BuildStatCell(RectTransform parent, string name, string caption, Color valueColor)
        {
            var cell = NewRect(name, parent);
            var img = cell.gameObject.AddComponent<Image>();
            img.sprite = _theme.PanelSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = _theme.Surface;
            img.raycastTarget = false;
            var vlg = cell.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 16, 16);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            var value = NewLabel("Value", cell, "0", _theme.TitleSize * 1.4f, FontStyles.Bold, valueColor);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.raycastTarget = false;
            var label = NewLabel("Caption", cell, caption, _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return value;
        }

        private static ScreenView BuildStoryListPrefab()
        {
            var rowPrefab = BuildStoryRowPrefab();
            var root = NewScreenRoot("StoryList", ScreenId.StoryList, out StoryListScreen screen);
            var back = BuildTopBar(root, UiStrings.ReadStories, out _);

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 48);
            content.offsetMax = new Vector2(-48, -144);

            var warning = NewRect("Warning", content);
            TopStretch(warning, -24, 112);
            warning.gameObject.AddComponent<Image>().color = _theme.WarningSurface;
            var warningText = NewLabel("WarningText", warning, "", _theme.CaptionSize, FontStyles.Normal, _theme.TextPrimary);
            Stretch(warningText.rectTransform);
            warningText.rectTransform.offsetMin = new Vector2(24, 0);
            warningText.rectTransform.offsetMax = new Vector2(-24, 0);
            warningText.alignment = TextAlignmentOptions.MidlineLeft;
            warningText.textWrappingMode = TextWrappingModes.NoWrap;
            warningText.overflowMode = TextOverflowModes.Ellipsis;
            warningText.raycastTarget = false;
            warning.gameObject.SetActive(false);

            var scrollRt = NewRect("ListScroll", content);
            Stretch(scrollRt);
            scrollRt.gameObject.AddComponent<Image>().color = Color.clear; // catches drags in gaps
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var list = NewRect("Content", viewport);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.sizeDelta = Vector2.zero;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(0, 0, 48, 48);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            scroll.viewport = viewport;
            scroll.content = list;

            var empty = NewLabel("EmptyMessage", content, UiStrings.EmptyLibrary, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(empty.rectTransform);
            empty.rectTransform.offsetMin = new Vector2(24, 24);
            empty.rectTransform.offsetMax = new Vector2(-24, -24);
            empty.raycastTarget = false;
            empty.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_listScroll").objectReferenceValue = scroll;
            so.FindProperty("_content").objectReferenceValue = list;
            so.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("_warning").objectReferenceValue = warning.gameObject;
            so.FindProperty("_warningText").objectReferenceValue = warningText;
            so.FindProperty("_emptyMessage").objectReferenceValue = empty;
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "StoryList");
        }

        private static ScreenView BuildDifficultListPrefab()
        {
            var rowPrefab = BuildStoryRowPrefab();
            var root = NewScreenRoot("DifficultList", ScreenId.DifficultList, out DifficultListScreen screen);
            var back = BuildTopBar(root, UiStrings.DifficultKanjis, out _);

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 48);
            content.offsetMax = new Vector2(-48, -144);

            var scrollRt = NewRect("ListScroll", content);
            Stretch(scrollRt);
            scrollRt.gameObject.AddComponent<Image>().color = Color.clear;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var list = NewRect("Content", viewport);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.sizeDelta = Vector2.zero;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(0, 0, 48, 48);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            scroll.viewport = viewport;
            scroll.content = list;

            var empty = NewLabel("EmptyMessage", content, UiStrings.EmptyDifficult, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(empty.rectTransform);
            empty.rectTransform.offsetMin = new Vector2(24, 24);
            empty.rectTransform.offsetMax = new Vector2(-24, -24);
            empty.raycastTarget = false;
            empty.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_listScroll").objectReferenceValue = scroll;
            so.FindProperty("_content").objectReferenceValue = list;
            so.FindProperty("_rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("_emptyMessage").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "DifficultList");
        }

        private static ScreenView BuildDifficultGridPrefab()
        {
            var cellPrefab = BuildDifficultCellPrefab();
            var root = NewScreenRoot("DifficultGrid", ScreenId.DifficultGrid, out DifficultGridScreen screen);
            var back = BuildTopBar(root, "", out var title);
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.textWrappingMode = TextWrappingModes.NoWrap;

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 240);
            content.offsetMax = new Vector2(-48, -144);

            // S11: "Study" pinned at the bottom; hidden by the screen when the list is empty.
            var study = NewButton("BtnStudy", root, UiStrings.StudyKanji, 160, ButtonStyle.Primary);
            var studyRt = (RectTransform)study.transform;
            studyRt.anchorMin = new Vector2(0, 0);
            studyRt.anchorMax = new Vector2(1, 0);
            studyRt.pivot = new Vector2(0.5f, 0);
            studyRt.offsetMin = new Vector2(48, 48);
            studyRt.offsetMax = new Vector2(-48, 48 + 160);

            var scrollRt = NewRect("GridScroll", content);
            Stretch(scrollRt);
            scrollRt.gameObject.AddComponent<Image>().color = Color.clear;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var grid = NewRect("Content", viewport);
            grid.anchorMin = new Vector2(0, 1);
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(0.5f, 1);
            grid.sizeDelta = Vector2.zero;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = grid.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.padding = new RectOffset(0, 0, 48, 48);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            scroll.viewport = viewport;
            scroll.content = grid;

            var empty = NewLabel("EmptyMessage", content, UiStrings.EmptyDifficult, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(empty.rectTransform);
            empty.rectTransform.offsetMin = new Vector2(24, 24);
            empty.rectTransform.offsetMax = new Vector2(-24, -24);
            empty.raycastTarget = false;
            empty.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_scroll").objectReferenceValue = scroll;
            so.FindProperty("_content").objectReferenceValue = grid;
            so.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("_emptyMessage").objectReferenceValue = empty;
            so.FindProperty("_studyButton").objectReferenceValue = study;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "DifficultGrid");
        }

        private static DifficultKanjiCell BuildDifficultCellPrefab()
        {
            var rt = NewRect("DifficultKanjiCell", null);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 168;
            le.minHeight = 168;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _theme.PanelSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = Color.white;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = _theme.Surface;
            colors.highlightedColor = _theme.Surface;
            colors.selectedColor = _theme.Surface;
            colors.pressedColor = _theme.SurfaceRaised;
            colors.disabledColor = _theme.Disabled;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            // Fixed size (no auto-sizing) so every row reads the same.
            var kanji = NewLabel("Kanji", rt, "", 72f, FontStyles.Normal, _theme.Link);
            Stretch(kanji.rectTransform);
            kanji.rectTransform.offsetMin = new Vector2(32, 8);
            kanji.rectTransform.offsetMax = new Vector2(-32 - 96 - 24, -8);
            kanji.enableAutoSizing = false;
            kanji.alignment = TextAlignmentOptions.MidlineLeft;
            kanji.textWrappingMode = TextWrappingModes.NoWrap;
            kanji.overflowMode = TextOverflowModes.Ellipsis;
            kanji.raycastTarget = false;

            // S11: progress circle on the right (track + radial fill = study successes / 5).
            var track = NewRect("ProgressTrack", rt);
            track.anchorMin = track.anchorMax = new Vector2(1, 0.5f);
            track.pivot = new Vector2(1, 0.5f);
            track.sizeDelta = new Vector2(96, 96);
            track.anchoredPosition = new Vector2(-32, 0);
            var trackImg = track.gameObject.AddComponent<Image>();
            trackImg.sprite = _theme.CircleSprite;
            trackImg.color = new Color(_theme.TextSecondary.r, _theme.TextSecondary.g, _theme.TextSecondary.b, 0.25f);
            trackImg.raycastTarget = false;
            var fillRt = NewRect("ProgressFill", track);
            Stretch(fillRt);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = _theme.CircleSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.color = _theme.Link;
            fill.raycastTarget = false;

            var cell = rt.gameObject.AddComponent<DifficultKanjiCell>();
            var so = new SerializedObject(cell);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_kanji").objectReferenceValue = kanji;
            so.FindProperty("_progressFill").objectReferenceValue = fill;
            so.FindProperty("_progressColor").colorValue = _theme.Link;
            so.ApplyModifiedPropertiesWithoutUndo();

            var path = "Assets/_Project/Prefabs/Widgets/DifficultKanjiCell.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(rt.gameObject, path);
            Object.DestroyImmediate(rt.gameObject);
            return prefab.GetComponent<DifficultKanjiCell>();
        }

        private static StoryListRow BuildStoryRowPrefab()
        {
            var rt = NewRect("StoryRow", null);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _theme.PanelSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = Color.white;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 168;
            le.minHeight = 168;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = _theme.Surface;
            colors.highlightedColor = _theme.Surface;
            colors.selectedColor = _theme.Surface;
            colors.pressedColor = _theme.SurfaceRaised;
            colors.disabledColor = _theme.Disabled;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(32, 32, 24, 24);
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var name = NewLabel("Name", rt, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.raycastTarget = false;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var count = NewLabel("Count", rt, "", _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            count.alignment = TextAlignmentOptions.MidlineRight;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.raycastTarget = false;

            var row = rt.gameObject.AddComponent<StoryListRow>();
            var so = new SerializedObject(row);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_count").objectReferenceValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();

            var path = "Assets/_Project/Prefabs/Widgets/StoryRow.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(rt.gameObject, path);
            Object.DestroyImmediate(rt.gameObject);
            return prefab.GetComponent<StoryListRow>();
        }

        private static Button BuildTopBar(RectTransform root, string title, out TextMeshProUGUI titleText)
        {
            var topBar = NewRect("TopBar", root);
            topBar.anchorMin = new Vector2(0, 1);
            topBar.anchorMax = new Vector2(1, 1);
            topBar.pivot = new Vector2(0.5f, 1);
            topBar.sizeDelta = new Vector2(0, 144);
            topBar.anchoredPosition = Vector2.zero;
            topBar.gameObject.AddComponent<Image>().color = _theme.Surface;

            var back = NewBackButton(topBar);

            titleText = NewLabel("Title", topBar, title, _theme.TitleSize, FontStyles.Bold, _theme.TextPrimary);
            Stretch(titleText.rectTransform);
            titleText.rectTransform.offsetMin = new Vector2(192, 0);
            titleText.rectTransform.offsetMax = new Vector2(-192, 0);
            return back;
        }

        private static ScreenView BuildLoadStoriesPrefab()
        {
            var root = NewScreenRoot("LoadStories", ScreenId.LoadStories, out LoadStoriesScreen screen);
            var back = BuildTopBar(root, UiStrings.LoadStories, out _);

            var content = NewRect("Content", root);
            Stretch(content);
            content.offsetMin = new Vector2(48, 48);
            content.offsetMax = new Vector2(-48, -144);

            var pick = NewButton("BtnPick", content, UiStrings.PickFile, 160, ButtonStyle.Primary);
            Object.DestroyImmediate(pick.GetComponent<LayoutElement>());
            TopStretch((RectTransform)pick.transform, -32, 160);

            var info = NewRect("Info", content);
            TopStretch(info, -224, 176);
            info.gameObject.AddComponent<Image>().color = _theme.Surface;
            var vlg = info.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            var fileName = NewLabel("FileName", info, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            var fileMeta = NewLabel("FileMeta", info, "", _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            foreach (var t in new[] { fileName, fileMeta })
            {
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                t.raycastTarget = false;
            }

            var panel = NewRect("PreviewPanel", content);
            Stretch(panel);
            panel.offsetMax = new Vector2(0, -432);
            panel.gameObject.AddComponent<Image>().color = _theme.Surface;

            var scrollRt = NewRect("PreviewScroll", panel);
            Stretch(scrollRt);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 1f;

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(24, 24);
            viewport.offsetMax = new Vector2(-24, -24);
            viewport.gameObject.AddComponent<RectMask2D>();

            var textContent = NewRect("Content", viewport);
            textContent.anchorMin = new Vector2(0, 1);
            textContent.anchorMax = new Vector2(1, 1);
            textContent.pivot = new Vector2(0.5f, 1);
            textContent.sizeDelta = Vector2.zero;
            textContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentLayout = textContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var previewText = NewLabel("PreviewText", textContent, "", _theme.BodySize, FontStyles.Normal, _theme.TextPrimary);
            previewText.alignment = TextAlignmentOptions.TopLeft;
            previewText.richText = false;
            previewText.textWrappingMode = TextWrappingModes.Normal;
            previewText.enableAutoSizing = false;
            previewText.raycastTarget = false;

            scroll.viewport = viewport;
            scroll.content = textContent;

            var more = NewLabel("MoreLabel", panel, "", _theme.CaptionSize, FontStyles.Normal, _theme.TextSecondary);
            var moreRt = more.rectTransform;
            moreRt.anchorMin = new Vector2(0, 0);
            moreRt.anchorMax = new Vector2(1, 0);
            moreRt.pivot = new Vector2(0.5f, 0);
            moreRt.sizeDelta = new Vector2(0, 80);
            moreRt.anchoredPosition = Vector2.zero;
            more.raycastTarget = false;

            var message = NewLabel("Message", content, UiStrings.LoadHint, _theme.BodySize, FontStyles.Normal, _theme.TextSecondary);
            Stretch(message.rectTransform);
            message.rectTransform.offsetMin = new Vector2(24, 0);
            message.rectTransform.offsetMax = new Vector2(-24, -432);
            message.raycastTarget = false;

            var so = new SerializedObject(screen);
            so.FindProperty("_btnBack").objectReferenceValue = back;
            so.FindProperty("_btnPick").objectReferenceValue = pick;
            so.FindProperty("_fileName").objectReferenceValue = fileName;
            so.FindProperty("_fileMeta").objectReferenceValue = fileMeta;
            so.FindProperty("_infoRoot").objectReferenceValue = info.gameObject;
            so.FindProperty("_previewRoot").objectReferenceValue = panel.gameObject;
            so.FindProperty("_previewScroll").objectReferenceValue = scroll;
            so.FindProperty("_previewText").objectReferenceValue = previewText;
            so.FindProperty("_theme").objectReferenceValue = _theme;
            so.FindProperty("_moreLabel").objectReferenceValue = more;
            so.FindProperty("_message").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root.gameObject, "LoadStories");
        }

        private static void TopStretch(RectTransform rt, float y, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, y);
        }

        private static RectTransform NewScreenRoot<T>(string name, ScreenId id, out T screen) where T : ScreenView
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            go.AddComponent<Image>().color = _theme.Background;
            go.AddComponent<SafeAreaFitter>();
            screen = go.AddComponent<T>();
            var so = new SerializedObject(screen);
            so.FindProperty("_id").enumValueIndex = (int)id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rt;
        }

        private static ScreenView SavePrefab(GameObject root, string name)
        {
            var path = ScreensDir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ScreenView>();
        }

        // ---------- UI helpers ----------

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI NewLabel(string name, Transform parent, string text, float size,
            FontStyles style, Color color)
        {
            var rt = NewRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = style == FontStyles.Bold ? _bold : _regular; // weight comes from the font asset
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, string label, float height, ButtonStyle style)
        {
            GetStyle(style, out var normal, out var pressed, out var textColor);
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _theme.ButtonSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = _theme.SlicedPpuMultiplier;
            img.color = Color.white; // tint is driven by the Button ColorBlock
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = normal;
            colors.selectedColor = normal;
            colors.pressedColor = pressed;
            colors.disabledColor = _theme.Disabled;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var text = NewLabel("Label", rt, label, _theme.ButtonSize, FontStyles.Normal, textColor);
            text.font = _regular;
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        private static void GetStyle(ButtonStyle style, out Color normal, out Color pressed, out Color text)
        {
            text = _theme.TextOnAccent;
            switch (style)
            {
                case ButtonStyle.Secondary:
                    normal = _theme.Secondary; pressed = _theme.SecondaryPressed; text = _theme.TextPrimary; break;
                case ButtonStyle.Success:
                    normal = _theme.Success; pressed = _theme.SuccessPressed; break;
                case ButtonStyle.Danger:
                    normal = _theme.Danger; pressed = _theme.DangerPressed; break;
                default:
                    normal = _theme.Primary; pressed = _theme.PrimaryPressed; break;
            }
        }

        private static Button NewBackButton(Transform parent)
        {
            var rt = NewRect("BtnBack", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(144, 144);
            rt.anchoredPosition = new Vector2(24, 0);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // touch area only
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            var iconRt = NewRect("Icon", rt);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(72, 72);
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = _theme.IconBack;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = _theme.TextPrimary;
            icon.raycastTarget = false;
            return button;
        }

        private static void EnsureFolders()
        {
            foreach (var dir in new[]
            {
                "Assets/_Project/Scenes", "Assets/_Project/Prefabs/Screens",
                "Assets/_Project/Prefabs/Popups", "Assets/_Project/Prefabs/Widgets"
            })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
    }
}
