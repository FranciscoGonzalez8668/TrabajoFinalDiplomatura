using UnityEngine;

/// <summary>
/// Item recogible. El estado de si fue recogido vive en LevelState,
/// no en este MonoBehaviour — sobrevive recargas de escena y respawns.
///
/// En Awake consulta LevelState: si ya fue recogido, se oculta solo.
/// </summary>
public class PickableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemId   = "item_01";
    [SerializeField] private string itemName = "Item";
    [SerializeField] private LevelState levelState;

    [Header("UI")]
    [Tooltip("Texto del hint cuando el jugador está en rango. Ej: 'Recoger Llave'.")]
    [SerializeField] private string hintText = "Recoger";
    [Tooltip("Mensaje que aparece al recoger el item. Deja vacío para usar '{itemName} recogido'.")]
    [SerializeField] private string pickupMessage = "";

    private Renderer[] rends;
    private Collider[]  cols;

    public bool CanInteract => !levelState.IsPickedUp(itemId);
    public string ItemId    => itemId;
    public string ItemName  => itemName;
    public string HintText  => string.IsNullOrEmpty(hintText) ? $"Recoger {itemName}" : hintText;
    public string LockedText => string.Empty;

    private void Awake()
    {
        rends = GetComponentsInChildren<Renderer>();
        cols  = GetComponentsInChildren<Collider>();

        // Si ya fue recogido en esta sesión, ocultarse sin esperar interacción
        if (levelState.IsPickedUp(itemId))
            SetVisible(false);
    }

    public void Interact()
    {
        if (!CanInteract) return;

        levelState.PickUp(itemId);
        SetVisible(false);

        string msg = string.IsNullOrEmpty(pickupMessage) ? $"{itemName} recogido" : pickupMessage;
        UIManager.Instance?.ShowNotification(msg);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in rends) r.enabled = visible;
        foreach (Collider c in cols)  c.enabled = visible;
    }
}
