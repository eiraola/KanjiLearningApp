using System;
using System.IO;
using Kanji.Core.Files;
using UnityEngine;

namespace Kanji.Infrastructure.Files
{
    /// <summary>IFilePicker backed by the NativeFilePicker plugin. In the Editor it can read a fixed sample without a modal dialog.</summary>
    public sealed class NativeFilePickerAdapter : IFilePicker
    {
        private readonly long _maxBytes;
        private readonly string _editorSamplePath;
        private bool _busy;

        public NativeFilePickerAdapter(long maxBytes, string editorSamplePath)
        {
            _maxBytes = maxBytes;
            _editorSamplePath = editorSamplePath;
        }

        public void PickTextFile(Action<PickResult> onCompleted)
        {
            if (_busy || NativeFilePicker.IsFilePickerBusy())
            {
                onCompleted(PickResult.Failed(PickError.Busy));
                return;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_editorSamplePath) && File.Exists(_editorSamplePath))
            {
                onCompleted(ReadFile(_editorSamplePath));
                return;
            }
#endif
            _busy = true;
            try
            {
                NativeFilePicker.PickFile(path => OnPicked(path, onCompleted),
                    NativeFilePicker.ConvertExtensionToFileType("txt"));
            }
            catch (Exception)
            {
                _busy = false;
                onCompleted(PickResult.Failed(PickError.Unreadable));
            }
        }

        private void OnPicked(string path, Action<PickResult> onCompleted)
        {
            _busy = false;
            if (path == null)
            {
                onCompleted(PickResult.Cancelled());
                return;
            }

            PickResult result;
            try
            {
                result = ReadFile(path);
            }
            finally
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                TryDelete(path);
#endif
            }
            onCompleted(result);
        }

        private PickResult ReadFile(string path)
        {
            string name = null;
            long size = 0;
            try
            {
                name = Path.GetFileNameWithoutExtension(path);
                size = new FileInfo(path).Length;
                if (size > _maxBytes)
                    return PickResult.Failed(PickError.TooLarge, name, size);
                return PickResult.Picked(name, File.ReadAllBytes(path));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            {
                return PickResult.Failed(PickError.Unreadable, name, size);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void TryDelete(string path)
        {
            try
            {
                // The plugin returns the user's original path when it can read it; only delete our own cache copy.
                string cacheRoot = Path.GetFullPath(Application.temporaryCachePath) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(path).StartsWith(cacheRoot, StringComparison.Ordinal))
                    File.Delete(path);
            }
            catch (Exception) { /* cache cleanup is best-effort */ }
        }
#endif
    }
}
