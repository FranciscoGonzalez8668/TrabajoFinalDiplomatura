using UnityEngine;

/// <summary>
/// Zona de checkpoint: cuando el jugador entra al trigger, mueve el punto de respawn aquí.
/// Agregar un Collider con Is Trigger = true en el mismo GameObject.
/// El spawn exacto se configura con el hijo SpawnPoint (o usa el transform propio si no hay hijo).
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Transform exacto donde aparece el jugador al respawnear. Si es null usa este GameObject.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Renderer opcional para mostrar estado activado/desactivado (ej. un ícono o luz).")]
    [SerializeField] private Renderer visualIndicator;
    [SerializeField] private Material activatedMaterial;

    [Header("Audio")]
    [SerializeField] private AudioClip activateClip;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup mixerGroup;

    public bool IsActivated { get; private set; }

    private void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsActivated) return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null) return;

        Activate(respawn);
    }

    private void Activate(PlayerRespawn respawn)
    {
        IsActivated = true;
        respawn.SetRespawnPoint(spawnPoint);

        if (visualIndicator != null && activatedMaterial != null)
            visualIndicator.material = activatedMaterial;

        if (activateClip != null)
        {
            GameObject go = new GameObject("CheckpointSFX");
            go.transform.position = transform.position;
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip            = activateClip;
            src.outputAudioMixerGroup = mixerGroup;
            src.spatialBlend    = 1f;
            src.Play();
            Destroy(go, activateClip.length);
        }
    }

    private void OnDrawGizmos()
    {
        Transform t = spawnPoint != null ? spawnPoint : transform;
        DrawSpawnGizmo(t, new Color(0.2f, 1f, 0.4f));
    }

    private static void DrawSpawnGizmo(Transform t, Color color)
    {
        Gizmos.color = color;

        // Círculo en el plano XZ
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float   a0 = i       * Mathf.PI * 2f / segments;
            float   a1 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 p0 = t.position + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.4f;
            Vector3 p1 = t.position + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.4f;
            Gizmos.DrawLine(p0, p1);
        }

        // Flecha hacia forward
        Vector3 tip = t.position + t.forward * 0.7f;
        Gizmos.DrawLine(t.position, tip);
        Gizmos.DrawLine(tip, tip - t.forward * 0.2f + t.right * 0.12f);
        Gizmos.DrawLine(tip, tip - t.forward * 0.2f - t.right * 0.12f);
    }
}
