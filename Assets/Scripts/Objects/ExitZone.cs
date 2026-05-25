using UnityEngine;

/// <summary>
/// Trigger que avanza al siguiente piso cuando el jugador entra.
/// Requiere Collider con Is Trigger = true.
/// Llama a LevelManager.Instance.AdvanceToNextLevel().
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("Tag del jugador para filtrar el trigger.")]
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        // Asegurarse de que el collider sea trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[ExitZone] El Collider no era trigger — se corrigió automáticamente.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (LevelManager.Instance == null)
        {
            Debug.LogError("[ExitZone] LevelManager.Instance es null. ¿Está la escena base cargada?");
            return;
        }

        LevelManager.Instance.AdvanceToNextLevel();
    }
}
