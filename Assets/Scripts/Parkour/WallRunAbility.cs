using UnityEngine;


public class WallRunAbility : MonoBehaviour
    {
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsWallRunning {get; private set;}

    public bool JumpedFromWall {get; private set;}
    private bool timerExpired;

    public void ResetJumpFlag()
    {
        JumpedFromWall = false;
    }

    private float wallRunTimer;
    private bool hasLeftGround;
    private Vector3 wallRunDirection;

    void Update()
    {
        if (motor.IsTouchingWall && !motor.IsGrounded && input.JumpPressed)
        {
            WallJump();
            return;
        }

        if (IsWallRunning)
        {
            UpdateWallRun();
        }
        else
        {
            if (JumpedFromWall && !motor.IsTouchingWall)
                ResetJumpFlag();

            if (timerExpired && !motor.IsTouchingWall)
                timerExpired = false;

            TryStartWallRun();
        }
    }

    //Try to start wall run
    void TryStartWallRun()
    {
        //Conditions to start wall run
        bool isLateralWall = Mathf.Abs(Vector3.Dot(motor.WallNormal, transform.right)) > 0.5f;

        if(!input.WallRunHeld||
        !motor.IsTouchingWall||
        !motor.IsGrounded||
        !isLateralWall||
        timerExpired) return;

        StartWallRun();

    }

    //Start wall run

    void StartWallRun()
    {
        IsWallRunning = true;
        wallRunTimer = data.wallRunDuration;
        hasLeftGround = false;
        motor.OverrideGravity = true;

        //Determine wall run direction
        wallRunDirection = CalculateWallRunDirection();

        // Impulso vertical único para crear la parábola (solo si no viene subiendo más rápido ya)
        if (motor.VerticalVelocity < data.wallRunUpBoost)
            motor.SetVerticalVelocity(data.wallRunUpBoost);
    }

    void UpdateWallRun()
    {
        wallRunTimer -= Time.deltaTime;

        if (!motor.IsGrounded) hasLeftGround = true;

        if (!input.WallRunHeld || !motor.IsTouchingWall || wallRunTimer <= 0f || (hasLeftGround && motor.IsGrounded))
        {
            if (wallRunTimer <= 0f) timerExpired = true;
            StopWallRun();
            return;
        }

        wallRunDirection = CalculateWallRunDirection();

        motor.Move(wallRunDirection, data.wallRunSpeed);

        motor.ApplyReduceGravity(data.wallRunGravity);

    }

    void WallJump()
    {
        // Guardamos la normal ANTES de detener el wall run
        Vector3 wallNormal = motor.WallNormal;
        StopWallRun();

        // Calculamos componente paralela a la pared en la velocidad actual
        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 horizontalVelocity = motor.Velocity;
        Vector3 parallelVelocity = horizontalVelocity - Vector3.Dot(horizontalVelocity, wallOut) * wallOut;

        Vector3 horizontalImpulse;
        if (parallelVelocity.magnitude > 1f)
        {
            // Tiene velocidad paralela (wall run o movimiento lateral) — mezcla normal + dirección paralela
            horizontalImpulse = (wallOut * data.wallJumpForce + parallelVelocity.normalized * data.wallJumpSpeed).normalized;
        }
        else
        {
            // Sin velocidad paralela — salta perpendicular a la pared
            horizontalImpulse = wallOut;
        }

        motor.SetHorizontalVelocity(horizontalImpulse, data.wallJumpSpeed);

        // Impulso vertical — arco bajo para sensación de parkour
        float gravity = (float)Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        JumpedFromWall = true;
    }


    //Stop wall run

    void StopWallRun()
    {
        IsWallRunning = false;
        wallRunTimer = 0f;
        motor.OverrideGravity = false;
    }

    //Calculate wall run direction

    Vector3 CalculateWallRunDirection()
    {

        //Cross(wall normal, up) to get a vector parallel to the wall and horizontal
        Vector3 wallParallel = Vector3.Cross(motor.WallNormal,Vector3.up).normalized;


        //Determinate if the wall parallel direction is the same as the player's forward direction,
        //if not, invert it
        float dot = Vector3.Dot(transform.forward, wallParallel);
        if (dot < 0f) wallParallel = -wallParallel;

        return wallParallel;
    }

    





}