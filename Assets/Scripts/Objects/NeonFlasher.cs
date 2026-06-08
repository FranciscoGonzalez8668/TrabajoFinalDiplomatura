using System.Collections;
using UnityEngine;

/// <summary>
/// Utilitario reutilizable para flashear el NeonEdges de un collider por un tiempo.
/// Las abilities llaman Flash(hit) pasando el RaycastHit o el Collider de la pared.
/// </summary>
public class NeonFlasher : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float    flashDuration = 0.35f;

    public void Flash(Collider wallCollider)
    {
        if (flashMaterial == null || wallCollider == null) return;

        NeonEdges neon = wallCollider.GetComponentInParent<NeonEdges>();
        if (neon == null) return;

        StartCoroutine(FlashRoutine(neon));
    }

    public void Flash(RaycastHit hit) => Flash(hit.collider);

    private IEnumerator FlashRoutine(NeonEdges neon)
    {
        neon.SetRuntimeMaterial(flashMaterial);
        yield return new WaitForSeconds(flashDuration);
        neon.ClearRuntimeMaterial();
    }
}
