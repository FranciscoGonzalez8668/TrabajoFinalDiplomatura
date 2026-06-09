using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja muerte y respawn del jugador.
/// Al morir: deshabilita input, para todas las abilities, resetea el motor.
/// Al respawnear: teletransporta al punto de respawn y reactiva todo.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private WallRunAbility wallRun;
    [SerializeField] private WallJumpAbility wallJump;
    [SerializeField] private VerticalWallRunAbility verticalWallRun;
    [SerializeField] private LedgeGrabAbility ledgeGrab;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform respawnPoint;

    [Header("Muerte")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSFX sfx;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;

    public bool IsDead { get; private set; }

    private Coroutine respawnRoutine;
    private Vector3    spawnPosition;
    private Quaternion spawnRotation;

    // Punto inicial del nivel — no cambia con los checkpoints
    private Transform  levelStartPoint;
    private Vector3    levelStartPosition;
    private Quaternion levelStartRotation;

    private void Awake()
    {
        if (respawnPoint != null)
        {
            spawnPosition = respawnPoint.position;
            spawnRotation = respawnPoint.rotation;
        }
        else
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        levelStartPoint    = respawnPoint;
        levelStartPosition = spawnPosition;
        levelStartRotation = spawnRotation;
    }

    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        if (newRespawnPoint == null) return;

        respawnPoint  = newRespawnPoint;
        spawnPosition = newRespawnPoint.position;
        spawnRotation = newRespawnPoint.rotation;
    }

    /// <summary>
    /// Vuelve el punto de respawn al inicio del nivel, descartando todos los checkpoints activados.
    /// </summary>
    public void ResetToLevelStart()
    {
        respawnPoint  = levelStartPoint;
        spawnPosition = levelStartPosition;
        spawnRotation = levelStartRotation;
    }

    /// <summary>
    /// Teletransporta al jugador al punto de respawn actual sin delay ni muerte.
    /// Usado por LevelManager al cargar una escena o al ejecutar ReturnToSpawn.
    /// </summary>
    public void TeleportToSpawn()
    {
        // Si hay una muerte en curso, la cancelamos
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        IsDead = false;
        Respawn();
    }

    public void Kill()
    {
        if (IsDead) return;

        IsDead = true;
        sfx?.PlayDeath();
        animator?.SetTrigger("Death");
        ApplyDeathState();
        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private void ApplyDeathState()
    {
        input.SetInputEnabled(false);
        wallRun.ForceStop();
        wallJump.ForceStop();
        ledgeGrab.ForceStop();
        motor.ResetMotion();
        stateMachine.ForceSetState(PlayerStateMachine.PlayerState.Idle);
        verticalWallRun.ForceStop();
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        Vector3 targetPosition    = respawnPoint != null ? respawnPoint.position : spawnPosition;
        Quaternion rawRotation    = respawnPoint != null ? respawnPoint.rotation  : spawnRotation;
        Quaternion targetRotation = Quaternion.Euler(0f, rawRotation.eulerAngles.y, 0f);

        wallRun.ForceStop();
        wallJump.ForceStop();
        ledgeGrab.ForceStop();
        verticalWallRun.ForceStop();
        motor.TeleportTo(targetPosition);
        transform.rotation = targetRotation;
        stateMachine.ForceSetState(PlayerStateMachine.PlayerState.Idle);
        input.SetInputEnabled(true);

        if (cameraController != null)
            cameraController.SnapToPlayerForward();

        animator?.ResetTrigger("Death");
        animator?.Play("Idle", 0, 0f);

        IsDead         = false;
        respawnRoutine = null;
    }

    private void OnDisable()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
    }

    private void OnDrawGizmos()
    {
        Transform t = respawnPoint != null ? respawnPoint : transform;
        Gizmos.color = new Color(1f, 0.6f, 0.1f);

        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float   a0 = i       * Mathf.PI * 2f / segments;
            float   a1 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 p0 = t.position + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.4f;
            Vector3 p1 = t.position + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.4f;
            Gizmos.DrawLine(p0, p1);
        }

        Vector3 tip = t.position + t.forward * 0.7f;
        Gizmos.DrawLine(t.position, tip);
        Gizmos.DrawLine(tip, tip - t.forward * 0.2f + t.right * 0.12f);
        Gizmos.DrawLine(tip, tip - t.forward * 0.2f - t.right * 0.12f);
    }
}
