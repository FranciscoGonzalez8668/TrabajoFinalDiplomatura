using UnityEngine;

/// <summary>
/// Detecta IInteractable cercanos y los activa cuando el jugador presiona E.
/// Cada frame actualiza el hint de UI según el objeto más cercano en rango.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private InputHandler input;
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactLayer;

    private void Update()
    {
        IInteractable nearest = FindClosest();
        UpdateHint(nearest);

        if (!input.InteractPressed) return;
        if (nearest == null) return;

        if (nearest.CanInteract)
        {
            nearest.Interact();
        }
        else if (!string.IsNullOrEmpty(nearest.LockedText))
        {
            UIManager.Instance?.ShowNotification(nearest.LockedText);
        }
    }

    private void UpdateHint(IInteractable nearest)
    {
        if (nearest != null && nearest.CanInteract && !string.IsNullOrEmpty(nearest.HintText))
            UIManager.Instance?.SetInteractionHint(nearest.HintText);
        else
            UIManager.Instance?.ClearInteractionHint();
    }

    private IInteractable FindClosest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);

        IInteractable best     = null;
        float         bestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best     = interactable;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
