using UnityEngine;
using System.Collections;
/// <summary>
/// Componente de movimiento genérico.
/// Puede activarse y desactivarse desde afuera
/// </summary>

public class ObjectMover : MonoBehaviour
{
    public enum MoverType {Linear, Pendulum, Vanishing}

    [Header( "General")]
    [SerializeField] private MoverType type = MoverType.Linear;
    [SerializeField] private bool startActive = true;

    [Header("Linear")]
    [SerializeField] private Vector3 offset = Vector3.right*2;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool loop = true;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);
    

    [Header("Pendulum")]
    [SerializeField] private Vector3 pivotAxis = Vector3.right *2f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float cycleDuration = 2f;

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
    private Renderer cachedRenderer;
    private Collider cachedCollider;


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
    }

    //:-------------------------------------------------------
    // Pendulum Movement
    //:-------------------------------------------------------

    private void UpdatePendulum()
    {
        
        timer += Time.deltaTime;
        float normalizedTime = (timer % cycleDuration) / cycleDuration;
        float angle = pendulumCurve.Evaluate(normalizedTime) * maxAngle;
        transform.rotation = startRotation * Quaternion.AngleAxis(angle,pivotAxis.normalized) ;
    }

    //:-------------------------------------------------------
    // Vanishing Movement
    //:-------------------------------------------------------

    private void OCollisionEnter(Collision collision)
    {
        if (type !=MoverType.Vanishing || isVanished || !collision.gameObject.CompareTag("Player")) return;

        StartCoroutine(VanishRoutine());
    }


    private IEnumerator VanishRoutine()
    {
        isVanished = true;

        yield return new WaitForSeconds(vanishDelay);
        SetVisible(false);
        yield return new WaitForSeconds(respawnTime);
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if(cachedRenderer != null) cachedRenderer.enabled = visible;
        if(cachedCollider != null) cachedCollider.enabled = visible;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;


}