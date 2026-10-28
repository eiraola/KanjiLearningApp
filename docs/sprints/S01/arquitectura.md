# S01 — Arquitectura (T01, T02, T04; nota T03)

> Marco general: `docs/arquitectura/ARQUITECTURA.md`. Aquí solo lo que se construye en S01.
> Unity 6000.6.2f1 · uGUI 2.6.0 (TMP incluido) · Input System 1.20.0 · Test Framework 1.8.0.

## T01 — Estructura, asmdefs, escena, bootstrap

### Carpetas a crear
```
Assets/_Project/
  Scenes/Main.unity
  Scripts/Core/            Kanji.Core.asmdef
  Scripts/UI/              Kanji.UI.asmdef
  Scripts/App/             Kanji.App.asmdef
  Scripts/Editor/          Kanji.Editor.asmdef
  Prefabs/Screens/         Home, LoadStories, StoryList, DifficultList (.prefab)
  Prefabs/Popups/  Prefabs/Widgets/   (vacías, con .gitkeep)
  Fonts/                   (T03)
  Settings/
  Tests/EditMode/Core/     Kanji.Core.Tests.asmdef
  Tests/PlayMode/          Kanji.UI.Tests.asmdef
```
`Kanji.Infrastructure` (+ sus tests) **no se crea en S01**: no hay código de disco hasta S02; se añade entonces y `Kanji.App` pasa a referenciarla.

### asmdefs (referencias por nombre; copiar tal cual)

| asmdef | refs | noEngineReferences | autoReferenced | Testable desde |
|---|---|---|---|---|
| Kanji.Core | — | **true** | false | Kanji.Core.Tests |
| Kanji.UI | Core, UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem | false | false | Kanji.UI.Tests |
| Kanji.App | Core, UI, UnityEngine.UI, Unity.TextMeshPro | false | false | — |
| Kanji.Editor | — (solo Editor) | false | false | — |
| Kanji.Core.Tests | Core + runner | true | false | — |
| Kanji.UI.Tests | Core, UI + runner | false | false | — |

TMP en Unity 6.x vive en `com.unity.ugui`, pero su ensamblado sigue llamándose **`Unity.TextMeshPro`** (namespace `TMPro`). Input System = **`Unity.InputSystem`**. uGUI = **`UnityEngine.UI`**.
"Testables" se materializa con `[assembly: InternalsVisibleTo("Kanji.Core.Tests")]` en un `AssemblyInfo.cs` de Core (y `Kanji.UI.Tests` en UI); preferir API pública y no necesitarlo.

