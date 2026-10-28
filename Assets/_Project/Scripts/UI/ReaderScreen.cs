using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Kanji.Core.Library;
using Kanji.Core.Model;
using Kanji.Core.Reading;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Kanji.UI
{
    /// <summary>
    /// Scrollable story reader. Re-reads the story from disk, builds rich-text chunks (Core) and fills one
    /// TextMeshPro chunk per frame so the first text shows early and the frame budget is shared.
    /// </summary>
    public sealed class ReaderScreen : ScreenView
    {
        private const float LoadingDelaySeconds = 0.3f;

        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _content;
        [SerializeField] private TextMeshProUGUI _chunkTemplate;
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private UiTheme _theme;
        [SerializeField] private int _maxChunkChars = 4000;

        private readonly List<TextMeshProUGUI> _chunks = new List<TextMeshProUGUI>();
        private ScreenNavigator _navigator;
        private StoryLibrary _library;
        private ToastView _toast;
        private KanjiPopup _kanjiPopup;
        private Coroutine _openRoutine;
        private Coroutine _loadingRoutine;
        private Story _story; // maps link ids to entries (OnEntryTapped)
        private ReadingSession _session; // green/red answers for the open story; not persisted

        // Scroll perf counters (editor / development builds only).
        private int _scrollFrames, _scrollOver33;
        private float _scrollMaxMs, _scrollTotalMs;

        internal static StoryRichTextBuilder CreateBuilder(UiTheme theme, int maxChunkChars)
            => new StoryRichTextBuilder(theme.LinkHex, maxChunkChars);

        public void Init(ScreenNavigator navigator, StoryLibrary library, ToastView toast, KanjiPopup kanjiPopup)
        {
            if (_theme == null) Debug.LogError("ReaderScreen: UiTheme not assigned.", this);
            _navigator = navigator;
            _library = library;
            _toast = toast;
            _kanjiPopup = kanjiPopup;
            _backButton.onClick.AddListener(CloseReading);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            ClearChunks();
            _content.anchoredPosition = Vector2.zero;
            _scroll.StopMovement();
            if (_loadingText != null) _loadingText.gameObject.SetActive(false);
            ResetScrollStats();

            var info = args as StoryInfo;
            _title.text = info != null ? info.Name : string.Empty;
            StopRoutines();
            _openRoutine = StartCoroutine(Open(info));
        }

        public override void OnHide()
        {
            StopRoutines();
            if (_kanjiPopup != null) _kanjiPopup.Close();
            LogScrollStats();
            ClearChunks();
            _story = null;
            _session = null;
            base.OnHide();
        }

        public override bool HandleBack()
        {
            CloseReading();
            return true;
        }

        /// <summary>Stories with kanji end on the stats screen (replacing the reader); others go straight back.</summary>
        internal void CloseReading()
        {
            if (_navigator == null || _navigator.Current != (IScreen)this) return;
            if (_session != null && _story != null && _story.Entries.Count > 0)
                _navigator.Replace(ScreenId.Stats, _session.BuildStats());
            else
                _navigator.Pop();
        }

        private IEnumerator Open(StoryInfo info)
        {
            yield return null; // leave ScreenNavigator.Push before any Pop

            var total = Stopwatch.StartNew();
            _loadingRoutine = StartCoroutine(ShowLoadingAfterDelay());

            IReadOnlyList<RichTextChunk> chunks = null;
            Story story = null;
            long loadMs = 0, buildMs = 0;
            string failure = null;
            try
            {
                if (info == null) failure = "args is not a StoryInfo";
                else
                {
                    var result = _library.Load(info.Id);
                    loadMs = total.ElapsedMilliseconds;
                    if (!result.IsSuccess || result.Value == null)
                        failure = "load failed: " + result.Error + " " + result.Message;
                    else
                    {
                        story = result.Value;
                        var builder = CreateBuilder(_theme, _maxChunkChars);
                        chunks = builder.Build(story);
                        buildMs = total.ElapsedMilliseconds - loadMs;
                    }
                }
            }
            catch (Exception e)
            {
                failure = e.GetType().Name + ": " + e.Message;
            }

            if (failure != null)
            {
                Debug.LogWarning("[Kanji] Reader could not open story: " + failure, this);
                HideLoading();
                _toast.Show(UiStrings.StoryOpenFailed);
                _openRoutine = null;
                _navigator.Pop();
                yield break;
            }

            _story = story;
            _session = new ReadingSession(story);
            long firstVisibleMs = -1;
            int frames = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                if (i > 0) { yield return null; frames++; }
                AddChunk(i, chunks[i].Markup);
                if (i == 0)
                {
                    HideLoading();
                    yield return null; // layout / first render
                    frames++;
                    firstVisibleMs = total.ElapsedMilliseconds;
                }
            }
            if (chunks.Count == 0) HideLoading();
            yield return null; // layout of the last chunk
            frames++;
            _openRoutine = null;
            LogOpen(info, chunks, loadMs, buildMs, firstVisibleMs, total.ElapsedMilliseconds, frames);
        }

        private void AddChunk(int index, string markup)
        {
            TextMeshProUGUI chunk;
            if (index < _chunks.Count) chunk = _chunks[index];
            else
            {
                chunk = Instantiate(_chunkTemplate, _content);
                chunk.GetComponent<LinkClickHandler>().EntryTapped += OnEntryTapped;
                _chunks.Add(chunk);
            }
            chunk.text = markup;
            chunk.gameObject.SetActive(true);
        }

        private void OnEntryTapped(int index)
        {
            long start = Stopwatch.GetTimestamp();
            if (_story == null || index < 0 || index >= _story.Entries.Count)
            {
                Debug.LogWarning("[Kanji] Tapped link has no entry: " + index, this);
                return;
            }
            KanjiEntry entry = _story.Entries[index];
            ReadingSession session = _session;
            _kanjiPopup.Show(entry, _story.Id, result => OnAnswered(session, entry, result));
            LogPopupOpen(index, start);
        }

        private static void OnAnswered(ReadingSession session, KanjiEntry entry, KanjiPopupResult result)
        {
            if (session == null) return;
            if (result == KanjiPopupResult.Knew) session.Record(entry, KanjiAnswer.Knew);
            else if (result == KanjiPopupResult.DidntKnow) session.Record(entry, KanjiAnswer.DidntKnow);
            // Dismissed is not an answer: the kanji keeps its previous answer (or counts as passed).
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private static void LogPopupOpen(int index, long startTimestamp)
        {
            double ms = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            Debug.Log($"[Kanji][Perf] kanji-popup idx={index} openMs={ms:F1}");
        }

        private void ClearChunks()
        {
            for (int i = 0; i < _chunks.Count; i++)
            {
                _chunks[i].text = string.Empty;
                _chunks[i].gameObject.SetActive(false);
            }
        }

        private void StopRoutines()
        {
            if (_openRoutine != null) StopCoroutine(_openRoutine);
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            _openRoutine = _loadingRoutine = null;
        }

        private IEnumerator ShowLoadingAfterDelay()
        {
            yield return new WaitForSecondsRealtime(LoadingDelaySeconds);
            if (_loadingText != null) _loadingText.gameObject.SetActive(true);
            _loadingRoutine = null;
        }

        private void HideLoading()
        {
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            _loadingRoutine = null;
            if (_loadingText != null) _loadingText.gameObject.SetActive(false);
        }


        // ---------- Perf logging ([Kanji][Perf]) ----------

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (_scroll == null || _scroll.velocity == Vector2.zero) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            _scrollFrames++;
            _scrollTotalMs += ms;
            if (ms > 33f) _scrollOver33++;
            if (ms > _scrollMaxMs) _scrollMaxMs = ms;
        }
#endif

        private void ResetScrollStats()
        {
            _scrollFrames = _scrollOver33 = 0;
            _scrollMaxMs = _scrollTotalMs = 0f;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void LogScrollStats()
        {
            if (_scrollFrames == 0) return;
            Debug.Log($"[Kanji][Perf] reader-scroll frames={_scrollFrames} over33ms={_scrollOver33} " +
                      $"maxMs={_scrollMaxMs:F1} avgMs={_scrollTotalMs / _scrollFrames:F1}");
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void LogOpen(StoryInfo info, IReadOnlyList<RichTextChunk> chunks, long loadMs, long buildMs,
            long firstVisibleMs, long totalMs, int frames)
        {
            int chars = 0, links = 0;
            for (int i = 0; i < chunks.Count; i++) { chars += chunks[i].VisibleLength; links += chunks[i].LinkCount; }
            int atlases = _chunkTemplate.font != null ? _chunkTemplate.font.atlasTextureCount : 0;
            Debug.Log($"[Kanji][Perf] reader-open id={info.Id} chars={chars} chunks={chunks.Count} links={links} " +
                      $"loadMs={loadMs} buildMs={buildMs} firstVisibleMs={firstVisibleMs} totalMs={totalMs} " +
                      $"frames={frames} monoMB={Profiler.GetMonoUsedSizeLong() / 1048576} " +
                      $"reservedMB={Profiler.GetTotalReservedMemoryLong() / 1048576} atlases={atlases}");
        }
    }
}
