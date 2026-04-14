using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private LedgeGrabAbility ledgeGrab;
    [SerializeField] private WallRunAbility wallRun;

    public PlayerState CurrentState { get; private set; }

    public enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        WallRunning,
        Sprinting,
        LedgeGrabbing
    }

    private Vector3 moveDirection;

    private void Update()
    {
        UpdateMoveDirection();
        UpdateState();
        HandleState();
    }

    private void UpdateMoveDirection()
    {
        Vector3 forward = cameraController.CameraForward * input.MoveInput.y;
        Vector3 right = cameraController.CameraRight * input.MoveInput.x;
        moveDirection = (forward + right).normalized;
    }

    private void UpdateState()
    {
        if (TryHandleJump())
        {
            return;
        }

        switch (CurrentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
            case PlayerState.Sprinting:
                UpdateGroundedState();
                break;

            case PlayerState.Jumping:
                if (ledgeGrab.IsLedgeGrabbing)
                {
                    SetState(PlayerState.LedgeGrabbing);
                }
                else if (motor.HitCeiling)
                {
                    SetState(PlayerState.Falling);
                }
                else if (motor.VerticalVelocity < 0f)
                {
                    SetState(PlayerState.Falling);
                }
                break;

            case PlayerState.Falling:
                if (motor.IsGrounded)
                {
                    UpdateGroundedState();
                }
                else if (ledgeGrab.IsLedgeGrabbing)
                {
                    SetState(PlayerState.LedgeGrabbing);
                }
                break;

            case PlayerState.WallRunning:
                if (wallRun.JumpedFromWall)
                {
                    SetState(PlayerState.Jumping);
                }
                else if (!wallRun.IsWallRunning)
                {
                    SetState(motor.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
                }
                break;

            case PlayerState.LedgeGrabbing:
                if (!ledgeGrab.IsLedgeGrabbing)
                {
                    SetState(PlayerState.Falling);
                }
                break;
        }
    }

    private void UpdateGroundedState()
    {
        if (wallRun.IsWallRunning)
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
        if (!input.JumpPressed)
        {
            return false;
        }

        if (!motor.TryJump())
        {
            return false;
        }

        SetState(PlayerState.Jumping);
        return true;
    }

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
                {
                    motor.Move(moveDirection, data.moveSpeed * 0.8f);
                }
                else
                {
                    motor.Decelerate();
                }
                break;

            case PlayerState.WallRunning:
                motor.Decelerate();
                break;

            case PlayerState.LedgeGrabbing:
                break;
        }
    }

    private void SetState(PlayerState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
    }

    public void ForceSetState(PlayerState newState)
    {
        CurrentState = newState;
    }
}
