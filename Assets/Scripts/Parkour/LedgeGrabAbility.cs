using UnityEngine;

public class LedgeGrabAbility : MonoBehaviour
{
    
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;    
    [SerializeField] private PlayerStateMachine stateMachine;


    public bool IsLedgeGrabbing {get; private set;}

    private float hangTimer;
    private float grabCooldown;
    private Vector3 hangPosition;
    private Vector3 ledgeTopPoint;
    private float grabHeightOffset;

    private Vector3 wallNormal;

    private bool isClimbing;
    private bool climbVerticalDone;
    private Vector3 climbTargetPosition;
    private float climbTimer;

    void Update()
    {
        grabCooldown -= Time.deltaTime;

        if (isClimbing)
        {
            UpdateClimb();
            return;
        }
        if(IsLedgeGrabbing)
            UpdateLedgeGrab();
        else
            TryStartLedgeGrab();

    }

    void TryStartLedgeGrab()
    {
        if (grabCooldown > 0f) return;

        if(stateMachine.CurrentState != PlayerStateMachine.PlayerState.Falling) return;

        if (!TryFindLedgeAtPosition(transform.position, transform.forward, out RaycastHit wallHit, out RaycastHit ledgeHit))
        {
            return;
        }

        float hightDiff = ledgeHit.point.y - transform.position.y;

        if(hightDiff < 0f || hightDiff > data.ledgeGrabReach) return;

        StartLedgeGrab(ledgeHit.point, wallHit.normal);
    }

    void StartLedgeGrab(Vector3 ledgeTopPoint, Vector3 normal)
    {
        IsLedgeGrabbing = true;
        wallNormal = normal;
        grabHeightOffset = ledgeTopPoint.y - transform.position.y;
        hangTimer = data.ledgeHangTimeout;
        hangPosition = new Vector3(transform.position.x, ledgeTopPoint.y -0.1f, transform.position.z);
        this.ledgeTopPoint = ledgeTopPoint;
        motor.OverrideGravity = true;
        motor.Stop();
        motor.SetVerticalVelocity(0f);

    }

    void UpdateLedgeGrab()
    {
        motor.MoveVerticalTo(hangPosition.y);
        motor.SetVerticalVelocity(0f);

        Vector3 wallCheckDirection = -new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 ledgeDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 moveAlongLedge = ledgeDirection * input.MoveInput.x;

        if(moveAlongLedge.magnitude > 0.1f)
        {
            float probeStep = Mathf.Max(0.05f, data.moveSpeed * Time.deltaTime);
            Vector3 lateralProbe = ledgeDirection * Mathf.Sign(input.MoveInput.x) * probeStep;
            Vector3 candidatePosition = new Vector3(
                transform.position.x + lateralProbe.x,
                ledgeTopPoint.y - grabHeightOffset,
                transform.position.z + lateralProbe.z);

            if (!TryFindLedgeAtPosition(candidatePosition, wallCheckDirection, out RaycastHit wallHit, out RaycastHit ledgeHit))
            {
                StopLedgeGrab();
                return;
            }

            wallNormal = wallHit.normal;
            ledgeTopPoint = ledgeHit.point;
            hangPosition = new Vector3(hangPosition.x, ledgeHit.point.y - 0.1f, hangPosition.z);
            motor.Move(moveAlongLedge, data.moveSpeed);
        }
        else
        {
            motor.Stop();
        }

        if (input.ClimbPressed )
        {
            StartClimb();
            return;
        }
        if (input.JumpPressed)
        {
            LedgeJump();
            return;
        }

        hangTimer -= Time.deltaTime;
        if(hangTimer <= 0f)
            StopLedgeGrab();    
    }

    bool TryFindLedgeAtPosition(Vector3 characterPosition, Vector3 wallCheckDirection, out RaycastHit wallHit, out RaycastHit ledgeHit)
    {
        Vector3 chestPosition = characterPosition + Vector3.up * data.ledgeCheckHeightOffset;
        Vector3 wallDirection = wallCheckDirection.normalized;

        Debug.DrawRay(chestPosition, wallDirection * data.ledgeDetectionDistance, Color.red);
        if (!Physics.Raycast(chestPosition, wallDirection, out wallHit, data.ledgeDetectionDistance))
        {
            ledgeHit = default;
            return false;
        }

        Vector3 ray2Origin = new Vector3(
            wallHit.point.x,
            characterPosition.y + data.ledgeGrabReach,
            wallHit.point.z);

        Debug.DrawRay(ray2Origin, Vector3.down * data.ledgeGrabReach, Color.green);
        return Physics.SphereCast(ray2Origin, 0.1f, Vector3.down, out ledgeHit, data.ledgeCheckHeightOffset);
    }

    void StartClimb()
    {
        isClimbing = true;
        climbVerticalDone = false;
        climbTimer = 0f;

        // Fase 2: target horizontal (dentro de la plataforma, misma X/Z del jugador menos la normal del muro)
        climbTargetPosition = transform.position - wallNormal * 0.6f;
    }

    void UpdateClimb()
    {
        climbTimer += Time.deltaTime;

        // Fase 1: el motor sube hasta que los pies queden sobre el borde
        if (!climbVerticalDone)
        {
            climbVerticalDone = motor.ClimbVertical(ledgeTopPoint.y + 0.1f, data.ledgeClimbSpeed);
            return;
        }

        // Fase 2: avanzar horizontalmente dentro de la plataforma
        Vector3 toTarget = new Vector3(
            climbTargetPosition.x - transform.position.x,
            0f,
            climbTargetPosition.z - transform.position.z
        );

        if (toTarget.magnitude > 0.1f)
        {
            motor.MoveRaw(toTarget.normalized * data.ledgeClimbSpeed * Time.deltaTime);
        }
        else
        {
            FinishClimb();
            return;
        }

        if (climbTimer > data.ledgeClimbDuration * 2f)
            FinishClimb();
    }

    void FinishClimb()
    {
        isClimbing = false;
        StopLedgeGrab();
        motor.Stop();
        motor.SetVerticalVelocity(0f);
    }

    void LedgeJump()
    {
        StopLedgeGrab();

        float gravity = Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

        if (Mathf.Abs(input.MoveInput.x) > 0.1f)
        {
            Vector3 lateralDir = Vector3.Cross(wallNormal, Vector3.up).normalized;
            motor.SetHorizontalVelocity(lateralDir * Mathf.Sign(input.MoveInput.x), data.wallJumpSpeed);
        }
        else
        {
            motor.SetHorizontalVelocity(wallOut, data.wallJumpSpeed);
        }
    }

    void StopLedgeGrab()
    {
        IsLedgeGrabbing = false;
        isClimbing = false;
        climbVerticalDone = false;
        climbTimer = 0f;
        motor.OverrideGravity = false;
        motor.Stop();
        grabCooldown = 0.5f;
    }

    public void ForceStop()
    {
        grabCooldown = 0f;
        StopLedgeGrab();
    }

}
