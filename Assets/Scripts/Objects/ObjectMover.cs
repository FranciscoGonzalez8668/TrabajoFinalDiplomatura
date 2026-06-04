using UnityEngine;
using System.Collections;
/// <summary>
/// Componente de movimiento genérico.
/// Puede activarse y desactivarse desde afuera
/// </summary>

public class ObjectMover : MonoBehaviour, IActivatable
{
    public enum MoverType { Linear, Pendulum, PendulumBob, Vanishing }

    [Header( "General")]
    [SerializeField] private MoverType type = MoverType.Linear;
    [SerializeField] private bool startActive = true;

    [Header("Linear")]
    [SerializeField] private Vector3 offset = Vector3.right*2;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool loop = true;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);
    

    [Header("Pendulum (rotación sobre eje propio)")]
    [SerializeField] private Vector3 pivotAxis = Vector3.right * 2f;
    [SerializeField] private float maxAngle = 45f;

    [Header("PendulumBob (traslación en arco desde punto de suspensión)")]
    [Tooltip("Vector desde el objeto hasta el punto de suspensión. Ej: Vector3.up * 4 = pivote 4u arriba.")]
    [SerializeField] private Vector3 pivotOffset = Vector3.up * 4f;
    [Tooltip("Eje alrededor del cual oscila. Vector3.forward = izquierda-derecha. Vector3.right = adelante-atrás.")]
    [SerializeField] private Vector3 bobSwingAxis = Vector3.forward;
    // Reutiliza maxAngle, cycleDuration y pendulumCurve del bloque Pendulum

    [SerializeField] private AnimationCurve pendulumCurve = new AnimationCurve(
        new Keyframe(0f,    0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.5f,  0f),
        new Keyframe(0.75f,-1f),
        new Keyframe(1f,    0f)
    );

    [Header("Vanishing")]
    [SerializeField] private float vanishDelay = 0.5f;
    [SerializeField] private float respawnTime = 3f;


    private Vector3 startPosition;
    private Quaternion startRotation;

    private float timer; 

    private bool isVanished;
    private Renderer  cachedRenderer;
    private Collider  cachedCollider;
    private NeonEdges cachedNeonEdges;


    public bool IsActive { get; private set; }

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        IsActive = startActive;

        if(type == MoverType.Vanishing)
        {
            cachedRenderer = GetComponent<Renderer>();
            cachedCollider = GetComponent<Collider>();
            cachedNeonEdges = GetComponent<NeonEdges>();
        }
    }

    private void Update()
    {
        if(!IsActive || isVanished) return;
        switch (type)
        {
            case MoverType.Linear:
                UpdateLinear();
                break;
            case MoverType.Pendulum:
                UpdatePendulum();
                break;
            case MoverType.PendulumBob:
                UpdatePendulumBob();
                break;
        }
    }


    //:-------------------------------------------------------
    // Linear Movement
    //:-------------------------------------------------------
    private void UpdateLinear()
    {
        timer += Time.deltaTime;
        float normalizedTime;

        if (loop)
        {
            float cycleTime = timer % (duration*2f); // El ciclo de movimineto es ida y vuelta

            // Normalizamos el tiempo para que vaya de 0 a 1 en la ida, y de 1 a 0 en la vuelta
            normalizedTime = cycleTime < duration? cycleTime/duration : 1f - (cycleTime - duration)/duration;
        }
        else
        {
            normalizedTime = Mathf.Clamp01(timer/duration);
        }

        float t = movementCurve.Evaluate(normalizedTime);
        transform.position = startPosition + offset * t;
        Physics.SyncTransforms();
    }

    //:-------------------------------------------------------
    // Pendulum Movement
    //:-------------------------------------------------------

    private void UpdatePendulum()
    {
        timer += Time.deltaTime;
        float normalizedTime = (timer % duration) / duration;
        float angle = pendulumCurve.Evaluate(normalizedTime) * maxAngle;
        transform.rotation = startRotation * Quaternion.AngleAxis(angle, pivotAxis.normalized);
        Physics.SyncTransforms();
    }

    //:-------------------------------------------------------
    // PendulumBob Movement
    //:-------------------------------------------------------

    private void UpdatePendulumBob()
    {
        timer += Time.deltaTime;
        float normalizedTime = (timer % duration) / duration;
        float angle = pendulumCurve.Evaluate(normalizedTime) * maxAngle;

        // Punto de suspensión fijo en world space
        Vector3 pivot = startPosition + pivotOffset;

        // Dirección en reposo: del pivote hacia el objeto (opuesta al offset)
        Vector3 restDir = -pivotOffset.normalized;
        float armLength = pivotOffset.magnitude;

        // Rotar la dirección en reposo según el ángulo del péndulo
        Quaternion swing = Quaternion.AngleAxis(angle, bobSwingAxis.normalized);
        transform.position = pivot + swing * restDir * armLength;
        Physics.SyncTransforms();
    }

    //:-------------------------------------------------------
    // Vanishing
    // No usa OnCollisionEnter porque CharacterController no genera eventos Collision.
    // CharacterMotor llama NotifyPlayerContact() desde OnControllerColliderHit.
    //:-------------------------------------------------------

    /// <summary>
    /// Llamado por CharacterMotor cuando el jugador pisa esta plataforma desde arriba.
    /// </summary>
    public void NotifyPlayerContact()
    {
        if (type != MoverType.Vanishing || isVanished) return;
        StartCoroutine(VanishRoutine());
    }

    private IEnumerator VanishRoutine()
    {
        isVanished = true;

        yield return new WaitForSeconds(vanishDelay);
        SetVisible(false);

        yield return new WaitForSeconds(respawnTime);
        isVanished = false;   // Debe resetearse ANTES de re-habilitar para que el próximo piso active el trigger
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if(cachedRenderer  != null) cachedRenderer.enabled  = visible;
        if(cachedCollider  != null) cachedCollider.enabled  = visible;
        if(cachedNeonEdges != null) cachedNeonEdges.enabled = visible;
    }

    /// <summary>Reanuda el movimiento desde donde se pausó.</summary>
    public void Play() => IsActive = true;

    /// <summary>Pausa el movimiento. El timer queda donde está.</summary>
    public void Stop() => IsActive = false;

    /// <summary>Vuelve a la posición y rotación iniciales. No inicia el movimiento — llamar Play() después si hace falta.</summary>
    public void Reset()
    {
        IsActive = false;
        timer    = 0f;
        transform.position = startPosition;
        transform.rotation = startRotation;
        Physics.SyncTransforms();
    }
}