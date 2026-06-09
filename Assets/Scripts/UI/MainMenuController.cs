using UnityEngine;

/// <summary>
/// Controla la navegación entre paneles del menú principal.
/// Cada botón llama al método correspondiente desde el Inspector (OnClick).
///
/// Setup en Unity — Canvas con 3 paneles hijos:
///   MainPanel    → título + botones principales
///   SettingsPanel → sliders de volumen
///   HowToPlayPanel → controles del juego
/// </summary>
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
        ShowMain();
    }

    // -------------------------------------------------------
    // Botones del panel principal
    // -------------------------------------------------------

    public void OnStartGame()
    {
        SceneFader.Instance.LoadScene(gameSceneName);
    }

    public void OnOpenSettings()
    {
        ShowSettings();
    }

    public void OnOpenHowToPlay()
    {
        ShowHowToPlay();
    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------
    // Botones de volver
    // -------------------------------------------------------

    public void OnBackFromSettings()
    {
        ShowMain();
    }

    public void OnBackFromHowToPlay()
    {
        ShowMain();
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
