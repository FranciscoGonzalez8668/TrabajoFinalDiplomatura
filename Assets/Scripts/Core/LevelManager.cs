using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton que persiste entre escenas (DontDestroyOnLoad).
/// Gestiona la transición entre pisos, el respawn del jugador y el reseteo del estado de nivel.
///
/// Responsabilidades:
///   - ReturnToSpawn()        → solo mueve al jugador, no toca el estado del nivel
///   - RestartCurrentLevel()  → resetea el LevelState del piso actual y recarga la escena
///   - AdvanceToNextLevel()   → marca piso completo en GameProgress y carga la siguiente escena
///
/// Al cargar cualquier escena busca el tag "SpawnPoint" y sitúa al jugador ahí.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // --- Singleton ---

    public static LevelManager Instance { get; private set; }

    // --- Inspector ---

    [Header("Datos de progreso")]
    [SerializeField] private GameProgress progress;

    [Header("Estados de nivel (uno por piso, en orden)")]
    [Tooltip("Cada LevelState corresponde al piso del mismo índice en GameProgress.levelSceneNames.")]
    [SerializeField] private LevelState[] levelStates;

    [Header("Referencias del jugador")]
    [Tooltip("Raíz del Player GameObject — se marca DontDestroyOnLoad.")]
    [SerializeField] private PlayerRespawn playerRespawn;

    [Header("Configuración de escena")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    // --- Ciclo de vida ---

    private void Awake()
    {
        // Patrón singleton: si ya existe una instancia, esta sobra
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // El player también sobrevive entre escenas
        if (playerRespawn != null)
            DontDestroyOnLoad(playerRespawn.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Al cargar cualquier escena: ubica al player en el SpawnPoint de esa escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // En la EndScene deshabilitamos al jugador — la cinemática lo controla
        if (scene.name == progress.levelSceneNames[progress.levelSceneNames.Length - 1])
        {
            DisablePlayer();
            return;
        }

        PlacePlayerAtSpawn();
    }

    /// <summary>Deshabilita el GameObject del jugador. Usado en la cinemática final.</summary>
    public void DisablePlayer()
    {
        if (playerRespawn != null)
            playerRespawn.gameObject.SetActive(false);
    }

    /// <summary>Vuelve a habilitar el jugador.</summary>
    public void EnablePlayer()
    {
        if (playerRespawn != null)
            playerRespawn.gameObject.SetActive(true);
    }

    // --- API pública ---

    /// <summary>
    /// Teletransporta al jugador al SpawnPoint de la escena actual.
    /// No resetea nada del estado del nivel — los items y switches quedan igual.
    /// </summary>
    public void ReturnToSpawn()
    {
        PlacePlayerAtSpawn();
    }

    /// <summary>
    /// Resetea el LevelState del piso actual y recarga la escena desde cero.
    /// Útil para reiniciar un piso si algo se bloqueó o el jugador quiere empezar de nuevo.
    /// </summary>
    public void RestartCurrentLevel()
    {
        ResetCurrentLevelState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Marca el piso actual como completado y carga el siguiente.
    /// Si no hay siguiente, carga la escena de victoria.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        progress.MarkCurrentCompleted();

        if (!progress.HasNextLevel)
        {
            SceneFader.Instance.LoadScene(progress.menuSceneName);
            return;
        }

        progress.AdvanceLevel();
        SceneManager.LoadScene(progress.CurrentSceneName);
    }

    // --- Privados ---

    private void PlacePlayerAtSpawn()
    {
        if (playerRespawn == null) return;

        GameObject spawnObj = GameObject.FindWithTag(spawnPointTag);
        if (spawnObj == null)
        {
            Debug.LogWarning($"[LevelManager] No se encontró ningún objeto con tag '{spawnPointTag}' en la escena.");
            return;
        }

        playerRespawn.SetRespawnPoint(spawnObj.transform);
        playerRespawn.TeleportToSpawn();
    }

    private void ResetCurrentLevelState()
    {
        int index = progress.CurrentLevelIndex;
        if (levelStates == null || index >= levelStates.Length) return;

        LevelState state = levelStates[index];
        if (state != null) state.Reset();
    }
}