`Scripts/Core/Kanji.Core.asmdef`
```json
{
  "name": "Kanji.Core",
  "rootNamespace": "Kanji.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`Scripts/UI/Kanji.UI.asmdef`
```json
{
  "name": "Kanji.UI",
  "rootNamespace": "Kanji.UI",
  "references": ["Kanji.Core", "UnityEngine.UI", "Unity.TextMeshPro", "Unity.InputSystem"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Scripts/App/Kanji.App.asmdef`
```json
{
  "name": "Kanji.App",
  "rootNamespace": "Kanji.App",
  "references": ["Kanji.Core", "Kanji.UI", "UnityEngine.UI", "Unity.TextMeshPro"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Scripts/Editor/Kanji.Editor.asmdef`
```json
{
  "name": "Kanji.Editor",
  "rootNamespace": "Kanji.Editor",
  "references": [],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Tests/EditMode/Core/Kanji.Core.Tests.asmdef`
```json
{
  "name": "Kanji.Core.Tests",
  "rootNamespace": "Kanji.Core.Tests",
  "references": ["Kanji.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`Tests/PlayMode/Kanji.UI.Tests.asmdef`
```json
{
  "name": "Kanji.UI.Tests",
  "rootNamespace": "Kanji.UI.Tests",
  "references": ["Kanji.Core", "Kanji.UI", "UnityEngine.UI", "Unity.TextMeshPro", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```
Kanji.UI.Tests puede quedar vacío en S01 (si hay tiempo: un test que carga `Main`, pulsa un botón de Home y comprueba la pantalla activa).

### Escena `Main` (única en Build Settings) — jerarquía
```
Main
├─ Main Camera                 (la de la plantilla URP; fondo sólido)
├─ EventSystem                 + InputSystemUIInputModule (no StandaloneInputModule)
├─ AppBootstrap                + AppBootstrap (Kanji.App)
└─ Canvas                      Screen Space - Overlay · CanvasScaler: Scale With Screen Size,
   │                           1080×1920, Match 0.5 · GraphicRaycaster
   ├─ ScreenLayer              RectTransform stretch (0,0)-(1,1)
   │  └─ (instancias de pantallas, creadas por AppBootstrap)
   └─ PopupLayer               stretch, vacío en S01 (siempre por encima de ScreenLayer)
```
Player Settings → *Active Input Handling* = **Input System Package (New)** (o *Both* si algo de la plantilla lo exige).

### AppBootstrap (composition root) en S01
```csharp
namespace Kanji.App {
  public sealed class AppBootstrap : MonoBehaviour {
    [SerializeField] private RectTransform _screenLayer;
    [SerializeField] private RectTransform _popupLayer;      // sin uso en S01
    [SerializeField] private ScreenView[] _screenPrefabs;    // Home, LoadStories, StoryList, DifficultList
    [SerializeField] private BackButtonRouter _backButtonRouter;
    private void Awake();   // compone todo (abajo)
  }
}
```
`Awake`: instancia cada prefab una vez bajo `_screenLayer` (inactivo) → `new ScreenNavigator(screensById)` → `homeScreen.Init(navigator, quit)` y `EmptyScreen.Init(navigator)` en el resto → `_backButtonRouter.Init(navigator, quit)` → `navigator.Push(ScreenId.Home)`. `quit` = `Application.Quit` pasado como `Action`, único sitio que toca la API real. Sin repositorios ni servicios de Core en S01.

## T02 — Navegación, Home, Atrás/Salir

**Registro de pantallas:** prefabs en `Prefabs/Screens/`, referenciados en `AppBootstrap._screenPrefabs` e instanciados una vez (coherente con ARQUITECTURA y construible con herramientas de Editor: crear prefab → arrastrar al array). Cada prefab declara su `ScreenId` en un campo serializado.

```csharp
// Kanji.Core — C# puro, testeable
namespace Kanji.Core.Navigation {
  public sealed class NavigationStack<T> {
    public int Count { get; }
    public T Current { get; }                 // InvalidOperationException si vacía
    public bool CanPop { get; }               // Count > 1 (la raíz nunca se saca)
    public void Push(T item);
    public bool TryPop(out T popped);         // false si solo queda la raíz
    public void Replace(T item);
    public void Clear();
  }
}

// Kanji.UI
namespace Kanji.UI {
  public enum ScreenId { Home, LoadStories, StoryList, Reader, Stats, DifficultList }

  public interface IScreen {
    ScreenId Id { get; }
    void OnShow(object args);
    void OnHide();
    bool HandleBack();                        // true = consumido; false = Pop por defecto
  }

  public abstract class ScreenView : MonoBehaviour, IScreen {   // base común (activa/desactiva GO)
    [SerializeField] private ScreenId _id;
    public ScreenId Id => _id;
    public virtual void OnShow(object args);  // gameObject.SetActive(true)
    public virtual void OnHide();             // gameObject.SetActive(false)
    public virtual bool HandleBack() => false;
  }

  public sealed class ScreenNavigator {                          // C# normal, no MonoBehaviour
    public ScreenNavigator(IReadOnlyDictionary<ScreenId, IScreen> screens);
    public IScreen Current { get; }
    public bool CanGoBack { get; }
    public event Action<ScreenId> Changed;
    public void Push(ScreenId id, object args = null);           // OnHide actual, OnShow nueva
    public bool Pop();                                           // false en la raíz
    public void Replace(ScreenId id, object args = null);
  }

  public sealed class BackButtonRouter : MonoBehaviour {
    public void Init(ScreenNavigator navigator, Action quit);
    private void Update();   // Keyboard.current?.escapeKey.wasPressedThisFrame →
                             // (S02+: popup superior) → Current.HandleBack() → si false: Pop() → si false: quit()
  }

  public sealed class HomeScreen : ScreenView {
    [SerializeField] private Button _loadStoriesButton, _storyListButton, _difficultListButton, _exitButton;
    public void Init(ScreenNavigator navigator, Action quit);   // los 3 primeros → Push(id); Salir → quit()
  }

  public sealed class EmptyScreen : ScreenView {   // placeholder para LoadStories/StoryList/DifficultList
    [SerializeField] private TMP_Text _title; [SerializeField] private Button _backButton;
    public void Init(ScreenNavigator navigator);   // botón Atrás en pantalla → navigator.Pop()
  }
}
```
Botones ≥ 144 px de alto; `SafeAreaFitter` (Kanji.UI, `MonoBehaviour` que ajusta anchors a `Screen.safeArea`) en el root de cada prefab de pantalla. Textos de Home los define ux-ui.

**Test EditMode mínimo** (`Tests/EditMode/Core/NavigationStackTests.cs`): Push A, Push B → `Current == B`; `TryPop` → `true`, `Current == A`; `TryPop` en raíz → `false` y `Current == A` (esta es la regla que hace que Atrás en Inicio cierre la app); `Replace` no cambia `Count`; `Current` en pila vacía lanza.

## T03 — Nota
Fuente en `Assets/_Project/Fonts/` (`NotoSansJP-Regular` + `NotoSansJP-Regular SDF` dinámico, Atlas Population Mode *Dynamic*, multi-atlas activado). Importar *TMP Essential Resources*; **TMP Settings debe quedarse en una carpeta `Resources`** (`Assets/TextMesh Pro/Resources/TMP Settings.asset`, no en `_Project/Settings`): ahí *Default Font Asset* = NotoSansJP SDF y, si se añade Bold u otra, va en *Fallback Font Assets*. Corregir ARQUITECTURA en ese punto.

## T04 — Android y APK de desarrollo

PlayerSettings (aplicados por código en el script de build, para que sean reproducibles; también fijarlos una vez a mano en el Editor):
| Ajuste | Valor |
|---|---|
| Company / Product | BattleCroissant / KanjiLearning |
| Package name | `com.battlecroissant.kanjilearning` |
| Scripting backend | IL2CPP |
| Target architectures | ARM64 solo (desmarcar ARMv7) |
| Minimum API Level | **26 (Android 8.0)** — por encima del mínimo de Unity 6, cubre >95 % de dispositivos |
| Target API Level | Automatic (el más alto instalado) |
| Orientation | Portrait (sin autorrotación) |
| Render outside safe area | true (la `SafeAreaFitter` se encarga) |
| Managed stripping | Minimal (Shift-JIS de S02 puede necesitar `link.xml`) |
| Build | Development Build = true, Script Debugging = false, formato APK (no AAB) |
| Active Input Handling | Input System Package |

Script de build en `Scripts/Editor/AndroidBuild.cs` (asmdef `Kanji.Editor`, solo Editor):
```csharp
namespace Kanji.Editor {
  public static class AndroidBuild {
    public const string OutputPath = "Builds/Android/KanjiLearning-dev.apk";
    [MenuItem("Kanji/Build/Android Dev APK")]
    public static void BuildDevelopmentApk();   // ApplyPlayerSettings → BuildPipeline.BuildPlayer(
                                                //   scenes: { "Assets/_Project/Scenes/Main.unity" }, Android,
                                                //   BuildOptions.Development) → log del BuildReport;
                                                //   en batchmode EditorApplication.Exit(ok ? 0 : 1)
    internal static void ApplyPlayerSettings(); // tabla de arriba vía PlayerSettings/EditorUserBuildSettings
  }
}
```
CLI (Editor cerrado; `Builds/` fuera de `Assets` y en `.gitignore`):
```
unity build --project . --target Android --execute-method Kanji.Editor.AndroidBuild.BuildDevelopmentApk
# equivalente directo:
"<Editor>\Unity.exe" -batchmode -quit -projectPath F:\UnityProjects\KanjiLearning -buildTarget Android ^
  -executeMethod Kanji.Editor.AndroidBuild.BuildDevelopmentApk -logFile Builds\build-android.log
```
Instalar: `adb install -r Builds/Android/KanjiLearning-dev.apk`. Requisito: módulo Android Build Support (+ OpenJDK, SDK, NDK) para 6000.6.2f1.

## Cobertura de requisitos (plan S01)
T01 → asmdefs/escena/bootstrap (Core sin Unity: `noEngineReferences`). T02 → `ScreenNavigator`+`NavigationStack`, Home 4 botones, Atrás/Salir vía `quit`. T03 → TMP Settings + fuente. T04 → `AndroidBuild` + PlayerSettings.
