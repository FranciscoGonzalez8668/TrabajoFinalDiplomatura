using UnityEngine;

/// <summary>
/// Habilidad de ledge grab: agarrarse de bordes durante la caída.
/// Fases: detección → hang → shimmy lateral → climb → finish.
/// PlayerStateMachine maneja el ciclo de vida via IMovementAbility.
/// </summary>
public class LedgeGrabAbility : MonoBehaviour, IMovementAbility
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsActive => IsLedgeGrabbing;
    public bool IsLedgeGrabbing { get; private set; }

    private float grabCooldownEndTime;
    private float hangStartTime;
    private Vector3 hangPosition;
    private Vector3 ledgeTopPoint;
    private float grabHeightOffset;
    private Vector3 wallNormal;

    private bool isClimbing;
    private bool climbVerticalDone;
    private Vector3 climbTargetPosition;
    private float climbTimer;

    public bool CanStart()
    {
        if (Time.time < grabCooldownEndTime) return false;
        if (stateMachine.CurrentState != PlayerStateMachine.PlayerState.Falling) return false;
        if (!TryFindLedgeAtPosition(transform.position, transform.forward, out _, out RaycastHit ledgeHit)) return false;

        float heightDiff = ledgeHit.point.y - transform.position.y;
        return heightDiff >= 0f && heightDiff <= data.ledgeGrabReach;
    }

    public void StartAbility()
    {
        TryFindLedgeAtPosition(transform.position, transform.forward, out RaycastHit wallHit, out RaycastHit ledgeHit);
        StartLedgeGrab(ledgeHit.point, wallHit.normal);
    }

    public void UpdateAbility()
    {
        if (isClimbing)
        {
            UpdateClimb();
            return;
        }

        UpdateLedgeGrab();
    }

    public void StopAbility() => StopLedgeGrab();

    public void ForceStop()
    {
        grabCooldownEndTime = 0f;
        StopLedgeGrab();
    }

    // -------------------------------------------------------
    // LEDGE GRAB
    // -------------------------------------------------------

    private void StartLedgeGrab(Vector3 topPoint, Vector3 normal)
    {
        IsLedgeGrabbing  = true;
        wallNormal       = normal;
        grabHeightOffset = topPoint.y - transform.position.y;
        hangPosition     = new Vector3(transform.position.x, topPoint.y - 0.1f, transform.position.z);
        ledgeTopPoint    = topPoint;
        hangStartTime    = Time.time;

        motor.OverrideGravity = true;
        motor.Stop();
        motor.SetVerticalVelocity(0f);
    }

    private void UpdateLedgeGrab()
    {
        motor.MoveVerticalTo(hangPosition.y);
        motor.SetVerticalVelocity(0f);

        // Shimmy lateral: moverse a lo largo del borde
        Vector3 ledgeDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 moveAlongLedge = ledgeDirection * input.MoveInput.x;

        if (moveAlongLedge.magnitude > 0.1f)
        {
            float probeStep         = Mathf.Max(0.05f, data.moveSpeed * Time.deltaTime);
            Vector3 lateralProbe    = ledgeDirection * Mathf.Sign(input.MoveInput.x) * probeStep;
            Vector3 candidatePos    = new Vector3(
                transform.position.x + lateralProbe.x,
                ledgeTopPoint.y - grabHeightOffset,
                transform.position.z + lateralProbe.z);

            Vector3 wallCheckDir = -new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

            if (!TryFindLedgeAtPosition(candidatePos, wallCheckDir, out RaycastHit wallHit, out RaycastHit ledgeHit))
            {
                StopLedgeGrab();
                return;
            }

            wallNormal    = wallHit.normal;
            ledgeTopPoint = ledgeHit.point;
            hangPosition  = new Vector3(hangPosition.x, ledgeHit.point.y - 0.1f, hangPosition.z);
            motor.Move(moveAlongLedge, data.moveSpeed);
        }
        else
        {
            motor.Stop();
        }

        if (input.ClimbPressed)
        {
            StartClimb();
            return;
        }

        if (input.JumpPressed)
        {
            LedgeJump();
            return;
        }

        // Timeout: soltar automáticamente si se cuelga demasiado tiempo
        if (Time.time >= hangStartTime + data.ledgeHangTimeout)
            StopLedgeGrab();
    }

    private void StopLedgeGrab()
    {
        IsLedgeGrabbing     = false;
        isClimbing          = false;
        climbVerticalDone   = false;
        climbTimer          = 0f;
        motor.OverrideGravity = false;
        motor.Stop();
        grabCooldownEndTime = Time.time + 0.5f;
    }

    // -------------------------------------------------------
    // CLIMB
    // -------------------------------------------------------

    private void StartClimb()
    {
        isClimbing          = true;
        climbVerticalDone   = false;
        climbTimer          = 0f;
        climbTargetPosition = transform.position - wallNormal * 0.6f;
    }

    private void UpdateClimb()
    {
        climbTimer += Time.deltaTime;

        // Fase 1: subir hasta que los pies superen el borde
        if (!climbVerticalDone)
        {
            climbVerticalDone = motor.ClimbVertical(ledgeTopPoint.y + 0.1f, data.ledgeClimbSpeed);
            return;
        }

        // Fase 2: avanzar horizontalmente dentro de la plataforma
        Vector3 toTarget = new Vector3(
            climbTargetPosition.x - transform.position.x,
            0f,
            climbTargetPosition.z - transform.position.z);

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

    private void FinishClimb()
    {
        isClimbing = false;
        StopLedgeGrab();
        motor.Stop();
        motor.SetVerticalVelocity(0f);
    }

    // -------------------------------------------------------
    // LEDGE JUMP
    // -------------------------------------------------------

    private void LedgeJump()
    {
        StopLedgeGrab();

        float gravity       = Physics.gravity.y;
        float jumpVelocity  = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
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

    // -------------------------------------------------------
    // DETECCIÓN DE BORDE
    // -------------------------------------------------------

    private bool TryFindLedgeAtPosition(Vector3 characterPosition, Vector3 wallCheckDirection,
                                         out RaycastHit wallHit, out RaycastHit ledgeHit)
    {
        Vector3 chestPosition = characterPosition + Vector3.up * data.ledgeCheckHeightOffset;
        Vector3 wallDir       = wallCheckDirection.normalized;

        Debug.DrawRay(chestPosition, wallDir * data.ledgeDetectionDistance, Color.red);

        if (!Physics.Raycast(chestPosition, wallDir, out wallHit, data.ledgeDetectionDistance))
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
}
