using System.Runtime.ExceptionServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{

    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private CameraController cameraController;

    [SerializeField] private WallRunAbility wallRun;

    public PlayerState CurrentState {get; private set;}

    public enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        WallRunning
    }

    private Vector3 moveDirection;

    void Update()
    {
        UpdateMoveDirection();
        UpdateState();
        HandleState();
    }



    //Movement Direction

    void UpdateMoveDirection()
    {
        Vector3 forward = cameraController.CameraForward * input.MoveInput.y;
        Vector3 right = cameraController.CameraRight * input.MoveInput.x;

        moveDirection = (forward + right).normalized;
    }


    void UpdateState ()
    {
        switch (CurrentState)
        {
            case PlayerState.Idle:
            case PlayerState.Running:
                if (wallRun.IsWallRunning)
                {
                    SetState(PlayerState.WallRunning);
                }
                else if (!motor.IsGrounded)
                {
                    SetState(PlayerState.Falling);
                }
                else if (input.MoveInput.magnitude > 0.1f)
                {
                    SetState(PlayerState.Running);
                }
                else
                {
                    SetState(PlayerState.Idle);
                }
                break;
            case PlayerState.Jumping:
                if(motor.Velocity.y < 0f)
                    SetState(PlayerState.Falling);
                break;
            case PlayerState.Falling:
                if (motor.IsGrounded)
                    SetState(PlayerState.Idle);
                break;

            case PlayerState.WallRunning:
                // Salimos del wall run si: toca el suelo, se aleja de la pared, o salta
                if (motor.IsGrounded)
                    SetState(PlayerState.Idle);
                else if (!motor.IsTouchingWall)
                    SetState(PlayerState.Falling);
                    else if(wallRun.JumpedFromWall)
                    {
                    SetState(PlayerState.Jumping);
                }
                break;
        }
        if (input.JumpPressed)
        {
            Debug.Log($"JumpPressed — IsGrounded: {motor.IsGrounded} — CoyoteAvailable: {motor.CoyoteAvailable} — State: {CurrentState}");
            bool jumped = motor.TryJump();
            Debug.Log($"TryJump resultado: {jumped}");
            if(jumped)
            {
                SetState(PlayerState.Jumping);
            }
        }
    }

    //State Logic

    void HandleState()
    {
        switch(CurrentState)
        {
            case PlayerState.Idle:
                motor.Decelerate();
                break;
            case PlayerState.Running:
                motor.Move(moveDirection, data.moveSpeed);
                break;
            case PlayerState.Jumping:

                if (moveDirection.magnitude > 0.1f)
                    motor.Move(moveDirection, data.moveSpeed * 0.8f);
                else
                    motor.Decelerate();
                break;

            case PlayerState.Falling:
                // Mismo control que en el aire
                if (moveDirection.magnitude > 0.1f)
                    motor.Move(moveDirection, data.moveSpeed * 0.8f);
                else
                    motor.Decelerate();
                break;

            case PlayerState.WallRunning:
                // Por ahora solo frena — WallRunAbility toma el control después
                motor.Decelerate();
                break;
        }
    }


    //State Set

    void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
    }
}
