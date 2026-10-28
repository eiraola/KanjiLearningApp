using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kanji.Editor
{
    /// <summary>Reproducible Android development APK build.</summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/KanjiLearning-dev.apk";
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string IconPath = "Assets/_Project/Art/Cover/KanjiBook_Icon.png";
        private const string IconFgPath = "Assets/_Project/Art/Cover/KanjiBook_IconFg.png";
        private const string IconBgPath = "Assets/_Project/Art/Cover/KanjiBook_IconBg.png";

        [MenuItem("Kanji/Build Android APK")]
        public static void BuildDevelopmentApk() => BuildDevelopmentApk(OutputPath);

        public static void BuildDevelopmentApk(string outputPath)
        {
            ApplyPlayerSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;
            Debug.Log("[Kanji] Android build " + summary.result + ": " + summary.outputPath + " (" +
                      (summary.totalSize / (1024f * 1024f)).ToString("F1") + " MB, " + summary.totalWarnings +
                      " warnings, " + summary.totalErrors + " errors, " + summary.totalTime + ")");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        internal static void ApplyPlayerSettings()
        {
            var target = NamedBuildTarget.Android;
            PlayerSettings.companyName = "BattleCroissant";
            PlayerSettings.productName = "KanjiLearning";
            PlayerSettings.SetApplicationIdentifier(target, "com.battlecroissant.kanjilearning");
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);

            // No Unity splash/logo at startup (allowed on every plan since Unity 6).
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            ApplyIcon();

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = false;
        }

        private static void ApplyIcon()
        {
            var icon = LoadIconTexture(IconPath);
            if (icon == null) return;
            // Default icon: Android scales it to every legacy/round size.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

#if UNITY_ANDROID
            // Adaptive icon (Android 8+): dark theme background + book inside the safe zone.
            var fg = LoadIconTexture(IconFgPath);
            var bg = LoadIconTexture(IconBgPath);
            if (fg == null || bg == null) return;
            var kind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var platformIcon in adaptive) platformIcon.SetTextures(bg, fg);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, adaptive);
#endif
        }

        private static Texture2D LoadIconTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[Kanji] App icon not found at " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Default || importer.mipmapEnabled ||
                !importer.alphaIsTransparency || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
