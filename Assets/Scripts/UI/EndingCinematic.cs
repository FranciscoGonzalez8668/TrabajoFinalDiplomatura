using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cinemática final. Corre automáticamente al cargar la EndScene.
///
/// Setup en Unity:
/// - playerVisual: el modelo visual del jugador en esta escena
/// - sphere: la esfera negra en el centro
/// - cam: la cámara de la escena
/// - cameraEndPoint: Empty GameObject con la posición/rotación final de la cámara
/// - endTextGroup: CanvasGroup con el texto final (alpha 0 al inicio)
/// </summary>
public class EndingCinematic : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private Transform sphere;
    [SerializeField] private Camera    cam;
    [SerializeField] private Transform cameraEndPoint;
    [SerializeField] private CanvasGroup endTextGroup;

    [Header("Cámara")]
    [SerializeField] private float cameraPullBackDuration = 3f;

    [Header("Jugador")]
    [SerializeField] private float playerWalkSpeed    = 2f;
    [SerializeField] private float absorptionDistance = 1f;

    [Header("Esfera")]
    [SerializeField] private float spherePulseSpeed  = 4f;
    [SerializeField] private float spherePulseAmount = 0.25f;
    [SerializeField] private float stabilizeDuration = 1.2f;

    [Header("Texto final")]
    [SerializeField] private float textFadeInDuration = 1.5f;
    [SerializeField] private float delayBeforeText    = 1f;

    private Vector3 sphereBaseScale;
    private bool    isPulsing;

    private void Start()
    {
        sphereBaseScale       = sphere.localScale;
        endTextGroup.alpha    = 0f;
        StartCoroutine(PlayCinematic());
    }

    private IEnumerator PlayCinematic()
    {
        // Fase 1 — cámara se aleja a la posición cinematográfica
        yield return StartCoroutine(PullBackCamera());

        // Fase 2 — jugador camina y esfera pulsa al mismo tiempo
        isPulsing = true;
        StartCoroutine(PulseSphere());
        yield return StartCoroutine(WalkPlayerToSphere());

        // Fase 3 — absorción: jugador desaparece
        yield return StartCoroutine(AbsorbPlayer());

        // Fase 4 — esfera se estabiliza
        isPulsing = false;
        yield return StartCoroutine(StabilizeSphere());

        // Fase 5 — texto final
        yield return new WaitForSeconds(delayBeforeText);
        yield return StartCoroutine(FadeInText());
    }

    // -------------------------------------------------------
    // Fase 1 — Pull back de cámara
    // -------------------------------------------------------

    private IEnumerator PullBackCamera()
    {
        Vector3    startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3    endPos   = cameraEndPoint.position;
        Quaternion endRot   = cameraEndPoint.rotation;

        float elapsed = 0f;
        while (elapsed < cameraPullBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraPullBackDuration);
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }

    // -------------------------------------------------------
    // Fase 2 — Jugador camina hacia la esfera
    // -------------------------------------------------------

    private IEnumerator WalkPlayerToSphere()
    {
        while (true)
        {
            Vector3 toSphere = sphere.position - playerVisual.position;
            toSphere.y = 0f;

            if (toSphere.magnitude <= absorptionDistance)
                yield break;

            playerVisual.position += toSphere.normalized * playerWalkSpeed * Time.deltaTime;
            playerVisual.rotation  = Quaternion.LookRotation(toSphere.normalized);
            yield return null;
        }
    }

    // -------------------------------------------------------
    // Fase 2 — Esfera pulsa (corre en paralelo)
    // -------------------------------------------------------

    private IEnumerator PulseSphere()
    {
        float t = 0f;
        while (isPulsing)
        {
            t += Time.deltaTime * spherePulseSpeed;
            float pulse = 1f + Mathf.Sin(t) * spherePulseAmount;
            sphere.localScale = sphereBaseScale * pulse;
            yield return null;
        }
    }

    // -------------------------------------------------------
    // Fase 3 — Absorción del jugador
    // -------------------------------------------------------

    private IEnumerator AbsorbPlayer()
    {
        Vector3 startScale = playerVisual.localScale;
        float   duration   = 0.6f;
        float   elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            playerVisual.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // La esfera hace un pulso grande al absorber
            float pulse = 1f + (1f - t) * spherePulseAmount * 3f;
            sphere.localScale = sphereBaseScale * pulse;

            yield return null;
        }

        playerVisual.localScale = Vector3.zero;
    }

    // -------------------------------------------------------
    // Fase 4 — Esfera se estabiliza
    // -------------------------------------------------------

    private IEnumerator StabilizeSphere()
    {
        Vector3 currentScale = sphere.localScale;
        float   elapsed      = 0f;

        while (elapsed < stabilizeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / stabilizeDuration);
            sphere.localScale = Vector3.Lerp(currentScale, sphereBaseScale, t);
            yield return null;
        }

        sphere.localScale = sphereBaseScale;
    }

    // -------------------------------------------------------
    // Fase 5 — Texto final
    // -------------------------------------------------------

    private IEnumerator FadeInText()
    {
        float elapsed = 0f;
        while (elapsed < textFadeInDuration)
        {
            elapsed          += Time.deltaTime;
            endTextGroup.alpha = Mathf.Clamp01(elapsed / textFadeInDuration);
            yield return null;
        }
        endTextGroup.alpha = 1f;
    }
}
