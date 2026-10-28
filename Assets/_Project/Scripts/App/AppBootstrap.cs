using System;
using System.Collections.Generic;
using Kanji.Core.Files;
using Kanji.Core.Library;
using Kanji.Core.Navigation;
using Kanji.Core.Parsing;
using Kanji.Core.Text;
using Kanji.Infrastructure.Files;
using Kanji.Infrastructure.Storage;
using Kanji.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kanji.App
{
    /// <summary>Composition root: builds the navigator and wires screens.</summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private RectTransform _screenLayer;
        [SerializeField] private RectTransform _popupLayer;
        [SerializeField] private GameObject _popupBlocker;
        [SerializeField] private UiTheme _theme;
        [SerializeField] private TransitionOverlay _transition;
        [SerializeField] private ToastView _toast;
        [SerializeField] private StoryActionsPopup _storyPopup;
        [SerializeField] private KanjiPopup _kanjiPopup;
        [SerializeField] private ConfirmPopup _confirmPopup;
        [SerializeField] private NameInputPopup _nameInputPopup;
        [SerializeField] private ScreenView[] _screenPrefabs;
        [SerializeField] private BackButtonRouter _backButtonRouter;
        [SerializeField] private string _editorSampleFile = "Assets/_Project/Tests/Fixtures/Text/sample-utf8.txt"; // Editor only

        private void Awake()
        {
            Action quit = Application.Quit;
            if (EventSystem.current != null)
                EventSystem.current.pixelDragThreshold = DragThresholdPixels(Screen.dpi);

            var instances = new List<ScreenView>(_screenPrefabs.Length);
            var screens = new Dictionary<ScreenId, IScreen>();
            foreach (var prefab in _screenPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogError("[Kanji] AppBootstrap: null screen prefab in _screenPrefabs.", this);
                    continue;
                }
                if (screens.ContainsKey(prefab.Id))
                {
                    Debug.LogError("[Kanji] AppBootstrap: duplicate ScreenId " + prefab.Id + " (" + prefab.name + ").", this);
                    continue;
                }
                var view = Instantiate(prefab, _screenLayer, false);
                view.OnHide();
                screens[view.Id] = view;
                instances.Add(view);
            }

            var gate = new InputGate(() => Time.unscaledTimeAsDouble);
            _transition.Init(gate, _theme.ScreenFadeOutSeconds, _theme.ScreenFadeInSeconds);
            var popups = new PopupService(_popupBlocker, _toast.transform.parent, gate, _theme.PopupFadeSeconds);
            _storyPopup.Init(popups);
            _confirmPopup.Init(popups);
            _nameInputPopup.Init(popups);
            var navigator = new ScreenNavigator(screens, _transition);
            var decoder = new TextDecoder(TextDecoder.TryCreateShiftJis());
            Debug.Log($"[Kanji] Shift-JIS available: {decoder.SupportsShiftJis}");
            IFilePicker picker = new NativeFilePickerAdapter(TextDecoder.MaxBytes, _editorSampleFile);
            var fileSystem = new DiskFileSystem();
            var repository = new JsonStoryRepository(fileSystem, Application.persistentDataPath);
            var library = new StoryLibrary(repository, () => Guid.NewGuid().ToString("N"), () => DateTime.UtcNow);
            var difficultStore = new DifficultKanjiStore(new JsonDifficultKanjiRepository(fileSystem, Application.persistentDataPath));
            var difficultRepo = new JsonDifficultKanjiRepository(fileSystem, Application.persistentDataPath);
            RunDifficultMigration(library, difficultRepo);
            library.Deleted += id => difficultStore.DeleteStory(id);
            _kanjiPopup.Init(popups, null, _toast, difficultStore);
            LogStorageSummary(fileSystem, library);
            foreach (var view in instances)
            {
                switch (view)
                {
                    case HomeScreen home: home.Init(navigator, quit); break;
                    case StoryListScreen list: list.Init(navigator, library, difficultStore, _storyPopup, _confirmPopup, _nameInputPopup, _toast); break;
                    case DifficultListScreen dl: dl.Init(navigator, library, difficultStore, storyId => navigator.Push(ScreenId.DifficultGrid, storyId)); break;
                    case DifficultGridScreen grid: grid.Init(navigator, library, difficultStore, _kanjiPopup); break;
                    case ReaderScreen reader: reader.Init(navigator, library, _toast, _kanjiPopup); break;
                    case EmptyScreen empty: empty.Init(navigator); break;
                    case StatsScreen stats: stats.Init(navigator); break;
                    case LoadStoriesScreen load: load.Init(navigator, picker, decoder, new StoryParser(), library, _nameInputPopup, _toast); break;
                }
            }

            _backButtonRouter.Init(navigator, popups, quit, gate);
            if (!screens.ContainsKey(ScreenId.Home))
            {
                Debug.LogError("[Kanji] AppBootstrap: no Home screen registered; nothing to show.", this);
                return;
            }
            navigator.Push(ScreenId.Home);
        }

        // Idempotent: the repository's marker makes later launches return Skipped without touching anything.
        private void RunDifficultMigration(StoryLibrary library, JsonDifficultKanjiRepository repo)
        {
            try
            {
                MigrationResult r = new DifficultMigration(library, repo, repo).Run();
                if (r.Status == MigrationStatus.Done && r.Discarded > 0)
                    _toast.Show(string.Format(UiStrings.DifficultMigrationDiscarded, r.Discarded));
                else if (r.Status == MigrationStatus.Failed)
                    Debug.LogWarning("[Kanji] Difficult migration failed; it will be retried next launch.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kanji] Difficult migration threw: " + e.Message);
            }
        }

        /// <summary>About 8 dp in pixels; the default 10 px turns taps into drags on dense screens.</summary>
        private static int DragThresholdPixels(float dpi) =>
            dpi <= 0f ? 10 : Mathf.Max(10, Mathf.RoundToInt(8f * dpi / 160f));

        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogStorageSummary(IFileSystem fs, StoryLibrary library)
        {
            try
            {
                StoryListing listing = library.List();
                int tmp = 0;
                foreach (string _ in fs.ListFiles(System.IO.Path.Combine(Application.persistentDataPath, "stories"), ".tmp")) tmp++;
                IoLog.Info("startup stories: " + listing.Items.Count + " ok, " + listing.UnreadableCount +
                           " unreadable, " + tmp + " .tmp; persistentDataPath=" + Application.persistentDataPath);
            }
            catch (Exception e)
            {
                IoLog.Warn("startup summary failed: " + e.GetType().FullName + " " + e.Message);
            }
        }
    }
}
