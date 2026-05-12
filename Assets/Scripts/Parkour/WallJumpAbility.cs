using UnityEngine;

/// <summary>
/// Habilidad de wall jump: impulso instantáneo al saltar desde una pared.
/// Separada de WallRunAbility porque es una mecánica distinta — no tiene duración ni estado activo.
/// Puede ejecutarse desde cualquier estado aéreo (Jumping, Falling).
/// </summary>
public class WallJumpAbility : MonoBehaviour, IMovementAbility
{
    [SerializeField] private PlayerData data;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private InputHandler input;

    // Es una habilidad instantánea — nunca tiene estado activo persistente
    public bool IsActive => false;

    /// <summary>True el frame en que se ejecutó el wall jump. PlayerStateMachine lo lee para transicionar a Jumping.</summary>
    public bool JumpedFromWall { get; private set; }

    public bool CanStart()
    {
        // Limpiar el flag cuando el jugador ya se alejó de la pared
        if (JumpedFromWall && !motor.IsTouchingWall)
            JumpedFromWall = false;

        return !JumpedFromWall &&
               motor.IsTouchingWall &&
               !motor.IsGrounded &&
               input.JumpPressed;
    }

    public void StartAbility()
    {
        Vector3 wallNormal = motor.WallNormal;

        // Componente horizontal perpendicular a la pared
        Vector3 wallOut = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;

        // Preservar inercia paralela a la pared
        Vector3 horizontalVelocity = motor.Velocity;
        Vector3 parallelVelocity = horizontalVelocity - Vector3.Dot(horizontalVelocity, wallOut) * wallOut;

        Vector3 horizontalImpulse;
        if (parallelVelocity.magnitude > 1f)
            horizontalImpulse = (wallOut * data.wallJumpForce + parallelVelocity.normalized * data.wallJumpSpeed).normalized;
        else
            horizontalImpulse = wallOut;

        motor.SetHorizontalVelocity(horizontalImpulse, data.wallJumpSpeed);

        float gravity = Physics.gravity.y;
        float jumpVelocity = 2f * Mathf.Abs(gravity) * motor.GravityScale * data.wallJumpHeight;
        motor.SetVerticalVelocity(Mathf.Sqrt(jumpVelocity));

        JumpedFromWall = true;
    }

    // Habilidad instantánea — estos métodos no tienen lógica
    public void UpdateAbility() { }
    public void StopAbility() { }

    public void ForceStop()
    {
        JumpedFromWall = false;
    }
}
