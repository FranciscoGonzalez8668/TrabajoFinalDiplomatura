using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton que persiste entre escenas (DontDestroyOnLoad).
/// Al entrar en una escena de nivel, instancia el Player y la Cámara si no existen,
/// los conecta y ubica al jugador en el SpawnPoint.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Datos de progreso")]
    [SerializeField] private GameProgress progress;

    [Header("Estados de nivel (uno por piso, en orden)")]
    [SerializeField] private LevelState[] levelStates;

    [Header("Prefabs de runtime")]
    [Tooltip("Prefab del Player. Se instancia una vez y persiste entre escenas.")]
    [SerializeField] private PlayerRespawn playerPrefab;
    [Tooltip("Prefab de la cámara. Se instancia una vez y persiste entre escenas.")]
    [SerializeField] private CameraController cameraPrefab;

    [Header("Configuración")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    // Instancias runtime (no en inspector — se crean en Bootstrap)
    private PlayerRespawn    playerRespawn;
    private CameraController cameraController;

    // -------------------------------------------------------
    // Ciclo de vida
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        progress.SyncIndexToCurrentScene();

        // MainMenu: sin jugador
        if (scene.name == progress.menuSceneName)
        {
            DisablePlayer();
            return;
        }

        // EndScene: la cinemática controla al jugador
        string lastScene = progress.levelSceneNames != null && progress.levelSceneNames.Length > 0
            ? progress.levelSceneNames[progress.levelSceneNames.Length - 1]
            : string.Empty;

        if (!string.IsNullOrEmpty(lastScene) && scene.name == lastScene)
        {
            DisablePlayer();
            return;
        }

        // Escena de nivel: asegurar que player y cámara existen, luego colocar
        Bootstrap();
    }

    // -------------------------------------------------------
    // Bootstrap — instancia y conecta player + cámara si no existen
    // -------------------------------------------------------

    private void Bootstrap()
    {
        EnsurePlayer();
        EnsureCamera();

        // Reconectar siempre — garantiza referencias válidas en cada nivel
        if (cameraController != null && playerRespawn != null)
            cameraController.InjectPlayerReferences(playerRespawn.transform);

        EnablePlayer();
        PlacePlayerAtSpawn();
    }

    private bool EnsurePlayer()
    {
        if (playerRespawn != null) return false;

        if (playerPrefab == null)
        {
            Debug.LogError("[LevelManager] Falta asignar playerPrefab en el inspector.");
            return false;
        }

        // Instanciar inactivo para que Start() corra DESPUÉS de que la cámara esté lista
        playerRespawn = Instantiate(playerPrefab);
        playerRespawn.gameObject.SetActive(false);
        DontDestroyOnLoad(playerRespawn.gameObject);
        return true;
    }

    private bool EnsureCamera()
    {
        if (cameraController != null) return false;

        // Puede que ya exista como singleton (arrancando desde una escena de nivel en el editor)
        cameraController = CameraController.Instance;
        if (cameraController != null) return false;

        if (cameraPrefab == null)
        {
            Debug.LogError("[LevelManager] Falta asignar cameraPrefab en el inspector.");
            return false;
        }

        cameraController = Instantiate(cameraPrefab);
        DontDestroyOnLoad(cameraController.gameObject);
        return true;
    }

    // -------------------------------------------------------
    // Player enable / disable
    // -------------------------------------------------------

    public void DisablePlayer()
    {
        if (playerRespawn != null)
            playerRespawn.gameObject.SetActive(false);
    }

    public void EnablePlayer()
    {
        if (playerRespawn != null)
            playerRespawn.gameObject.SetActive(true);
    }

    // -------------------------------------------------------
    // API pública
    // -------------------------------------------------------

    public void ReturnToSpawn() => PlacePlayerAtSpawn();

    public void RestartCurrentLevel()
    {
        ResetCurrentLevelState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AdvanceToNextLevel()
    {
        progress.MarkCurrentCompleted();

        // Desactivar el player antes de cambiar de escena
        // así OnEnable corre limpio cuando se reactiva en el nuevo nivel
        DisablePlayer();

        if (!progress.HasNextLevel)
        {
            SceneFader.Instance.LoadScene(progress.menuSceneName);
            return;
        }

        progress.AdvanceLevel();
        SceneManager.LoadScene(progress.CurrentSceneName);
    }

    // -------------------------------------------------------
    // Privados
    // -------------------------------------------------------

    private void PlacePlayerAtSpawn()
    {
        if (playerRespawn == null) return;

        GameObject spawnObj = GameObject.FindWithTag(spawnPointTag);
        if (spawnObj == null)
        {
            Debug.LogWarning($"[LevelManager] No hay objeto con tag '{spawnPointTag}' en la escena.");
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
