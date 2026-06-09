using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton que persiste entre escenas y maneja el fade negro.
/// - Al cargar cualquier escena hace fade in automático (negro → transparente).
/// - Llamar LoadScene() para hacer fade out antes de cargar otra escena.
///
/// Setup en Unity:
/// 1. Crear un Canvas (Screen Space - Overlay, Sort Order 999)
/// 2. Agregar este script al Canvas
/// 3. Agregar un Image hijo que cubra toda la pantalla (Anchor: stretch/stretch, color negro)
/// 4. Asignar esa Image al campo Fade Image en el Inspector
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.6f;

    private void Awake()
    {
        // Singleton — si ya existe una instancia, destruir el duplicado
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Empezar en negro para que el fade in funcione al arrancar
        SetAlpha(1f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Se llama automáticamente cada vez que carga una escena nueva
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>
    /// Hace fade out y carga la escena por nombre.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoadRoutine(sceneName));
    }

    /// <summary>
    /// Hace fade out y carga la escena por índice.
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(FadeAndLoadRoutine(sceneIndex));
    }

    // -------------------------------------------------------
    // Coroutines internas
    // -------------------------------------------------------

    private IEnumerator FadeInRoutine()
    {
        SetAlpha(1f);
        fadeImage.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        fadeImage.raycastTarget = false;
    }

    private IEnumerator FadeOutRoutine()
    {
        fadeImage.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(1f);
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOutRoutine());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeAndLoadRoutine(int sceneIndex)
    {
        yield return StartCoroutine(FadeOutRoutine());
        SceneManager.LoadScene(sceneIndex);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}
