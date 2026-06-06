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
    [SerializeField] private CameraController cameraController;

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
    private Vector3 localClimbTargetPosition;
    private float climbTimer;

    // Moving platform support
    private Transform attachedPlatform;
    private Vector3 localHangPosition;
    private Vector3 localLedgeTopPoint;
    private Vector3 localWallNormal;

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
        if (!TryFindLedgeAtPosition(transform.position, transform.forward, out RaycastHit wallHit, out RaycastHit ledgeHit))
            return;

        StartLedgeGrab(ledgeHit.point, wallHit.normal);

        // Si el borde pertenece a un ObjectMover, adjuntarse para moverse con él
        ObjectMover mover = wallHit.collider != null
            ? wallHit.collider.GetComponentInParent<ObjectMover>()
            : null;

        if (mover != null)
        {
            attachedPlatform = mover.transform;
            motor.AttachToSurface(attachedPlatform);
            motor.SurfaceLocked = true;
            localHangPosition  = attachedPlatform.InverseTransformPoint(hangPosition);
            localLedgeTopPoint = attachedPlatform.InverseTransformPoint(ledgeTopPoint);
            localWallNormal    = attachedPlatform.InverseTransformDirection(wallNormal);
        }
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
        motor.DetachFromSurface();
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
        // Si estamos colgados de una plataforma móvil, actualizar posiciones world-space
        if (attachedPlatform != null)
        {
            hangPosition  = attachedPlatform.TransformPoint(localHangPosition);
            ledgeTopPoint = attachedPlatform.TransformPoint(localLedgeTopPoint);
            wallNormal    = attachedPlatform.TransformDirection(localWallNormal);
        }

        motor.MoveVerticalTo(hangPosition.y);
        motor.SetVerticalVelocity(0f);

        // Shimmy: W mueve en la dirección de la cámara proyectada sobre la pared
        // A/D mueven lateralmente respecto a la cámara proyectada sobre la pared
        Vector3 ledgeDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 moveAlongLedge;

        if (cameraController != null)
        {
            Vector3 camFwdOnWall    = Vector3.ProjectOnPlane(cameraController.CameraForward, wallNormal).normalized;
            Vector3 camRightOnWall  = Vector3.ProjectOnPlane(cameraController.CameraRight,   wallNormal).normalized;
            moveAlongLedge = (camFwdOnWall * input.MoveInput.y + camRightOnWall * input.MoveInput.x);
        }
        else
        {
            moveAlongLedge = ledgeDirection * input.MoveInput.x;
        }

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

        if (input.ClimbPressed && HasSpaceToClimb())
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

        // Liberar el lock — si el jugador subió, el sistema de plataformas toma el control normalmente.
        // Si saltó o cayó, DetachFromSurface se llama desde LedgeJump/ForceStop.
        motor.SurfaceLocked = false;
        attachedPlatform = null;
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

        // Si está en plataforma móvil, guardar el target en local space para actualizarlo cada frame
        if (attachedPlatform != null)
            localClimbTargetPosition = attachedPlatform.InverseTransformPoint(climbTargetPosition);
    }

    private void UpdateClimb()
    {
        climbTimer += Time.deltaTime;

        // Actualizar posiciones world-space desde local space si está en plataforma móvil
        if (attachedPlatform != null)
        {
            ledgeTopPoint       = attachedPlatform.TransformPoint(localLedgeTopPoint);
            climbTargetPosition = attachedPlatform.TransformPoint(localClimbTargetPosition);
        }

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
        motor.DetachFromSurface();
        StopLedgeGrab();

        float gravity      = Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        // Siempre salimos alejados de la pared
        // El input lateral (A/D relativo a la cámara) agrega dirección diagonal
        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

        Vector3 lateral = Vector3.zero;
        if (cameraController != null && Mathf.Abs(input.MoveInput.x) > 0.1f)
            lateral = cameraController.CameraRight * input.MoveInput.x;

        Vector3 finalJumpDir = (wallOut + lateral).normalized;
        motor.SetHorizontalVelocity(finalJumpDir, data.wallJumpSpeed);
    }

    // -------------------------------------------------------
    // DETECCIÓN DE BORDE
    // -------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Vector3 chestPos = transform.position + Vector3.up * data.ledgeCheckHeightOffset;

        // Ray 1 — detección de pared (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(chestPos, transform.forward * data.ledgeDetectionDistance);
        Gizmos.DrawSphere(chestPos, 0.04f);

        // Ray 2 — detección de borde hacia abajo (verde)
        // Origen estimado: justo delante del jugador a la altura máxima de agarre
        Vector3 ray2Origin = new Vector3(
            transform.position.x + transform.forward.x * data.ledgeDetectionDistance,
            transform.position.y + data.ledgeGrabReach,
            transform.position.z + transform.forward.z * data.ledgeDetectionDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(ray2Origin, Vector3.down * data.ledgeGrabReach);
        Gizmos.DrawSphere(ray2Origin, 0.04f);

        // Rango de agarre vertical (zona semitransparente)
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Vector3 zoneCenter = transform.position
                           + transform.forward * data.ledgeDetectionDistance
                           + Vector3.up * (data.ledgeGrabReach * 0.5f);
        Gizmos.DrawCube(zoneCenter, new Vector3(0.15f, data.ledgeGrabReach, 0.15f));
    }

    /// <summary>
    /// Verifica que haya suficiente espacio vertical sobre el borde para que el jugador pueda subir.
    /// Evita trepar a superficies muy pequeñas donde el personaje quedaría encajado.
    /// </summary>
    private bool HasSpaceToClimb()
    {
        float requiredHeight = motor.HalfHeight * 2f;
        Vector3 checkOrigin  = ledgeTopPoint + Vector3.up * 0.1f;

        bool blocked = Physics.SphereCast(checkOrigin, 0.2f, Vector3.up, out _, requiredHeight);
        return !blocked;
    }

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
