using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Generic
{
    [System.Serializable]
    public class SettingsData
    {
        public InputBindingData moveBinding;
        public InputBindingData attackBinding;
        public int resolutionIndex;
        public bool isFullscreen;
        public bool vsync;
        public int fpsLimit;
        public int antialiasing;
    }

    [System.Serializable]
    public class InputBindingData
    {
        public string actionName;
        public string[] paths;
    }

    public class SettingsManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        private string settingsPath;
        private bool isFirstLoad = true;

        private void Awake()
        {
            // Гарантируем единственный экземпляр во всех сценах
            if (FindObjectsByType<SettingsManager>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            settingsPath = Path.Combine(Application.persistentDataPath, "gameSettings.json");
            LoadSettings();
        }

        public void SaveSettings()
        {
            SettingsData data = new SettingsData
            {
                moveBinding = GetBindingData(inputActions.FindAction("Move")),
                attackBinding = GetBindingData(inputActions.FindAction("Attack")),
                resolutionIndex = GetResolutionIndex(),
                isFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed,
                vsync = QualitySettings.vSyncCount > 0,
                fpsLimit = Application.targetFrameRate,
                antialiasing = QualitySettings.antiAliasing
            };

            File.WriteAllText(settingsPath, JsonUtility.ToJson(data, true));
        }

        private InputBindingData GetBindingData(InputAction action)
        {
            InputBindingData binding = new InputBindingData
            {
                actionName = action.name,
                paths = new string[action.bindings.Count]
            };

            for (int i = 0; i < action.bindings.Count; i++)
                binding.paths[i] = action.bindings[i].path;

            return binding;
        }

        public void LoadSettings()
        {
            // Загружаем настройки только один раз при старте игры
            if (!isFirstLoad) return;

            if (!File.Exists(settingsPath))
            {
                SaveSettings(); // Создаём файл с настройками по умолчанию
                isFirstLoad = false;
                return;
            }

            SettingsData data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));

            // Применяем только графические настройки
            // Привязки НЕ перезаписываем - используем базовые из ассета
            ApplyGraphicsSettings(data);

            isFirstLoad = false;
        }

        public void LoadInputBindings(SettingsData data)
        {
            // Явный метод для загрузки привязок (вызывается только при переназначении)
            if (data.moveBinding != null)
                ApplyBindingData(data.moveBinding);
            if (data.attackBinding != null)
                ApplyBindingData(data.attackBinding);
        }

        private void ApplyBindingData(InputBindingData binding)
        {
            InputAction action = inputActions.FindAction(binding.actionName);
            if (action == null) return;

            // Отключаем действие перед изменением привязок
            bool wasEnabled = action.enabled;
            if (wasEnabled)
                action.Disable();

            // Удаляем все переопределения
            action.RemoveAllBindingOverrides();

            // Добавляем новые привязки
            for (int i = 0; i < binding.paths.Length; i++)
            {
                action.AddBinding(binding.paths[i]);
            }

            // Включаем действие обратно
            if (wasEnabled)
                action.Enable();
        }

        private void ApplyGraphicsSettings(SettingsData data)
        {
            if (data.resolutionIndex < Screen.resolutions.Length)
            {
                Resolution res = Screen.resolutions[data.resolutionIndex];
                Screen.SetResolution(res.width, res.height,
                    data.isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            QualitySettings.vSyncCount = data.vsync ? 1 : 0;
            Application.targetFrameRate = data.fpsLimit;
            QualitySettings.antiAliasing = data.antialiasing;
        }

        private int GetResolutionIndex()
        {
            Resolution current = Screen.currentResolution;
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                if (Screen.resolutions[i].width == current.width &&
                    Screen.resolutions[i].height == current.height)
                    return i;
            }
            return 0;
        }
    }
}