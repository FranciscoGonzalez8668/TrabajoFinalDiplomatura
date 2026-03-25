using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private PlayerData data;
    [SerializeField] private InputHandler input;


    //Public State (Read Only)
    public bool IsGrounded {get; private set;}
    public bool IsTouchingWall {get; private set;}
    public Vector3 WallNormal {get; private set;}
    public Vector3  Velocity =>velocity;
    public float GravityScale => data.gravityScale;


    //Private State

    private CharacterController cc;
    private Vector3 velocity;
    private float verticalVelocity;

    //Coyote Time
    private float coyoteTimer;
    public bool CoyoteAvailable => coyoteTimer > 0f;

    //Jump Buffer
    private float jumpBufferTimer;
    private bool jumpBuffered => jumpBufferTimer >0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckGround();
        CheckWall();
        HandleCoyoteTime();
        HandleJumpBuffer();
        ApplyMovement();
    }

    //Ground Check
    private void CheckGround()
    {
        IsGrounded = cc.isGrounded;

        if(IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; //Small negative to keep grounded
        }

    }

    //Wall Check

    void CheckWall()
    {
        bool hitRight = Physics.Raycast(
            transform.position,
            transform.right,
            out RaycastHit hitInforRight, data.wallDetectionDistance
        );

        bool hitLeft = Physics.Raycast(
            transform.position,
            -transform.right,
            out RaycastHit hitInforLeft, data.wallDetectionDistance
        );

        if(hitRight)
        {
            IsTouchingWall = true;
            WallNormal = hitInforRight.normal;
        }
        else if(hitLeft)
        {
            IsTouchingWall = true;
            WallNormal = hitInforLeft.normal;
        }
        else
        {
            IsTouchingWall = false;
            WallNormal = Vector3.zero;
        }

    }


    //Coyote Time 

    void HandleCoyoteTime()
    {
        if(IsGrounded)
        {
            coyoteTimer = data.coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    //Jump Buffer

    void HandleJumpBuffer()
    {
        if (input.JumpPressed)
        {
            jumpBufferTimer = data.jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }


    //Apply Movement

    public void Move(Vector3 direction, float speed)
    {
        
        Vector3 targetVelocity = direction * speed;

        velocity = Vector3.MoveTowards(
            velocity,
            targetVelocity,
            data.acceleration * Time.deltaTime
        );
    }

    public void Decelerate()
    {
        velocity = Vector3.MoveTowards(
            velocity,
            Vector3.zero,
            data.deceleration * Time.deltaTime
        );
    }

    public bool TryJump()
    {
        if ((IsGrounded || CoyoteAvailable) && jumpBuffered)
        {
            

            float gravity = (float)Physics.gravity.y;
            float jumpVelocity = 2f * Mathf.Abs(gravity) * data.gravityScale * data.jumpHeight;
            verticalVelocity = Mathf.Sqrt(jumpVelocity);
            jumpBufferTimer = 0f; //Consume the jump buffer
            coyoteTimer = 0f; //Consume coyote time
            return true;
        }

        return false;
    }

    //Gravity Application

    void ApplyGravity()
    {

        if(IsGrounded) return;

        float gravityBase = Physics.gravity.y * data.gravityScale;

        if(verticalVelocity < 0f)
        {
            verticalVelocity += gravityBase * data.fallMultiplier * Time.deltaTime;
        }
        else
        {
            verticalVelocity += gravityBase * Time.deltaTime;
        }

        verticalVelocity = Mathf.Max(verticalVelocity, -data.maxFallSpeed);
    }

    // WallRun Gravity Override

    public void SetVerticalVelocity(float value)
    {
        verticalVelocity = value;
    }

    public void ApplyReduceGravity(float multiplier)
    {
        float gravityBase = Physics.gravity.y * data.gravityScale;
        verticalVelocity += gravityBase * multiplier * Time.deltaTime;
        verticalVelocity = Math.Max(verticalVelocity, -data.maxFallSpeed);
    }

    //SetHorizontal Velocity (Used in WallRun)
    public void SetHorizontalVelocity(Vector3 horizontalVelocity, float speed)
    {
        velocity = horizontalVelocity * speed;
    }


    //Apply on Character Controller

    void ApplyMovement()
    {
        ApplyGravity();
        Vector3 finalMove = velocity + Vector3.up * verticalVelocity;
        cc.Move(finalMove * Time.deltaTime);
    }

}