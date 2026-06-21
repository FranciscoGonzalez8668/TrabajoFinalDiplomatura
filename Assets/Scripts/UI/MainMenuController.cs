using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Escena a cargar")]
    [SerializeField] private string gameSceneName = "Level_01";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        ShowMain();
    }

    // -------------------------------------------------------
    // Botones
    // -------------------------------------------------------

    public void OnStartGame()
    {
        SceneFader.Instance.LoadScene(gameSceneName);
    }

    // Botón Resume del pause menu — llama al PauseMenuController
    // Estos métodos se asignan en el OnClick del botón dentro del prefab
    // usando "this" (el propio MainMenuController del canvas) — no necesitan
    // referencia externa porque usan el singleton PauseMenuController.Instance
    public void OnResume()
    {
        PauseMenuController.Instance?.Resume();
    }

    public void OnQuit()
    {
        PauseMenuController.Instance?.Resume();
        SceneFader.Instance.LoadScene("MainMenu");
    }

    public void OnOpenSettings() => ShowSettings();
    public void OnOpenHowToPlay() => ShowHowToPlay();
    public void OnBackFromSettings() => ShowMain();
    public void OnBackFromHowToPlay() => ShowMain();

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------
    // Navegación interna
    // -------------------------------------------------------

    private void ShowMain()
    {
        mainPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
    }

    private void ShowSettings()
    {
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        howToPlayPanel?.SetActive(false);
    }

    private void ShowHowToPlay()
    {
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(true);
    }
}
