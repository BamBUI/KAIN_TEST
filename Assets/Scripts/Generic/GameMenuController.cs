using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Generic
{
    /// <summary>
    /// Контроллер игрового меню (пауза, возврат в главное меню, рестарт сцены)
    /// Размещается в игровых сценах (TestBox_KAIN, Level_1, etc.)
    /// </summary>
    public class GameMenuController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset inputActions;
        private InputAction menuAction;        // ← F1: Главное меню
        private InputAction restartAction;     // ← + F2: Рестарт сцены

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            // Инициализация Input Actions
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            }

            if (inputActions != null)
            {
                // - menuAction = inputActions.FindAction("Menu");
                menuAction = inputActions.FindAction("Menu");        // ← F1
                restartAction = inputActions.FindAction("RestartScene"); // ← + F2

                if (menuAction != null)
                {
                    menuAction.performed += OnMenuPressed;
                }
                else
                {
                    Debug.LogWarning("[GameMenuController] Menu action not found in InputSystem!");
                }

                // + Подписка на рестарт
                if (restartAction != null)
                {
                    restartAction.performed += OnRestartPressed;
                }
                else
                {
                    Debug.LogWarning("[GameMenuController] RestartScene action not found in InputSystem!");
                }
            }
            else
            {
                Debug.LogError("[GameMenuController] InputActionAsset not found!");
            }
        }

        private void OnEnable()
        {
            // Включаем действия при активации объекта
            if (menuAction != null)
            {
                menuAction.Enable();
            }
            if (restartAction != null)
            {
                restartAction.Enable(); // ← + Включаем рестарт
            }
        }

        private void OnDisable()
        {
            // Отключаем действия при деактивации
            if (menuAction != null)
            {
                menuAction.Disable();
            }
            if (restartAction != null)
            {
                restartAction.Disable(); // ← + Отключаем рестарт
            }
        }

        private void OnDestroy()
        {
            // Отписка от событий
            if (menuAction != null)
            {
                menuAction.performed -= OnMenuPressed;
            }
            // + Отписка от рестарта
            if (restartAction != null)
            {
                restartAction.performed -= OnRestartPressed;
            }
        }

        // ━━━ F1: ВОЗВРАТ В ГЛАВНОЕ МЕНЮ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnMenuPressed(InputAction.CallbackContext context)
        {
            Debug.Log("[GameMenuController] Menu button pressed (F1)!");

            // Воспроизвести звук кнопки
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }

            ReturnToMainMenu();
        }

        // ━━━ F2: РЕСТАРТ ТЕКУЩЕЙ СЦЕНЫ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnRestartPressed(InputAction.CallbackContext context)
        {
            Debug.Log("[GameMenuController] Restart button pressed (F2)!");

            // Воспроизвести звук кнопки
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }

            RestartCurrentScene();
        }

        /// <summary>
        /// Вернуться в главное меню
        /// </summary>
        public void ReturnToMainMenu()
        {
            Debug.Log($"[GameMenuController] Loading scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// + Перезагрузить текущую сцену
        /// </summary>
        public void RestartCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[GameMenuController] Restarting scene: {currentSceneName}");
            SceneManager.LoadScene(currentSceneName);
        }

        /// <summary>
        /// Публичный метод для вызова из UI кнопок (если понадобится)
        /// </summary>
        public void OnReturnToMenuButton()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
            ReturnToMainMenu();
        }

        /// <summary>
        /// + Публичный метод для UI кнопки рестарта (если понадобится в меню паузы)
        /// </summary>
        public void OnRestartButton()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSound();
            }
            RestartCurrentScene();
        }
    }
}