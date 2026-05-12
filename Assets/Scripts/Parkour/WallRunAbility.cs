using UnityEngine;

/// <summary>
/// Habilidad de wall run: correr sobre paredes laterales.
/// Wall jump fue separado a WallJumpAbility.
/// PlayerStateMachine maneja el ciclo de vida via IMovementAbility.
/// </summary>
public class WallRunAbility : MonoBehaviour, IMovementAbility
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;

    public bool IsActive => IsWallRunning;
    public bool IsWallRunning { get; private set; }

    private bool timerExpired;
    private float wallRunTimer;
    private bool hasLeftGround;
    private Vector3 wallRunDirection;

    public bool CanStart()
    {
        // Resetear el cooldown cuando el jugador se aleja de la pared
        if (timerExpired && !motor.IsTouchingWall)
            timerExpired = false;

        bool isLateralWall = Mathf.Abs(Vector3.Dot(motor.WallNormal, transform.right)) > 0.5f;

        return input.SprintHeld      &&
               motor.IsTouchingWall  &&
               motor.IsGrounded      &&
               isLateralWall         &&
               !timerExpired;
    }

    public void StartAbility()
    {
        IsWallRunning = true;
        wallRunTimer  = data.horizontalWallRunDuration;
        hasLeftGround = false;
        motor.OverrideGravity = true;

        wallRunDirection = CalculateWallRunDirection();

        if (motor.VerticalVelocity < data.wallRunUpBoost)
            motor.SetVerticalVelocity(data.wallRunUpBoost);

        motor.SetHorizontalVelocity(wallRunDirection, motor.Velocity.magnitude);
    }

    public void UpdateAbility()
    {
        wallRunTimer -= Time.deltaTime;

        if (!motor.IsGrounded)
            hasLeftGround = true;

        bool shouldStop = !input.SprintHeld          ||
                          !motor.IsTouchingWall       ||
                          wallRunTimer <= 0f          ||
                          (hasLeftGround && motor.IsGrounded);

        if (shouldStop)
        {
            if (wallRunTimer <= 0f)
                timerExpired = true;

            StopAbility();
            return;
        }

        motor.Move(wallRunDirection, data.wallRunSpeed);
        motor.ApplyReduceGravity(data.wallRunGravity);
    }

    public void StopAbility()
    {
        IsWallRunning = false;
        wallRunTimer  = 0f;
        motor.OverrideGravity = false;
    }

    public void ForceStop()
    {
        timerExpired  = false;
        hasLeftGround = false;
        StopAbility();
    }

    private Vector3 CalculateWallRunDirection()
    {
        Vector3 wallParallel = Vector3.Cross(motor.WallNormal, Vector3.up).normalized;
        return Vector3.Dot(transform.forward, wallParallel) < 0f ? -wallParallel : wallParallel;
    }
}
