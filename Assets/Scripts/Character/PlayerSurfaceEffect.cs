using UnityEngine;

/// <summary>
/// Cambia el material de los bordes neon de la superficie que el jugador
/// está usando (wall run, wall run vertical, ledge grab).
/// Da la sensación de que el jugador "activa" la pared al interactuar con ella.
/// </summary>
public class PlayerSurfaceEffect : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private CharacterMotor     motor;
    [SerializeField] private Material           wallActiveMaterial;
    [SerializeField] private float              revertDelay = 0.2f;

    private NeonEdges currentEdges;
    private float     revertTimer;

    private void Update()
    {
        NeonEdges target = FindTargetEdges();

        if (target != null)
        {
            // Nueva superficie activa — resetear timer y aplicar efecto
            revertTimer = revertDelay;

            if (target != currentEdges)
            {
                if (currentEdges != null)
                    currentEdges.ClearRuntimeMaterial();

                currentEdges = target;
                currentEdges.SetRuntimeMaterial(wallActiveMaterial);
            }
        }
        else if (currentEdges != null)
        {
            // Sin superficie activa — esperar el delay antes de revertir
            revertTimer -= Time.deltaTime;
            if (revertTimer <= 0f)
            {
                currentEdges.ClearRuntimeMaterial();
                currentEdges = null;
            }
        }
    }

    private NeonEdges FindTargetEdges()
    {
        switch (stateMachine.CurrentState)
        {
            case PlayerStateMachine.PlayerState.WallRunning:
                return RaycastEdges(-motor.WallNormal);

            case PlayerStateMachine.PlayerState.VerticalWallRunning:
                return RaycastEdges(transform.forward);

            case PlayerStateMachine.PlayerState.LedgeGrabbing:
                return RaycastEdges(transform.forward);

            default:
                return null;
        }
    }

    private NeonEdges RaycastEdges(Vector3 direction)
    {
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, 1.5f))
            return hit.collider.GetComponentInParent<NeonEdges>();

        return null;
    }

    private void OnDisable()
    {
        if (currentEdges != null)
        {
            currentEdges.ClearRuntimeMaterial();
            currentEdges = null;
        }
    }
}
