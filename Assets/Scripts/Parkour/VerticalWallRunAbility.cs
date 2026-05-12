using UnityEngine;

/// <summary>
/// Habilidad de wall run vertical: el jugador corre hacia una pared forntal y sube por ella.
/// Utilizamos un animation curve para manejar la velocidada
/// </summary>
public class VerticalWallRunAbility : MonoBehaviour, IMovementAbility
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;

    [Header("Vertical Wall Run")]
    [Tooltip("Velocidad vertical a loo largo del tiempo. Eje x = tiempo normalizado a 1. Eje y = velocidad vertical")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f,1f,1f,0f);

    public bool IsActive => IsVerticalWallRunning;
    public bool IsVerticalWallRunning {get; private set;}

    private float wallRunTimer;

    private bool hasLeftGround;

    public bool CanStart()
    {
        return input.SprintHeld &&
                motor.IsFrontWall &&
                motor.IsGrounded &&
                !IsVerticalWallRunning;
    }

    public void StartAbility()
    {
        IsVerticalWallRunning = true;
        wallRunTimer = 0f;
        hasLeftGround = false;
        motor.OverrideGravity = true;
        motor.Stop();
    }

    public void UpdateAbility()
    {
        if(!motor.IsGrounded)
        {
            hasLeftGround = true;
        }

        bool shouldStop = !input.SprintHeld || 
                        !motor.IsFrontWall || 
                        wallRunTimer >=  
                        data.verticalWallRunDuration || 
                        (hasLeftGround && motor.IsGrounded);

        if(shouldStop)        {
            StopAbility();
            return;
        }

        float normalizedTime = wallRunTimer / data.verticalWallRunDuration;
        float verticalSpeed = speedCurve.Evaluate(normalizedTime) * data.wallRunSpeed;
        motor.SetVerticalVelocity(verticalSpeed);

        wallRunTimer += Time.deltaTime;

    }

    public void StopAbility()
    {
        IsVerticalWallRunning = false;
        wallRunTimer = 0f;
        motor.OverrideGravity = false;
        motor.SetVerticalVelocity(0f);
    }

    public void ForceStop()
    {
        StopAbility();
    }

}