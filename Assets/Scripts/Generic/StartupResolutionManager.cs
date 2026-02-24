using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.IO;

public class StartupResolutionManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown ResolutionDropdown;
    [SerializeField] private TMP_Dropdown GraphicsDropdown;
    [SerializeField] private Toggle FullscreenToggle;
    [SerializeField] private Button StartButton;
    [SerializeField] private Button QuitButton;

    [Header("Settings")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool applySettingsImmediately = false; // + Для отладки: применить сразу без coroutine

    private Resolution[] resolutions;
    private PlayerInput playerInput;

    private void Awake()
    {
        // ━━━ ОТКЛЮЧЕНИЕ ВВОДА ИГРОКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Log("[Startup] PlayerInput DISABLED - UI control enabled");
        }
        else
        {
            Log("[Startup] No PlayerInput found in scene");
        }

        // ━━━ КУРСОР ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Log($"[Startup] Cursor: locked={Cursor.lockState}, visible={Cursor.visible}");

        // ━━━ ИНИЦИАЛИЗАЦИЯ UI ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        InitializeDropdowns();

        if (StartButton != null)
            StartButton.onClick.AddListener(StartGame);

        if (QuitButton != null)
            QuitButton.onClick.AddListener(QuitGame);

        Log("[Startup] Awake complete - ready for input");
    }

    private void InitializeDropdowns()
    {
        // ━━━ РАЗРЕШЕНИЯ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (ResolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            ResolutionDropdown.ClearOptions();

            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRateRatio}Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            ResolutionDropdown.AddOptions(options);
            ResolutionDropdown.value = currentResolutionIndex;
            ResolutionDropdown.RefreshShownValue();
            Log($"[Startup] Loaded {resolutions.Length} resolutions");
        }

        // ━━━ ГРАФИКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (GraphicsDropdown != null)
        {
            GraphicsDropdown.ClearOptions();
            GraphicsDropdown.AddOptions(System.Linq.Enumerable.ToList(QualitySettings.names));
            GraphicsDropdown.value = QualitySettings.GetQualityLevel();
            GraphicsDropdown.RefreshShownValue();
        }

        // ━━━ FULLSCREEN ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (FullscreenToggle != null)
        {
            FullscreenToggle.isOn = Screen.fullScreen;
        }
    }

    public void ApplySettings()
    {
        string logMessage = $"[ApplySettings] Time: {System.DateTime.Now}\n";

        // ━━━ РАЗРЕШЕНИЕ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (ResolutionDropdown != null && resolutions != null && resolutions.Length > 0)
        {
            Resolution selectedResolution = resolutions[ResolutionDropdown.value];

            // + ИСПРАВЛЕНИЕ: используем FullScreenMode вместо bool
            FullScreenMode fullScreenMode = FullscreenToggle.isOn
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            Screen.SetResolution(
                selectedResolution.width,
                selectedResolution.height,
                fullScreenMode
            );

            // + Принудительно обновляем fullscreen (для надёжности в билде)
            Screen.fullScreenMode = fullScreenMode;

            logMessage += $"Resolution: {selectedResolution.width}x{selectedResolution.height}\n";
            logMessage += $"Fullscreen: {FullscreenToggle.isOn} ({fullScreenMode})\n";
            logMessage += $"Actual Screen: {Screen.width}x{Screen.height}\n";
            logMessage += $"Actual FullscreenMode: {Screen.fullScreenMode}\n";
        }
        else
        {
            logMessage += "ResolutionDropdown or resolutions is null/empty!\n";
        }

        // ━━━ КАЧЕСТВО ГРАФИКИ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (GraphicsDropdown != null)
        {
            QualitySettings.SetQualityLevel(GraphicsDropdown.value);
            logMessage += $"Quality: {QualitySettings.names[GraphicsDropdown.value]}\n";
        }

        // + ПИШЕМ ЛОГ В ФАЙЛ (работает в билде!)
        WriteLogToFile(logMessage);

        // + Также выводим в Console (для редактора)
        Debug.Log(logMessage);
    }

    private void StartGame()
    {
        Log("[Startup] StartGame called");

        if (applySettingsImmediately)
        {
            // Для отладки: применяем сразу
            ApplySettings();
            FinishStartup();
        }
        else
        {
            // Для билда: применяем с задержкой через coroutine
            StartCoroutine(ApplySettingsAndLoadScene());
        }
    }

    private IEnumerator ApplySettingsAndLoadScene()
    {
        ApplySettings();

        // + Ждём 1-2 кадра для применения настроек экрана (критично для билда!)
        yield return null;
        yield return null;

        FinishStartup();
    }

    private void FinishStartup()
    {
        // ━━━ ВКЛЮЧЕНИЕ ВВОДА ИГРОКА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (playerInput != null)
        {
            playerInput.enabled = true;
            Log("[Startup] PlayerInput ENABLED - game control restored");
        }

        // ━━━ СКРЫТИЕ КУРСОРА ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Log($"[Startup] Cursor: locked={Cursor.lockState}, visible={Cursor.visible}");

        // ━━━ ЗАГРУЗКА СЦЕНЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        Log($"[Startup] Loading scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void QuitGame()
    {
        Log("[Startup] QuitGame called");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ━━━ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private void Log(string message)
    {
        Debug.Log(message);
        // + В билде также пишем в файл для отладки
#if !UNITY_EDITOR
        WriteLogToFile(message + "\n");
#endif
    }

    private void WriteLogToFile(string message)
    {
        try
        {
            string logPath = Path.Combine(Application.persistentDataPath, "startup_log.txt");
            File.AppendAllText(logPath, message + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Startup] Failed to write log: {e.Message}");
        }
    }

    // ━━━ ОТПИСКА ОТ СОБЫТИЙ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private void OnDestroy()
    {
        if (StartButton != null)
            StartButton.onClick.RemoveListener(StartGame);

        if (QuitButton != null)
            QuitButton.onClick.RemoveListener(QuitGame);
    }
}