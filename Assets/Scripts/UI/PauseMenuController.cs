using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja la pausa con Escape. El canvas del pause usa MainMenuController
/// para los botones — Resume llama a PauseMenuController.Instance.Resume().
/// Poner en el GameManager (DontDestroyOnLoad).
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Tooltip("Prefab del canvas de pausa. Se instancia automáticamente al arrancar.")]
    [SerializeField] private GameObject pauseMenuPrefab;
    [SerializeField] private string mainMenuScene = "MainMenu";

    public bool IsPaused { get; private set; }

    private GameObject pauseMenuCanvas;
    private float lastToggleTime = -1f;
    private const float toggleCooldown = 0.2f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Instanciar el prefab como hijo de este GameObject
        if (pauseMenuPrefab != null)
        {
            pauseMenuCanvas = Instantiate(pauseMenuPrefab, transform);
            pauseMenuCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == mainMenuScene) return;
        if (Time.unscaledTime - lastToggleTime < toggleCooldown) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            lastToggleTime = Time.unscaledTime;
            if (IsPaused) Resume();
            else          Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        pauseMenuCanvas?.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pauseMenuCanvas?.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
