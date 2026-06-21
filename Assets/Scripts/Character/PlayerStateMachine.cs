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

    [Header("Efectos visuales")]
    [SerializeField] private NeonFlasher neonFlasher;

    [Header("Animación")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private PlayerSFX sfx;

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

    private void OnEnable()
    {
        cameraController = CameraController.Instance;
    }

    private void Update()
    {
        UpdateMoveDirection();
        UpdateAbilities();
        UpdateState();
        HandleState();
        HandleRotation();
    }

    // -------------------------------------------------------
    // ABILITIES
    // -------------------------------------------------------

    private void UpdateAbilities()
    {
        // Durante LedgeGrabbing y VerticalWallRunning no se inician abilities nuevas
        bool canStartNew = CurrentState != PlayerState.LedgeGrabbing
                        && CurrentState != PlayerState.VerticalWallRunning;

        foreach (IMovementAbility ability in abilities)
        {
            if (ability.IsActive)
                ability.UpdateAbility();
            else if (canStartNew && ability.CanStart())
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
                {
                    sfx?.PlayLand();
                    UpdateGroundedState();
                }
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
        if (verticalWallRun.IsActive)
        {
            SetState(PlayerState.VerticalWallRunning);
            return;
        }

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

        SetState(PlayerState.Idle);
    }

    private bool TryHandleJump()
    {
        if (!input.JumpPressed && !motor.JumpBuffered) return false;
        if (!motor.TryJump()) return false;

        neonFlasher?.Flash(motor.GroundCollider);
        sfx?.PlayJump();
        SetState(PlayerState.Jumping);
        return true;
    }

    private bool TryHandleWallJump()
    {
        if (!wallJump.JumpedFromWall && !verticalWallRun.JumpedFromVerticalWall) return false;

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
                    // Control aéreo reducido — acelera más lento para no cortar el momentum
                    motor.Move(moveDirection, data.airControlSpeed, data.airControlAcceleration);
                else
                    // Sin input: desacelera suave para preservar la dirección del salto
                    motor.Move(motor.Velocity.normalized, motor.Velocity.magnitude, data.airDeceleration);
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
    // ROTACIÓN
    // -------------------------------------------------------

    private void HandleRotation()
    {
        Vector3 targetDirection = GetRotationTarget();
        if (targetDirection.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            data.rotationSpeed * Time.deltaTime
        );
    }

    private Vector3 GetRotationTarget()
    {
        switch (CurrentState)
        {
            case PlayerState.Running:
            case PlayerState.Sprinting:
                // Rotar hacia la dirección de input
                return moveDirection;

            case PlayerState.Jumping:
            case PlayerState.Falling:
                // Usar la velocidad real — captura la dirección del wall jump automáticamente
                Vector3 horizontalVel = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
                if (horizontalVel.magnitude > 0.5f) return horizontalVel.normalized;
                return moveDirection;

            case PlayerState.WallRunning:
                // Rotar hacia la dirección de desplazamiento lateral en la pared
                Vector3 wallRunVel = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
                if (wallRunVel.magnitude > 0.1f) return wallRunVel.normalized;
                return Vector3.zero;

            // VerticalWallRunning: el jugador ya enfrenta la pared para activarla, no rotar
            // LedgeGrabbing: no rotar mientras está colgado o subiendo
            // Idle: no hay dirección relevante
            default:
                return Vector3.zero;
        }
    }

    // -------------------------------------------------------
    // UTILIDADES
    // -------------------------------------------------------

    private void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        bool isJumping  = CurrentState == PlayerState.Jumping || CurrentState == PlayerState.Falling;
        bool isGrounded = CurrentState == PlayerState.Idle    || CurrentState == PlayerState.Running
                       || CurrentState == PlayerState.Sprinting;
        animator.SetBool("IsJumping",  isJumping);
        animator.SetBool("IsGrounded", isGrounded);
    }

    public void ForceSetState(PlayerState newState)
    {
        CurrentState = newState;
        UpdateAnimator();
    }
}
