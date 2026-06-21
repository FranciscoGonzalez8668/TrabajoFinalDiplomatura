using UnityEngine;

/// <summary>
/// Habilidad de wall run vertical: el jugador corre hacia una pared frontal y sube por ella.
/// Incluye salto lateral desde la pared (con dirección relativa a la cámara).
/// </summary>
public class VerticalWallRunAbility : MonoBehaviour, IMovementAbility
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;
    [SerializeField] private CameraController cameraController;

    [Header("Vertical Wall Run")]
    [Tooltip("Velocidad vertical a lo largo del tiempo. Eje x = tiempo normalizado a 1. Eje y = velocidad vertical")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Audio")]
    [SerializeField] private PlayerSFX sfx;

    public bool IsActive => IsVerticalWallRunning;
    public bool IsVerticalWallRunning { get; private set; }

    /// <summary>
    /// True el frame en que se ejecutó el salto lateral desde la pared vertical.
    /// PlayerStateMachine lo lee para transicionar a Jumping.
    /// </summary>
    public bool JumpedFromVerticalWall { get; private set; }

    private float wallRunTimer;
    private float cooldownTimer;
    private bool hasLeftGround;

    private void OnEnable()
    {
        cameraController = CameraController.Instance;
    }

    public bool CanStart()
    {
        if (JumpedFromVerticalWall && !motor.IsFrontWall)
            JumpedFromVerticalWall = false;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        return input.SprintHeld   &&
               motor.IsFrontWall  &&
               motor.IsGrounded   &&
               !IsVerticalWallRunning &&
               cooldownTimer <= 0f;
    }

    public void StartAbility()
    {
        IsVerticalWallRunning = true;
        wallRunTimer  = 0f;
        hasLeftGround = false;
        motor.OverrideGravity = true;
        motor.Stop();

        sfx?.PlayVerticalWallRun();
    }

    public void UpdateAbility()
    {
        if (!motor.IsGrounded)
            hasLeftGround = true;

        // Salto lateral desde la pared — solo disponible después de despegarse del suelo
        if (input.JumpPressed && hasLeftGround)
        {
            PerformLateralJump();
            return;
        }

        bool shouldStop = !input.SprintHeld             ||
                          !motor.IsFrontWall             ||
                          wallRunTimer >= data.verticalWallRunDuration ||
                          (hasLeftGround && motor.IsGrounded);

        if (shouldStop)
        {
            StopAbility();
            return;
        }

        float normalizedTime = wallRunTimer / data.verticalWallRunDuration;
        float verticalSpeed  = speedCurve.Evaluate(normalizedTime) * data.verticalWallRunSpeed;
        motor.SetVerticalVelocity(verticalSpeed);

        wallRunTimer += Time.deltaTime;
    }

    // -------------------------------------------------------
    // SALTO LATERAL
    // -------------------------------------------------------

    private void PerformLateralJump()
    {
        // Dirección base: alejarse de la pared (normal frontal, solo componente horizontal)
        Vector3 wallOut = new Vector3(motor.FrontWallNormal.x, 0f, motor.FrontWallNormal.z).normalized;

        // Input lateral del jugador relativo a la cámara
        Vector3 lateral = cameraController.CameraRight * input.MoveInput.x;

        // Combinar: la normal empuja al jugador de la pared, el input lateral orienta el salto
        Vector3 jumpDirection = (wallOut + lateral).normalized;

        motor.SetHorizontalVelocity(jumpDirection, data.wallJumpSpeed);

        float gravity       = Physics.gravity.y;
        float jumpVelocity  = Mathf.Sqrt(2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight);
        motor.SetVerticalVelocity(jumpVelocity);

        JumpedFromVerticalWall = true;
        StopAbility();
    }

    // -------------------------------------------------------

    public void StopAbility()
    {
        IsVerticalWallRunning = false;
        wallRunTimer          = 0f;
        cooldownTimer         = data.verticalWallRunCooldown;
        motor.OverrideGravity = false;
        motor.SetVerticalVelocity(0f);
        motor.Stop();
    }

    public void ForceStop()
    {
        JumpedFromVerticalWall = false;
        cooldownTimer          = 0f;
        StopAbility();
    }
}
