using System.IO;
using Kanji.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Kanji.Editor
{
    /// <summary>Configures the imported Dark UI subset (import settings, atlas) and the UiTheme asset. Idempotent.</summary>
    public static class DarkUiAssetSetup
    {
        public const string ArtDir = "Assets/_Project/Art/DarkUI";
        public const string ThemePath = "Assets/_Project/Settings/UiTheme_Dark.asset";
        public const string AtlasPath = ArtDir + "/DarkUI.spriteatlasv2";

        private readonly struct SpriteDef
        {
            public readonly string RelativePath;
            public readonly Vector4 Border; // L, B, R, T
            public readonly int MaxSize;

            public SpriteDef(string relativePath, Vector4 border, int maxSize)
            {
                RelativePath = relativePath;
                Border = border;
                MaxSize = maxSize;
            }

            public string FullPath => ArtDir + "/" + RelativePath;
        }

        private static readonly SpriteDef[] Sprites =
        {
            new SpriteDef("Buttons/btn_pill.png", new Vector4(64, 64, 64, 64), 256),
            new SpriteDef("Buttons/btn_pill_round.png", new Vector4(64, 64, 64, 64), 256),
            new SpriteDef("Buttons/btn_medium.png", new Vector4(64, 64, 64, 64), 256),
            new SpriteDef("Inputs/input_field.png", new Vector4(64, 0, 64, 0), 256),
            new SpriteDef("Panels/divider.png", new Vector4(2, 2, 2, 2), 256),
            new SpriteDef("Panels/circle_2px.png", Vector4.zero, 256),
            new SpriteDef("Panels/circle_4px.png", Vector4.zero, 256),
            new SpriteDef("Icons/icon_back.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_close.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_trash.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_check.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_cross.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_x.png", Vector4.zero, 128), // plus rotated 45 deg: the package has no X glyph
            new SpriteDef("Icons/icon_plus.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_books.png", Vector4.zero, 128),
            new SpriteDef("Icons/icon_book.png", Vector4.zero, 128),
        };

        [MenuItem("Kanji/Setup Dark UI Assets")]
        public static void Setup()
        {
            foreach (var def in Sprites) ConfigureSprite(def);
            AssetDatabase.Refresh();
            ConfigureAtlas();
            ConfigureTheme();
            AssetDatabase.SaveAssets();
            Debug.Log("[Kanji] Dark UI assets set up.");
        }

        private static void ConfigureSprite(SpriteDef def)
        {
            if (!(AssetImporter.GetAtPath(def.FullPath) is TextureImporter importer))
            {
                Debug.LogError("[Kanji] Missing sprite: " + def.FullPath);
                return;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            string before = EditorJsonUtility.ToJson(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = def.Border;
            settings.spritePixelsPerUnit = 100f;
            settings.mipmapEnabled = false;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.filterMode = FilterMode.Bilinear;
            settings.alphaIsTransparency = true;
            bool changed = before != EditorJsonUtility.ToJson(settings);
            if (!changed && importer.maxTextureSize == def.MaxSize
                && importer.textureCompression == TextureImporterCompression.Uncompressed) return;

            importer.SetTextureSettings(settings);
            importer.maxTextureSize = def.MaxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // the atlas compresses (ASTC)
            importer.SaveAndReimport();
        }

        private static void ConfigureAtlas()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath) == null && !File.Exists(AtlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<Object>(ArtDir) });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }

            if (!(AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)) return;

            var packing = importer.packingSettings;
            var android = importer.GetPlatformSettings("Android");
            bool upToDate = packing.padding == 4 && !packing.enableRotation && !packing.enableTightPacking
                && android.overridden && android.format == TextureImporterFormat.ASTC_6x6;
            if (upToDate) return;

            packing.padding = 4;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            importer.packingSettings = packing;
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformSettings(android);
            importer.SaveAndReimport();
        }

        private static void ConfigureTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var so = new SerializedObject(theme);
            LinkSprite(so, "_buttonSprite", "Buttons/btn_pill.png");
            LinkSprite(so, "_panelSprite", "Buttons/btn_pill.png");
            LinkSprite(so, "_inputSprite", "Inputs/input_field.png");
            LinkSprite(so, "_dividerSprite", "Panels/divider.png");
            LinkSprite(so, "_circleSprite", "Panels/circle_2px.png");
            LinkSprite(so, "_iconBack", "Icons/icon_back.png");
            LinkSprite(so, "_iconClose", "Icons/icon_close.png");
            LinkSprite(so, "_iconTrash", "Icons/icon_trash.png");
            LinkSprite(so, "_iconCheck", "Icons/icon_check.png");
            LinkSprite(so, "_iconCross", "Icons/icon_x.png"); // icon_cross is a dagger, icon_close a prohibition sign
            LinkSprite(so, "_iconPlus", "Icons/icon_plus.png");
            LinkSprite(so, "_iconBooks", "Icons/icon_books.png");
            LinkSprite(so, "_iconBook", "Icons/icon_book.png");
            so.FindProperty("_regular").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontSetup.RegularPath);
            so.FindProperty("_bold").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontSetup.BoldPath);
            so.ApplyModifiedPropertiesWithoutUndo(); // colors are never touched here
            EditorUtility.SetDirty(theme);
        }

        private static void LinkSprite(SerializedObject so, string field, string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + relativePath);
            if (sprite == null) Debug.LogError("[Kanji] Sprite not found for " + field + ": " + relativePath);
            so.FindProperty(field).objectReferenceValue = sprite;
        }
    }
}
