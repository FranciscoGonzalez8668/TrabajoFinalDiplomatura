/// <summary>
/// Interfaz base para todas las habilidades de movimiento del jugador.
/// PlayerStateMachine es responsable de llamar a los métodos del ciclo de vida.
/// </summary>
public interface IMovementAbility
{
    /// <summary>True mientras la habilidad está activa.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Evalúa si las condiciones para activar la habilidad se cumplen.
    /// No debe tener efectos secundarios.
    /// </summary>
    bool CanStart();

    /// <summary>Se llama una vez cuando la habilidad arranca.</summary>
    void StartAbility();

    /// <summary>Se llama cada frame mientras IsActive es true.</summary>
    void UpdateAbility();

    /// <summary>Se llama para detener la habilidad de forma limpia.</summary>
    void StopAbility();

    /// <summary>
    /// Detiene la habilidad inmediatamente sin condiciones.
    /// Usado en muerte, respawn o forzado externo.
    /// </summary>
    void ForceStop();
}
