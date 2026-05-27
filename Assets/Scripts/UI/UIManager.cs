using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Singleton que gestiona los elementos de UI del juego.
/// DontDestroyOnLoad — persiste entre escenas junto al player.
///
/// Dos paneles:
///   - Hint: muestra "[ E ]  {texto}" mientras el jugador está cerca de un interactuable.
///   - Notificación: mensaje temporal que desaparece solo después de unos segundos.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Hint de interacción")]
    [Tooltip("Panel que contiene el texto de hint (ej: '[ E ]  Recoger Llave').")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [Header("Notificaciones")]
    [Tooltip("Panel que muestra mensajes temporales de feedback.")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float notificationDuration = 2.5f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ClearInteractionHint();
        if (notificationPanel != null) notificationPanel.SetActive(false);
    }

    // --- Hint de interacción ---

    /// <summary>Muestra "[ E ]  {hint}" en el panel de hint.</summary>
    public void SetInteractionHint(string hint)
    {
        if (hintPanel == null || hintText == null) return;
        hintText.text = $"[ E ]  {hint}";
        hintPanel.SetActive(true);
    }

    /// <summary>Oculta el panel de hint.</summary>
    public void ClearInteractionHint()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    // --- Notificaciones ---

    /// <summary>
    /// Muestra un mensaje temporal. Si ya había uno activo, lo reemplaza.
    /// </summary>
    public void ShowNotification(string message, float duration = -1f)
    {
        if (notificationPanel == null || notificationText == null) return;

        if (hideRoutine != null) StopCoroutine(hideRoutine);

        notificationText.text = message;
        notificationPanel.SetActive(true);

        float d = duration > 0f ? duration : notificationDuration;
        hideRoutine = StartCoroutine(HideNotificationAfter(d));
    }

    private IEnumerator HideNotificationAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (notificationPanel != null) notificationPanel.SetActive(false);
        hideRoutine = null;
    }
}
