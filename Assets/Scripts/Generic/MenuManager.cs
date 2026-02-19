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
        // Инициализация аниматоров
        mainPanelAnimator = mainPanel.GetComponent<Animator>();
        optionsPanelAnimator = optionsPanel.GetComponent<Animator>();
        rebindPanelAnimator = rebindPanel.GetComponent<Animator>();

        // Убедимся, что меню видимо при старте
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        rebindPanel.SetActive(false);

        // Установим начальное состояние аниматоров
        mainPanelAnimator.SetBool("IsInvisible", false);
        optionsPanelAnimator.SetBool("IsInvisible", true);
        rebindPanelAnimator.SetBool("IsInvisible", true);

        // Инициализация кнопок
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(ShowOptions);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backToMainButton != null) backToMainButton.onClick.AddListener(HideOptions);
        if (rebindButton != null) rebindButton.onClick.AddListener(StartRebinding);
        if (cancelRebindButton != null) cancelRebindButton.onClick.AddListener(CancelRebinding);
    }

    public void StartGame()
    {
        // Загрузка игровой сцены
        Debug.Log("StartGame called!");
        UnityEngine.SceneManagement.SceneManager.LoadScene("TestBox_KAIN");
    }

    public void ShowOptions()
    {
        mainPanel.SetActive(true);
        mainPanelAnimator.SetBool("IsInvisible", false);
        optionsPanel.SetActive(true);
        optionsPanelAnimator.SetBool("IsInvisible", false);
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(true);
        optionsPanelAnimator.SetBool("IsInvisible", false);
        mainPanel.SetActive(true);
        mainPanelAnimator.SetBool("IsInvisible", false);
    }

    public void StartRebinding()
    {
        optionsPanel.SetActive(true);
        optionsPanelAnimator.SetBool("IsInvisible", false);
        rebindPanel.SetActive(true);
        rebindPanelAnimator.SetBool("IsInvisible", false);
    }

    public void CancelRebinding()
    {
        rebindPanel.SetActive(true);
        rebindPanelAnimator.SetBool("IsInvisible", false);
        optionsPanel.SetActive(true);
        optionsPanelAnimator.SetBool("IsInvisible", false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void TestButton()
    {
        Debug.Log("Button works!");
    }
}