using UnityEngine;

public class WallRunAbility : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;

    public bool IsWallRunning { get; private set; }
    public bool JumpedFromWall { get; private set; }

    private bool timerExpired;
    private float wallRunTimer;
    private bool hasLeftGround;
    private Vector3 wallRunDirection;

    private void Update()
    {
        if (motor.IsTouchingWall && !motor.IsGrounded && input.JumpPressed)
        {
            WallJump();
            return;
        }

        if (IsWallRunning)
        {
            UpdateWallRun();
            return;
        }

        if (JumpedFromWall && !motor.IsTouchingWall)
        {
            ResetJumpFlag();
        }

        if (timerExpired && !motor.IsTouchingWall)
        {
            timerExpired = false;
        }

        TryStartWallRun();
    }

    public void ResetJumpFlag()
    {
        JumpedFromWall = false;
    }

    private void TryStartWallRun()
    {
        bool isLateralWall = Mathf.Abs(Vector3.Dot(motor.WallNormal, transform.right)) > 0.5f;

        if (!input.SprintHeld ||
            !motor.IsTouchingWall ||
            !motor.IsGrounded ||
            !isLateralWall ||
            timerExpired)
        {
            return;
        }

        StartWallRun();
    }

    private void StartWallRun()
    {
        IsWallRunning = true;
        wallRunTimer = data.wallRunDuration;
        hasLeftGround = false;
        motor.OverrideGravity = true;

        wallRunDirection = CalculateWallRunDirection();

        if (motor.VerticalVelocity < data.wallRunUpBoost)
        {
            motor.SetVerticalVelocity(data.wallRunUpBoost);
        }

        motor.SetHorizontalVelocity(wallRunDirection, motor.Velocity.magnitude);
    }

    private void UpdateWallRun()
    {
        wallRunTimer -= Time.deltaTime;

        if (!motor.IsGrounded)
        {
            hasLeftGround = true;
        }

        if (!input.SprintHeld || !motor.IsTouchingWall || wallRunTimer <= 0f || (hasLeftGround && motor.IsGrounded))
        {
            if (wallRunTimer <= 0f)
            {
                timerExpired = true;
            }

            StopWallRun();
            return;
        }

        motor.Move(wallRunDirection, data.wallRunSpeed);
        motor.ApplyReduceGravity(data.wallRunGravity);
    }

    private void WallJump()
    {
        Vector3 wallNormal = motor.WallNormal;
        StopWallRun();

        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 horizontalVelocity = motor.Velocity;
        Vector3 parallelVelocity = horizontalVelocity - Vector3.Dot(horizontalVelocity, wallOut) * wallOut;

        Vector3 horizontalImpulse;
        if (parallelVelocity.magnitude > 1f)
        {
            horizontalImpulse = (wallOut * data.wallJumpForce + parallelVelocity.normalized * data.wallJumpSpeed).normalized;
        }
        else
        {
            horizontalImpulse = wallOut;
        }

        motor.SetHorizontalVelocity(horizontalImpulse, data.wallJumpSpeed);

        float gravity = Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        JumpedFromWall = true;
    }

    private void StopWallRun()
    {
        IsWallRunning = false;
        wallRunTimer = 0f;
        motor.OverrideGravity = false;
    }

    private Vector3 CalculateWallRunDirection()
    {
        Vector3 wallParallel = Vector3.Cross(motor.WallNormal, Vector3.up).normalized;
        float dot = Vector3.Dot(transform.forward, wallParallel);

        if (dot < 0f)
        {
            wallParallel = -wallParallel;
        }

        return wallParallel;
    }

    public void ForceStop()
    {
        JumpedFromWall = false;
        timerExpired = false;
        hasLeftGround = false;
        StopWallRun();
    }
}
