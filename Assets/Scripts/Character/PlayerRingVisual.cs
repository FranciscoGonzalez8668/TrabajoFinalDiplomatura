using UnityEngine;

/// <summary>
/// Cambia el material del anillo según el estado del jugador.
/// Grounded → material cyan (mismo que los bordes de plataformas)
/// Wall run / ledge grab → material violeta
/// Jumping / Falling → mantiene el último material
/// </summary>
public class PlayerRingVisual : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private Renderer ringRenderer;

    [Header("Materiales (los mismos que usa NeonEdges)")]
    [SerializeField] private Material groundedMaterial;  // OutlineCyan
    [SerializeField] private Material wallMaterial;      // OutlineViolet

    private void Update()
    {
        if (stateMachine == null || ringRenderer == null) return;

        Material target = GetMaterialForState(stateMachine.CurrentState);
        if (target == null) return; // estado neutro — no actualizar

        if (ringRenderer.sharedMaterial != target)
            ringRenderer.sharedMaterial = target;
    }

    private Material GetMaterialForState(PlayerStateMachine.PlayerState state)
    {
        switch (state)
        {
            case PlayerStateMachine.PlayerState.Idle:
            case PlayerStateMachine.PlayerState.Running:
            case PlayerStateMachine.PlayerState.Sprinting:
                return groundedMaterial;

            case PlayerStateMachine.PlayerState.WallRunning:
            case PlayerStateMachine.PlayerState.VerticalWallRunning:
            case PlayerStateMachine.PlayerState.LedgeGrabbing:
                return wallMaterial;

            // Jumping y Falling mantienen el último material
            default:
                return null;
        }
    }
}
