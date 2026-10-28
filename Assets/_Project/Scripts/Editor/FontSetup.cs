using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Kanji.Editor
{
    /// <summary>Creates the dynamic Noto Sans JP TMP font assets and wires TMP Settings (reproducible).</summary>
    public static class FontSetup
    {
        public const string RegularPath = "Assets/_Project/Fonts/NotoSansJP-Regular SDF.asset";
        public const string BoldPath = "Assets/_Project/Fonts/NotoSansJP-Bold SDF.asset";

        private const int SamplingSize = 48;
        private const int Padding = 5;
        private const int AtlasSize = 2048; // ~1200 glyphs per page; multi-atlas adds pages on demand

        [MenuItem("Kanji/Setup Fonts")]
        public static void Setup()
        {
            DeleteObsolete("Assets/_Project/Fonts/NotoSansJP-Medium SDF.asset");
            DeleteObsolete("Assets/_Project/Fonts/NotoSansJP-Medium.ttf");

            var regular = CreateDynamic("Assets/_Project/Fonts/NotoSansJP-Regular.ttf", RegularPath);
            var bold = CreateDynamic("Assets/_Project/Fonts/NotoSansJP-Bold.ttf", BoldPath);

            // Bold falls back (locally) to Regular for any glyph it lacks.
            SetFallback(bold, regular);

            var settings = TMP_Settings.instance;
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
            so.FindProperty("m_fallbackFontAssets").arraySize = 0; // global fallback list stays empty
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Kanji] Fonts set up. TMP Settings: " + AssetDatabase.GetAssetPath(settings));
        }

        private static TMP_FontAsset CreateDynamic(string ttfPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                if (existing.atlasWidth == AtlasSize && existing.atlasHeight == AtlasSize
                    && existing.faceInfo.pointSize == SamplingSize && existing.isMultiAtlasTexturesEnabled)
                    return existing;
                AssetDatabase.DeleteAsset(assetPath); // different parameters: recreate
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            var fa = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            for (int i = 0; i < fa.atlasTextures.Length; i++)
            {
                fa.atlasTextures[i].name = fa.name + " Atlas" + (i == 0 ? "" : " " + i);
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static void DeleteObsolete(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }

        private static void SetFallback(TMP_FontAsset target, TMP_FontAsset fallback)
        {
            target.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(target);
        }
    }
}
