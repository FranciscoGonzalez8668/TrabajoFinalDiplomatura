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

    public bool IsFrontWall {get; private set;}

    public Vector3 FrontWallNormal {get;private set;}
    public Vector3 Velocity => velocity;
    public float VerticalVelocity => verticalVelocity;
    public bool OverrideGravity { get; set; }
    public bool HitCeiling { get; private set; }
    public float GravityScale => data.gravityScale;
    public float HalfHeight => cc.height * 0.5f;
    public float FeetY => transform.position.y + cc.center.y - cc.height * 0.5f;


    //Private State 

    private CharacterController cc;
    private Vector3 velocity;
    private float verticalVelocity;

    // Plataformas móviles
    private Transform currentSurface;
    private float platformLostTimer;
    private const float PlatformGraceTime = 0.12f;

    /// <summary>
    /// Cuando está en true, el timer de gracia no libera el parenting.
    /// Usado por LedgeGrabAbility mientras el jugador está colgado.
    /// </summary>
    public bool SurfaceLocked { get; set; }

    public void AttachToSurface(Transform surface)
    {
        currentSurface = surface;
        platformLostTimer = 0f;
        transform.SetParent(surface);
    }

    public void DetachFromSurface()
    {
        SurfaceLocked = false;
        currentSurface = null;
        platformLostTimer = 0f;
        transform.SetParent(null);
    }

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

        // Timer de gracia antes de soltarse de la plataforma
        // Evita que un flickering puntual de isGrounded deshaga el parenting
        if (IsGrounded)
        {
            platformLostTimer = 0f;
        }
        else if (currentSurface != null && !SurfaceLocked)
        {
            platformLostTimer += Time.deltaTime;
            if (platformLostTimer >= PlatformGraceTime)
            {
                currentSurface = null;
                transform.SetParent(null);
            }
        }
    }

    //Wall Check

    void CheckWall()
    {

        CheckLateralWall();
        CheckFrontWall();
    }

    void CheckLateralWall(){
        Vector3 diagRight = (transform.forward + transform.right).normalized;
        Vector3 diagLeft  = (transform.forward - transform.right).normalized;

        // Prioridad: laterales → diagonales → frontal
        RaycastHit hit;
        if      (Physics.Raycast(transform.position,  transform.right,   out hit, data.wallDetectionDistance) ||
                 Physics.Raycast(transform.position, -transform.right,   out hit, data.wallDetectionDistance) ||
                 Physics.Raycast(transform.position,  diagRight,         out hit, data.wallDetectionDistance) ||
                 Physics.Raycast(transform.position,  diagLeft,          out hit, data.wallDetectionDistance) ||
                 Physics.Raycast(transform.position,  transform.forward, out hit, data.wallDetectionDistance))
        {
            IsTouchingWall = true;
            WallNormal = hit.normal;
        }
        else
        {
            IsTouchingWall = false;
            WallNormal = Vector3.zero;
        }
    }
    
    void CheckFrontWall()
    {
        // Fan de 3 rays: centro + leve apertura izquierda/derecha
        // Evita falsos negativos cuando el personaje está levemente desalineado con la pared
        Vector3 slightLeft  = Quaternion.AngleAxis(-15f, Vector3.up) * transform.forward;
        Vector3 slightRight = Quaternion.AngleAxis( 15f, Vector3.up) * transform.forward;

        RaycastHit hit;
        bool detected =
            Physics.Raycast(transform.position, transform.forward, out hit, data.wallDetectionDistance) ||
            Physics.Raycast(transform.position, slightLeft,        out hit, data.wallDetectionDistance) ||
            Physics.Raycast(transform.position, slightRight,       out hit, data.wallDetectionDistance);

        // Debug: verde = detectó, rojo = no detectó
        Debug.DrawRay(transform.position, transform.forward  * data.wallDetectionDistance, detected ? Color.green : Color.red);
        Debug.DrawRay(transform.position, slightLeft         * data.wallDetectionDistance, Color.yellow);
        Debug.DrawRay(transform.position, slightRight        * data.wallDetectionDistance, Color.yellow);

        if (detected)
        {
            float alignment = Vector3.Dot(transform.forward, -hit.normal);
            if(alignment > data.frontWallThreshold)
            {
                IsFrontWall = true;
                FrontWallNormal = hit.normal;
                return;
            }
        }
        IsFrontWall = false;
        FrontWallNormal = Vector3.zero;
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
    public void Move(Vector3 direction, float speed, float customAcceleration)
    {
        Vector3 targetVelocity = direction * speed;
        velocity = Vector3.MoveTowards(
            velocity,
            targetVelocity,
            customAcceleration * Time.deltaTime
        );
    }

    public void MoveRaw(Vector3 motion)
    {
        cc.Move(motion);
    }

    public void MoveVerticalTo(float targetY)
    {
        float deltaY = targetY - transform.position.y;
        if (Mathf.Abs(deltaY) <= 0.001f)
        {
            return;
        }

        cc.Move(Vector3.up * deltaY);
    }

    // Moves player upward until feet reach targetFeetY. Returns true when reached.
    public bool ClimbVertical(float targetFeetY, float speed)
    {
        float remaining = targetFeetY - FeetY;
        if (remaining <= 0.05f) return true;
        cc.Move(Vector3.up * Mathf.Min(speed * Time.deltaTime, remaining));
        return false;
    }


    public void Decelerate()
    {
        velocity = Vector3.MoveTowards(
            velocity,
            Vector3.zero,
            data.deceleration * Time.deltaTime
        );
    }

    public void Stop()
    {
        velocity = Vector3.zero;
    }

    public void ResetMotion()
    {
        velocity = Vector3.zero;
        verticalVelocity = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        OverrideGravity = false;
        HitCeiling = false;
        IsFrontWall = false;
        FrontWallNormal = Vector3.zero;
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
        verticalVelocity = Mathf.Max(verticalVelocity, -data.maxFallSpeed);
    }

    //SetHorizontal Velocity (Used in WallRun)
    public void SetHorizontalVelocity(Vector3 horizontalVelocity, float speed)
    {
        velocity = horizontalVelocity * speed;
    }

    public void TeleportTo(Vector3 worldPosition)
    {
        ResetMotion();
        cc.enabled = false;
        transform.position = worldPosition;
        cc.enabled = true;
        CheckGround();
        CheckWall();
    }


    //Apply on Character Controller

    void ApplyMovement()
    {
        HitCeiling = false;

        if (!OverrideGravity)
            ApplyGravity();

        Vector3 finalMove = velocity + Vector3.up * verticalVelocity;
        CollisionFlags collisionFlags = cc.Move(finalMove * Time.deltaTime);

        if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            HitCeiling = true;
            verticalVelocity = 0f;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Solo superficies que miran hacia arriba
        if (hit.normal.y < 0.7f) return;

        ObjectMover mover = hit.gameObject.GetComponent<ObjectMover>();
        if (mover == null) return;

        // Notificar contacto — usado por plataformas Vanishing
        mover.NotifyPlayerContact();

        // Parenting para plataformas móviles (Linear / Pendulum)
        if (currentSurface == hit.transform) return;
        currentSurface = hit.transform;
        platformLostTimer = 0f;
        transform.SetParent(hit.transform);
    }
}
