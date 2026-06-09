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

    [Header("Climb check")]
    [Tooltip("Cuánto se meten los puntos de validación dentro de la plataforma (alejándose de la pared). Aumentar si plataformas válidas no dejan subir.")]
    [SerializeField] private float climbCheckInset  = 0.85f;
    [Tooltip("Radio del círculo de validación al trepar.")]
    [SerializeField] private float climbCheckRadius = 0.2f;

    [Header("Efectos visuales")]
    [SerializeField] private NeonFlasher neonFlasher;

    [Header("Audio")]
    [SerializeField] private PlayerSFX sfx;

    public bool IsActive => IsLedgeGrabbing;
    public bool IsLedgeGrabbing { get; private set; }

    private float grabCooldownEndTime;
    private float hangStartTime;
    private Vector3 hangPosition;
    private Vector3 ledgeTopPoint;
    private float grabHeightOffset;
    private Vector3 wallNormal;

    // Debug gizmos del climb check
    private Vector3[] debugClimbOrigins;
    private bool[]    debugClimbHits;

    private bool isClimbing;
    private bool climbVerticalDone;
    private Vector3 climbTargetPosition;
    private Vector3 localClimbTargetPosition;
    private float climbTimer;

    private Collider grabbedWallCollider;

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

        grabbedWallCollider = wallHit.collider;

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

        sfx?.PlayLedgeGrab();
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

        // Actualizar debug del climb check cada frame para que los gizmos reflejen
        // la posición actual y el estado real de la superficie sobre el borde.
        RefreshClimbCheck();

        motor.MoveVerticalTo(hangPosition.y);
        motor.SetVerticalVelocity(0f);

        // El salto tiene prioridad máxima: chequearlo ANTES del shimmy para evitar que
        // la sonda de shimmy llame StopLedgeGrab+return antes de procesar el jump.
        if (input.JumpPressed)
        {
            LedgeJump();
            return;
        }

        if (input.ClimbPressed && HasSpaceToClimb())
        {
            StartClimb();
            return;
        }

        // Shimmy: igual que correr pero el resultado se proyecta al eje del borde.
        // inputWorld = dirección camera-relative (mismo cálculo que el movimiento en suelo).
        // Dot contra ledgeDirection da cuánto de ese input va a lo largo del borde.
        // Si W apunta hacia la pared: dot ≈ 0, igual que correr contra una pared.
        Vector3 ledgeDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 moveAlongLedge;

        if (cameraController != null)
        {
            Vector3 inputWorld = cameraController.CameraForward * input.MoveInput.y
                               + cameraController.CameraRight   * input.MoveInput.x;
            float shimmyAmount = Vector3.Dot(inputWorld, ledgeDirection);
            moveAlongLedge = ledgeDirection * shimmyAmount;
        }
        else
        {
            moveAlongLedge = ledgeDirection * input.MoveInput.x;
        }

        if (moveAlongLedge.magnitude > 0.1f)
        {
            float probeStep      = Mathf.Max(0.05f, data.moveSpeed * Time.deltaTime);
            Vector3 lateralProbe = moveAlongLedge.normalized * probeStep;
            Vector3 candidatePos = new Vector3(
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
            motor.Move(moveAlongLedge.normalized, data.moveSpeed);
        }
        else
        {
            motor.Stop();
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
        motor.SurfaceLocked  = false;
        attachedPlatform     = null;
        grabbedWallCollider  = null;

        // Limpiar debug para que los gizmos no muestren datos viejos de un grab anterior
        debugClimbOrigins = null;
        debugClimbHits    = null;
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

        sfx?.PlayLedgeClimb();

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
        neonFlasher?.Flash(grabbedWallCollider);
        sfx?.PlayLedgeJump();

        motor.DetachFromSurface();
        StopLedgeGrab();

        float gravity      = Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        // Dirección base: siempre alejándose de la pared
        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

        if (cameraController == null)
        {
            motor.SetHorizontalVelocity(wallOut, data.wallJumpSpeed);
            return;
        }

        // Input camera-relative
        Vector3 cameraForward = cameraController.CameraForward;
        Vector3 cameraRight   = cameraController.CameraRight;
        Vector3 inputDir      = cameraForward * input.MoveInput.y + cameraRight * input.MoveInput.x;

        // Extraer solo el componente LATERAL del input (perpendicular a wallOut).
        // Esto descarta lo que empuja hacia la pared o la refuerza,
        // y deja solo la desviación izquierda/derecha que el jugador quiso aplicar.
        Vector3 lateralInput = Vector3.ProjectOnPlane(inputDir, wallOut);

        // Combinar: siempre sale de la pared + inclinación lateral por input de cámara
        Vector3 finalDir = (wallOut + lateralInput).normalized;
        motor.SetHorizontalVelocity(finalDir, data.wallJumpSpeed);
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

        // Rays del climb check — solo visibles mientras el jugador está colgado del borde.
        // Se actualizan cada frame (RefreshClimbCheck en UpdateLedgeGrab) así que siempre
        // reflejan la superficie actual. Verde = hit (hay suelo), Rojo = miss.
        if (!IsLedgeGrabbing || debugClimbOrigins == null) return;
        for (int i = 0; i < debugClimbOrigins.Length; i++)
        {
            bool hit = debugClimbHits != null && i < debugClimbHits.Length && debugClimbHits[i];
            Gizmos.color = hit ? Color.green : Color.red;
            Gizmos.DrawLine(debugClimbOrigins[i], debugClimbOrigins[i] + Vector3.down * 0.4f);
            Gizmos.DrawSphere(debugClimbOrigins[i], 0.03f);
        }
    }

    /// <summary>
    /// Verifica dos condiciones antes de permitir trepar:
    /// 1. Altura: hay espacio vertical sobre el borde para pararse.
    /// 2. Ancho: la mayoría de los puntos del área de aterrizaje tienen suelo debajo.
    /// </summary>
    private bool HasSpaceToClimb()
    {
        // Check 1 — altura libre sobre el borde
        float requiredHeight = motor.HalfHeight * 2f;
        Vector3 heightOrigin = ledgeTopPoint + Vector3.up * 0.1f;
        if (Physics.SphereCast(heightOrigin, 0.2f, Vector3.up, out _, requiredHeight))
            return false;

        // Check 2 — ancho de la superficie: RefreshClimbCheck() ya corrió este frame en
        // UpdateLedgeGrab() así que debugClimbHits está al día. Solo contamos los hits.
        if (debugClimbHits == null) return false;

        int hitCount = 0;
        foreach (bool h in debugClimbHits)
            if (h) hitCount++;

        // Todos los puntos deben tener suelo debajo.
        return hitCount == debugClimbHits.Length;
    }

    /// <summary>
    /// Recalcula cada frame los orígenes y resultados del climb check.
    /// Se llama desde UpdateLedgeGrab() para que los gizmos siempre reflejen
    /// la posición y la superficie actual, sin necesidad de apretar Shift.
    ///
    /// 4 puntos de verificación alineados con el punto de aterrizaje:
    ///   - Centro
    ///   - Alejado de la pared  (wallOut)
    ///   - Derecha a lo largo del borde (+ledgeDir)
    ///   - Izquierda a lo largo del borde (-ledgeDir)
    /// El punto hacia la pared se omite: sabemos que hay pared ahí, siempre fallaría.
    /// Todos se castean hacia abajo 0.4f. Verde = hit, Rojo = miss.
    /// </summary>
    private void RefreshClimbCheck()
    {
        // Punto de aterrizaje: posición horizontal final del jugador después de trepar,
        // a la altura del borde (levemente por encima para no empezar dentro del collider).
        Vector3 landingXZ   = transform.position - wallNormal * climbCheckInset;
        Vector3 landingBase = new Vector3(landingXZ.x, ledgeTopPoint.y + 0.15f, landingXZ.z);
        float   r           = climbCheckRadius;

        // Ejes relativos al borde (plano horizontal)
        Vector3 wallOut  = -new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
        Vector3 ledgeDir =  Vector3.Cross(wallNormal, Vector3.up).normalized;

        debugClimbOrigins = new Vector3[]
        {
            landingBase,                      // centro
            landingBase + wallOut  * r,       // alejado de pared
            landingBase + ledgeDir * r,       // derecha del borde
            landingBase - ledgeDir * r,       // izquierda del borde
        };

        if (debugClimbHits == null || debugClimbHits.Length != debugClimbOrigins.Length)
            debugClimbHits = new bool[debugClimbOrigins.Length];

        for (int i = 0; i < debugClimbOrigins.Length; i++)
            debugClimbHits[i] = Physics.Raycast(debugClimbOrigins[i], Vector3.down, 0.4f);
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
