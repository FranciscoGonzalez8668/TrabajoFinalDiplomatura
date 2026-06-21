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

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce cuando la plataforma empieza a moverse (Play). Dejar vacío para no reproducir nada.")]
    [SerializeField] private AudioClip moveStartClip;
    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("Vanishing")]
    [SerializeField] private float    vanishDelay        = 1.5f;
    [SerializeField] private float    respawnTime        = 3f;
    [SerializeField] private Material vanishingMaterial;
    [SerializeField] private float    flickerSpeedStart  = 3f;
    [SerializeField] private float    flickerSpeedEnd    = 20f;
    [SerializeField] private AudioClip vanishWarningClip; // suena al empezar a titilar
    [SerializeField] private AudioClip vanishClip;        // suena al desaparecer

    private Vector3    startPosition;
    private Quaternion startRotation;
    private float      timer;

    private bool      isVanished;
    private bool      isFlickering;
    private float     flickerTimer;
    private float     flickerProgress; // 0→1 durante vanishDelay, usado para acelerar
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
            cachedRenderer  = GetComponent<Renderer>();
            cachedCollider  = GetComponent<Collider>();
            cachedNeonEdges = GetComponent<NeonEdges>();
        }
    }

    private void Update()
    {
        if (isFlickering) UpdateFlicker();
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

    private void UpdateFlicker()
    {
        if (cachedNeonEdges == null) return;
        float speed = Mathf.Lerp(flickerSpeedStart, flickerSpeedEnd, flickerProgress);
        flickerTimer += Time.deltaTime * speed;
        cachedNeonEdges.enabled = Mathf.Sin(flickerTimer * Mathf.PI) > 0f;
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

        // Titilado acelerado antes de desaparecer
        isFlickering    = true;
        flickerTimer    = 0f;
        flickerProgress = 0f;
        if (cachedNeonEdges != null && vanishingMaterial != null)
            cachedNeonEdges.SetRuntimeMaterial(vanishingMaterial);

        if (vanishWarningClip != null)
            AudioSource.PlayClipAtPoint(vanishWarningClip, transform.position);

        float elapsed = 0f;
        while (elapsed < vanishDelay)
        {
            elapsed        += Time.deltaTime;
            flickerProgress = Mathf.Clamp01(elapsed / vanishDelay);
            yield return null;
        }

        // Dejar de titilar y desaparecer
        isFlickering = false;
        if (cachedNeonEdges != null)
        {
            cachedNeonEdges.enabled = true;
            cachedNeonEdges.ClearRuntimeMaterial();
        }
        if (vanishClip != null)
            AudioSource.PlayClipAtPoint(vanishClip, transform.position);
        SetVisible(false);

        yield return new WaitForSeconds(respawnTime);
        isVanished = false;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if(cachedRenderer  != null) cachedRenderer.enabled  = visible;
        if(cachedCollider  != null) cachedCollider.enabled  = visible;
        if(cachedNeonEdges != null) cachedNeonEdges.enabled = visible;
    }

    /// <summary>Reanuda el movimiento desde donde se pausó.</summary>
    public void Play()
    {
        IsActive = true;
        if (moveStartClip != null)
            AudioSource.PlayClipAtPoint(moveStartClip, transform.position, audioVolume);
    }

    /// <summary>Pausa el movimiento. El timer queda donde está.</summary>
    public void Stop() => IsActive = false;

    /// <summary>Vuelve a la posición y rotación iniciales. No inicia el movimiento — llamar Play() después si hace falta.</summary>
    public void ResetToStart()
    {
        IsActive = false;
        timer    = 0f;
        transform.position = startPosition;
        transform.rotation = startRotation;
        Physics.SyncTransforms();
    }

    private void OnDrawGizmosSelected()
    {
        // En edit mode startPosition no está seteado (Awake no corrió), usar transform.position
        Vector3    origin   = Application.isPlaying ? startPosition : transform.position;
        Quaternion rotation = Application.isPlaying ? startRotation : transform.rotation;

        switch (type)
        {
            case MoverType.Linear:
                Vector3 destination = origin + offset;
                Gizmos.color = new Color(0.2f, 0.8f, 1f);
                Gizmos.DrawLine(origin, destination);
                Gizmos.DrawSphere(origin,      0.07f);
                Gizmos.DrawSphere(destination, 0.07f);
                // Línea de retorno punteada (semitransparente)
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
                Gizmos.DrawLine(destination, origin);
                break;

            case MoverType.Pendulum:
                // Muestra las dos posiciones extremas del balanceo
                Quaternion anglePos = rotation * Quaternion.AngleAxis( maxAngle, pivotAxis.normalized);
                Quaternion angleNeg = rotation * Quaternion.AngleAxis(-maxAngle, pivotAxis.normalized);
                Vector3 fwdPos = anglePos * Vector3.forward * 0.6f;
                Vector3 fwdNeg = angleNeg * Vector3.forward * 0.6f;
                Gizmos.color = new Color(1f, 0.8f, 0.2f);
                Gizmos.DrawLine(origin, origin + fwdPos);
                Gizmos.DrawLine(origin, origin + fwdNeg);
                Gizmos.DrawSphere(origin + fwdPos, 0.07f);
                Gizmos.DrawSphere(origin + fwdNeg, 0.07f);
                break;

            case MoverType.PendulumBob:
                // Muestra el pivote y las dos posiciones extremas del arco
                Vector3 pivot   = origin + pivotOffset;
                Vector3 restDir = -pivotOffset.normalized;
                float   arm     = pivotOffset.magnitude;
                Quaternion swingPos = Quaternion.AngleAxis( maxAngle, bobSwingAxis.normalized);
                Quaternion swingNeg = Quaternion.AngleAxis(-maxAngle, bobSwingAxis.normalized);
                Vector3 posA = pivot + swingPos * restDir * arm;
                Vector3 posB = pivot + swingNeg * restDir * arm;
                Gizmos.color = new Color(1f, 0.8f, 0.2f);
                Gizmos.DrawSphere(pivot,  0.07f);
                Gizmos.DrawLine(pivot, posA);
                Gizmos.DrawLine(pivot, posB);
                Gizmos.DrawLine(pivot, origin);
                Gizmos.DrawSphere(posA, 0.07f);
                Gizmos.DrawSphere(posB, 0.07f);
                break;
        }
    }
}