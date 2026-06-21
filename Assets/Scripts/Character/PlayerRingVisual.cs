using UnityEngine;

/// <summary>
/// Cambia el material del anillo según el estado del jugador.
/// Grounded → material cyan. Wall run / ledge grab → material violeta.
/// Sprint → lerp suave hacia sprintTintColor (más blanco por defecto).
/// La transición al entrar al sprint es suave; al salir, abrupta.
/// </summary>
public class PlayerRingVisual : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private Renderer ringRenderer;

    [Header("Materiales")]
    [SerializeField] private Material groundedMaterial;
    [SerializeField] private Material wallMaterial;

    [Header("Sprint")]
    [Tooltip("Color destino al sprintar. HDR — subí la Intensity para que se note sobre materiales neon.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color sprintTintColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Velocidad al entrar al sprint (más bajo = más suave).")]
    [SerializeField] [Range(0.5f, 10f)] private float sprintInSpeed  = 2f;
    [Tooltip("Velocidad al salir del sprint (más alto = más abrupto).")]
    [SerializeField] [Range(1f, 30f)]  private float sprintOutSpeed = 15f;

    private Material _groundedInstance;
    private Color    _baseColor;
    private float    _sprintT;

    // propiedad del shader que controla el color visible
    // "_EmissionColor" para materiales neon/outline, "_Color" o "_BaseColor" para los demás
    private static readonly string[] _colorProps = { "_EmissionColor", "_Color", "_BaseColor" };
    private string _resolvedProp;

    private void Awake()
    {
        if (groundedMaterial == null || ringRenderer == null) return;

        _groundedInstance = new Material(groundedMaterial);

        // Detectar qué propiedad de color usa este material
        foreach (string prop in _colorProps)
        {
            if (_groundedInstance.HasProperty(prop))
            {
                _resolvedProp = prop;
                break;
            }
        }

        _baseColor = _resolvedProp != null
            ? groundedMaterial.GetColor(_resolvedProp)
            : groundedMaterial.color;
    }

    private void Update()
    {
        if (stateMachine == null || ringRenderer == null) return;

        PlayerStateMachine.PlayerState state = stateMachine.CurrentState;

        // El lerp de sprint corre siempre, incluso en el aire
        bool  isSprinting = state == PlayerStateMachine.PlayerState.Sprinting;
        float speed       = isSprinting ? sprintInSpeed : sprintOutSpeed;
        _sprintT = Mathf.MoveTowards(_sprintT, isSprinting ? 1f : 0f, speed * Time.deltaTime);

        if (IsWallState(state))
        {
            ringRenderer.sharedMaterial = wallMaterial;
        }
        else if (IsGroundedState(state) && _groundedInstance != null)
        {
            Color c = Color.Lerp(_baseColor, sprintTintColor, _sprintT);
            if (_resolvedProp != null)
                _groundedInstance.SetColor(_resolvedProp, c);
            else
                _groundedInstance.color = c;

            ringRenderer.sharedMaterial = _groundedInstance;
        }
        // Jumping / Falling: no toca el material — mantiene el último
    }

    private bool IsWallState(PlayerStateMachine.PlayerState s) =>
        s == PlayerStateMachine.PlayerState.WallRunning     ||
        s == PlayerStateMachine.PlayerState.VerticalWallRunning ||
        s == PlayerStateMachine.PlayerState.LedgeGrabbing;

    private bool IsGroundedState(PlayerStateMachine.PlayerState s) =>
        s == PlayerStateMachine.PlayerState.Idle     ||
        s == PlayerStateMachine.PlayerState.Running  ||
        s == PlayerStateMachine.PlayerState.Sprinting;

    private void OnDestroy()
    {
        if (_groundedInstance != null)
            Destroy(_groundedInstance);
    }
}
