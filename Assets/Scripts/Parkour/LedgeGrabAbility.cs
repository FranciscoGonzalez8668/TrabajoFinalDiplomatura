using System.Threading;
using UnityEngine;
using UnityEngine.AI;

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

        Vector3 chestPosition = transform.position + Vector3.up * data.ledgeCheckHeightOffset;
        Debug.DrawRay(chestPosition, transform.forward * data.ledgeDetectionDistance, Color.red);
        if(!Physics.Raycast(chestPosition,transform.forward ,out RaycastHit wallHit, data.ledgeDetectionDistance))
        {
            Debug.Log("Ray1 no golpea nada");
            return;
        }
    
        Vector3 ray2Origin =new Vector3(
            wallHit.point.x, 
            transform.position.y + data.ledgeGrabReach, 
            wallHit.point.z);
        
        Debug.DrawRay(ray2Origin, Vector3.down * data.ledgeGrabReach, Color.green);
        if(!Physics.SphereCast(ray2Origin, 0.1f, Vector3.down, out RaycastHit ledgeHit, data.ledgeCheckHeightOffset)) 
        {
            Debug.Log("Ray2 no golpea nada");
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
        hangTimer = data.ledgeHangTimeout;
        hangPosition = new Vector3(transform.position.x, ledgeTopPoint.y -0.1f, transform.position.z);
        this.ledgeTopPoint = ledgeTopPoint;
        motor.OverrideGravity = true;
        motor.Stop();
        motor.SetVerticalVelocity(0f);

    }

    void UpdateLedgeGrab()
    {
        Debug.Log($"IsLedgeGrabbing: {IsLedgeGrabbing} | isClimbing: {isClimbing} | hangTimer: {hangTimer:F2}");
        Debug.Log($"ClimbPressed: {input.ClimbPressed}");
        Vector3 pos = transform.position;
        pos.y = hangPosition.y;
        transform.position = pos;
        motor.SetVerticalVelocity(0f);

        Vector3 ledgeDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 moveAlongLedge = ledgeDirection * input.MoveInput.x;

        if(moveAlongLedge.magnitude > 0.1f)
            motor.Move(moveAlongLedge, data.moveSpeed);
        else
            motor.Stop();

        if (input.ClimbPressed )
        {
            Debug.Log("ClimbPressed detectado");
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

        if (Mathf.Abs(input.MoveInput.x) > 0.1f)
        {
            Vector3 lateralDir = Vector3.Cross(wallNormal, Vector3.up).normalized;
            motor.SetHorizontalVelocity(lateralDir * Mathf.Sign(input.MoveInput.x), data.wallJumpSpeed);
        }
    }

    void StopLedgeGrab()
    {
        IsLedgeGrabbing = false;
        motor.OverrideGravity = false;
        motor.Stop();
        grabCooldown = 0.5f;
    }

}