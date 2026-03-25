using UnityEngine;


public class WallRunAbility : MonoBehaviour
    {
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsWallRunning {get; private set;}

    public bool JumpedFromWall {get; private set;}

    public void ResetJumpFlag()
    {
        JumpedFromWall = false;
    }

    private float wallRunTimer;
    
    private Vector3 wallRunDirection;

    void Update()
    {
        if (IsWallRunning)
        {
            UpdateWallRun();
        }else
        {
            TryStartWallRun();
        }
    }

    //Try to start wall run
    void TryStartWallRun()
    {
        //Conditions to start wall run
        if(!input.WallRunHeld||
        !motor.IsTouchingWall||
        motor.IsGrounded||
        !HasEnoughtHeight()) return;

        StartWallRun();

    }

    //Start wall run

    void StartWallRun()
    {
        IsWallRunning = true;
        wallRunTimer = data.wallRunDuration;

        //Determine wall run direction
        wallRunDirection = CalculateWallRunDirection();
    }

    void UpdateWallRun()
    {
        wallRunTimer -= Time.deltaTime;

        if (!input.WallRunHeld ||!motor.IsTouchingWall || motor.IsGrounded || wallRunTimer <= 0f)
        {
            StopWallRun();
            return;
        }

        if(input.JumpPressed)
        {
            WallJump();
            return;
        }

        wallRunDirection = CalculateWallRunDirection();

        motor.Move(wallRunDirection, data.wallRunSpeed);

        motor.ApplyReduceGravity(data.wallRunGravity);

        float verticalInput = input.MoveInput.y;
        if(verticalInput > 0.1f)
        {
            motor.SetVerticalVelocity(data.wallRunSpeed * 0.6f);
        }
    }

    void WallJump()
    {
        // Guardamos la normal ANTES de detener el wall run
        Vector3 wallNormal = motor.WallNormal;
        StopWallRun();

        // Impulso horizontal — solo XZ, sin Y
        Vector3 horizontalImpulse = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        motor.SetHorizontalVelocity(horizontalImpulse,data.wallJumpForce);

        // Impulso vertical — misma fórmula que el salto normal
        float gravity = (float)Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.jumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        JumpedFromWall = true;
    }


    //Stop wall run

    void StopWallRun()
    {
        IsWallRunning = false;
        wallRunTimer = 0f;
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

    //Check if the player has enough height to start wall run

    bool HasEnoughtHeight()
    {

        return !Physics.Raycast(transform.position,Vector3.down,data.minWallRunHeight);

    }
    





}