using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Options Panel")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backToMainButton;
    [SerializeField] private Button rebindButton;

    [Header("Rebind Panel")]
    [SerializeField] private GameObject rebindPanel;
    [SerializeField] private Button cancelRebindButton;

    private Animator mainPanelAnimator;
    private Animator optionsPanelAnimator;
    private Animator rebindPanelAnimator;

    private void Awake()
    {
        // ━━━ РАЗБЛОКИРОВКА КУРСОРА ДЛЯ МЕНЮ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // + Важно: после Startup сцены курсор может оставаться заблокированным
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[MenuManager] Cursor unlocked for menu navigation");

        // ━━━ ИНИЦИАЛИЗАЦИЯ АНИМАТОРОВ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        mainPanelAnimator = mainPanel.GetComponent<Animator>();
        optionsPanelAnimator = optionsPanel.GetComponent<Animator>();
        rebindPanelAnimator = rebindPanel.GetComponent<Animator>();

        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        rebindPanel.SetActive(false);

        // + ПРОВЕРКА: есть ли контроллер у аниматора (защита от предупреждений)
        if (mainPanelAnimator != null && mainPanelAnimator.runtimeAnimatorController != null)
        {
            mainPanelAnimator.SetBool("IsInvisible", false);
        }
        else
        {
            Debug.LogWarning("[MenuManager] mainPanel Animator Controller not assigned!");
        }

        if (optionsPanelAnimator != null && optionsPanelAnimator.runtimeAnimatorController != null)
        {
            optionsPanelAnimator.SetBool("IsInvisible", true);
        }
        else
        {
            Debug.LogWarning("[MenuManager] optionsPanel Animator Controller not assigned!");
        }

        if (rebindPanelAnimator != null && rebindPanelAnimator.runtimeAnimatorController != null)
        {
            rebindPanelAnimator.SetBool("IsInvisible", true);
        }
        else
        {
            Debug.LogWarning("[MenuManager] rebindPanel Animator Controller not assigned!");
        }

        // ━━━ ИНИЦИАЛИЗАЦИЯ КНОПОК ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(ShowOptions);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backToMainButton != null) backToMainButton.onClick.AddListener(HideOptions);
        if (rebindButton != null) rebindButton.onClick.AddListener(StartRebinding);
        if (cancelRebindButton != null) cancelRebindButton.onClick.AddListener(CancelRebinding);

        Debug.Log("[MenuManager] Awake complete - menu ready");
    }

    public void StartGame()
    {
        Debug.Log("[MenuManager] StartGame called");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("TestBox_KAIN");
    }

    public void ShowOptions()
    {
        Debug.Log("[MenuManager] ShowOptions called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSound();

        mainPanel.SetActive(true);
        if (mainPanelAnimator != null && mainPanelAnimator.runtimeAnimatorController != null)
            mainPanelAnimator.SetBool("IsInvisible", false);

        optionsPanel.SetActive(true);
        if (optionsPanelAnimator != null && optionsPanelAnimator.runtimeAnimatorController != null)
            optionsPanelAnimator.SetBool("IsInvisible", false);
    }

    public void HideOptions()
    {
        Debug.Log("[MenuManager] HideOptions called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSound();

        optionsPanel.SetActive(true);
        if (optionsPanelAnimator != null && optionsPanelAnimator.runtimeAnimatorController != null)
            optionsPanelAnimator.SetBool("IsInvisible", false);

        mainPanel.SetActive(true);
        if (mainPanelAnimator != null && mainPanelAnimator.runtimeAnimatorController != null)
            mainPanelAnimator.SetBool("IsInvisible", false);
    }

    public void StartRebinding()
    {
        Debug.Log("[MenuManager] StartRebinding called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSound();

        optionsPanel.SetActive(true);
        if (optionsPanelAnimator != null && optionsPanelAnimator.runtimeAnimatorController != null)
            optionsPanelAnimator.SetBool("IsInvisible", false);

        rebindPanel.SetActive(true);
        if (rebindPanelAnimator != null && rebindPanelAnimator.runtimeAnimatorController != null)
            rebindPanelAnimator.SetBool("IsInvisible", false);
    }

    public void CancelRebinding()
    {
        Debug.Log("[MenuManager] CancelRebinding called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSound();

        rebindPanel.SetActive(true);
        if (rebindPanelAnimator != null && rebindPanelAnimator.runtimeAnimatorController != null)
            rebindPanelAnimator.SetBool("IsInvisible", false);

        optionsPanel.SetActive(true);
        if (optionsPanelAnimator != null && optionsPanelAnimator.runtimeAnimatorController != null)
            optionsPanelAnimator.SetBool("IsInvisible", false);
    }

    public void ExitGame()
    {
        Debug.Log("[MenuManager] ExitGame called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSound();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void TestButton()
    {
        Debug.Log("[MenuManager] TestButton works!");
    }
}