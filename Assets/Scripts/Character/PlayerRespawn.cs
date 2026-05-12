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

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;

    public bool IsDead { get; private set; }

    private Coroutine respawnRoutine;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Awake()
    {
        if (respawnPoint != null)
        {
            spawnPosition = respawnPoint.position;
            spawnRotation = respawnPoint.rotation;
            return;
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        if (newRespawnPoint == null) return;

        respawnPoint  = newRespawnPoint;
        spawnPosition = newRespawnPoint.position;
        spawnRotation = newRespawnPoint.rotation;
    }

    public void Kill()
    {
        if (IsDead) return;

        IsDead = true;
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
}
