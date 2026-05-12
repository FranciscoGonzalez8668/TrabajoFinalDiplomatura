using UnityEngine;

/// <summary>
/// Coordina los estados del jugador y el ciclo de vida de las habilidades de movimiento.
/// Es el único responsable de llamar a CanStart, StartAbility, UpdateAbility y StopAbility.
/// No mueve al personaje directamente — delega a CharacterMotor.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private CameraController cameraController;

    [Header("Abilities")]
    [SerializeField] private WallRunAbility wallRun;
    [SerializeField] private WallJumpAbility wallJump;
    [SerializeField] private LedgeGrabAbility ledgeGrab;

    [SerializeField] private VerticalWallRunAbility verticalWallRun;

    public PlayerState CurrentState { get; private set; }

    public enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        WallRunning,
        Sprinting,
        LedgeGrabbing,
        VerticalWallRunning
    }

    private IMovementAbility[] abilities;
    private Vector3 moveDirection;

    private void Awake()
    {
        abilities = new IMovementAbility[] { wallRun, wallJump, ledgeGrab, verticalWallRun };
    }

    private void Update()
    {
        UpdateMoveDirection();
        UpdateAbilities();
        UpdateState();
        HandleState();
    }

    // -------------------------------------------------------
    // ABILITIES
    // -------------------------------------------------------

    private void UpdateAbilities()
    {
        foreach (IMovementAbility ability in abilities)
        {
            if (ability.IsActive)
                ability.UpdateAbility();
            else if (ability.CanStart())
                ability.StartAbility();
        }
    }

    // -------------------------------------------------------
    // ESTADO
    // -------------------------------------------------------

    private void UpdateMoveDirection()
    {
        Vector3 forward = cameraController.CameraForward * input.MoveInput.y;
        Vector3 right   = cameraController.CameraRight   * input.MoveInput.x;
        moveDirection = (forward + right).normalized;
    }

    private void UpdateState()
    {
        if (TryHandleJump())     return;
        if (TryHandleWallJump()) return;

        switch (CurrentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
            case PlayerState.Sprinting:
                UpdateGroundedState();
                break;

            case PlayerState.Jumping:
                if (ledgeGrab.IsActive)
                    SetState(PlayerState.LedgeGrabbing);
                else if (motor.HitCeiling)
                    SetState(PlayerState.Falling);
                else if (motor.VerticalVelocity < 0f)
                    SetState(PlayerState.Falling);
                break;

            case PlayerState.Falling:
                if (motor.IsGrounded)
                    UpdateGroundedState();
                else if (ledgeGrab.IsActive)
                    SetState(PlayerState.LedgeGrabbing);
                break;

            case PlayerState.WallRunning:
                if (!wallRun.IsActive)
                    SetState(motor.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
                break;

            case PlayerState.LedgeGrabbing:
                if (!ledgeGrab.IsActive)
                    SetState(PlayerState.Falling);
                break;

            case PlayerState.VerticalWallRunning:
                if (!verticalWallRun.IsActive)
                    SetState(motor.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
                break;
        }
    }

    private void UpdateGroundedState()
    {
        if (wallRun.IsActive)
        {
            SetState(PlayerState.WallRunning);
            return;
        }

        if (!motor.IsGrounded)
        {
            SetState(PlayerState.Falling);
            return;
        }

        if (input.MoveInput.magnitude > 0.1f)
        {
            SetState(input.SprintHeld ? PlayerState.Sprinting : PlayerState.Running);
            return;
        }

        if(verticalWallRun.IsActive)
        {
            SetState(PlayerState.VerticalWallRunning);
            return;
        }

        SetState(PlayerState.Idle);
    }

    private bool TryHandleJump()
    {
        if (!input.JumpPressed) return false;
        if (!motor.TryJump())   return false;

        SetState(PlayerState.Jumping);
        return true;
    }

    private bool TryHandleWallJump()
    {
        if (!wallJump.JumpedFromWall) return false;

        SetState(PlayerState.Jumping);
        return true;
    }

    // -------------------------------------------------------
    // MOVIMIENTO POR ESTADO
    // -------------------------------------------------------

    private void HandleState()
    {
        switch (CurrentState)
        {
            case PlayerState.Idle:
                motor.Stop();
                break;

            case PlayerState.Running:
                motor.Move(moveDirection, data.moveSpeed);
                break;

            case PlayerState.Sprinting:
                motor.Move(moveDirection, data.sprintSpeed, data.sprintAcceleration);
                break;

            case PlayerState.Jumping:
            case PlayerState.Falling:
                if (moveDirection.magnitude > 0.1f)
                    motor.Move(moveDirection, data.moveSpeed * 0.8f);
                else
                    motor.Decelerate();
                break;

            case PlayerState.WallRunning:
                motor.Decelerate();
                break;

            case PlayerState.LedgeGrabbing:
                break;

            case PlayerState.VerticalWallRunning:
                
                break;
        }
    }

    // -------------------------------------------------------
    // UTILIDADES
    // -------------------------------------------------------

    private void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
    }

    public void ForceSetState(PlayerState newState)
    {
        CurrentState = newState;
    }
}
