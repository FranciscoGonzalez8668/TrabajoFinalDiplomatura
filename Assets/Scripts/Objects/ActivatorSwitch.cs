using UnityEngine;

/// <summary>
/// Switch interactuable que controla uno o más IActivatable.
/// El jugador presiona E cerca de este objeto para activarlo.
///
/// Los targets son MonoBehaviour en el Inspector — cualquier componente
/// que implemente IActivatable puede ser arrastrado (ObjectMover, ToggleObject, etc.)
/// </summary>
public class ActivatorSwitch : MonoBehaviour, IInteractable
{
    public enum SwitchMode
    {
        /// <summary>Llama Play() una sola vez. No puede reactivarse.</summary>
        OneShot,
        /// <summary>Alterna entre Play() y Stop() en cada interacción.</summary>
        Toggle,
        /// <summary>Llama ResetToStart() + Play() en cada interacción. Reinicia el ciclo desde cero.</summary>
        Restart
    }

    [Header("Configuración")]
    [SerializeField] private SwitchMode mode = SwitchMode.OneShot;

    [Tooltip("Plataformas y objetos móviles a controlar.")]
    [SerializeField] private ObjectMover[] objectMovers;

    [Tooltip("Objetos que aparecen/desaparecen al activar.")]
    [SerializeField] private ToggleObject[] toggleObjects;

    [Header("Requerimiento")]
    [Tooltip("Activá si este switch requiere que el jugador tenga un item.")]
    [SerializeField] private bool requiresItem = false;
    [Tooltip("ID del item requerido. Debe coincidir exactamente con el itemId del PickableItem.")]
    [SerializeField] private string requiredItemId = "";
    [SerializeField] private LevelState levelState;

    [Header("Estado inicial")]
    [SerializeField] private bool startActivated = false;

    [Header("UI")]
    [Tooltip("Texto del hint cuando el jugador puede interactuar. Ej: 'Activar Puerta'.")]
    [SerializeField] private string hintText = "Activar";
    [Tooltip("Mensaje al activar el switch. Deja vacío para no mostrar nada.")]
    [SerializeField] private string activateMessage = "";
    [Tooltip("Mensaje cuando el jugador presiona E pero le falta el item requerido.")]
    [SerializeField] private string requiresItemMessage = "Necesitás un ítem para esto";

    private bool used;
    private bool isPlaying;

    public bool CanInteract =>
        (mode != SwitchMode.OneShot || !used) &&
        ItemRequirementMet();

    public string HintText  => string.IsNullOrEmpty(hintText) ? "Activar" : hintText;

    public string LockedText
    {
        get
        {
            if (requiresItem && !ItemRequirementMet())
                return string.IsNullOrEmpty(requiresItemMessage) ? "Necesitás un ítem para esto" : requiresItemMessage;
            return string.Empty;
        }
    }

    private bool ItemRequirementMet()
    {
        if (!requiresItem) return true;
        if (levelState == null || string.IsNullOrEmpty(requiredItemId)) return false;
        return levelState.IsPickedUp(requiredItemId);
    }

    private void Start()
    {
        if (startActivated)
            Activate();
    }

    public void Interact()
    {
        if (!CanInteract) return;

        switch (mode)
        {
            case SwitchMode.OneShot:
                Activate();
                used = true;
                break;

            case SwitchMode.Toggle:
                if (isPlaying) Deactivate();
                else           Activate();
                break;

            case SwitchMode.Restart:
                ForEachTarget(t => t.ResetToStart());
                Activate();
                break;
        }

        if (!string.IsNullOrEmpty(activateMessage))
            UIManager.Instance?.ShowNotification(activateMessage);
    }

    private void Activate()
    {
        isPlaying = true;
        ForEachTarget(t => t.Play());
    }

    private void Deactivate()
    {
        isPlaying = false;
        ForEachTarget(t => t.Stop());
    }

    private void ForEachTarget(System.Action<IActivatable> action)
    {
        if (objectMovers != null)
            foreach (ObjectMover m in objectMovers)
                if (m != null) action(m);

        if (toggleObjects != null)
            foreach (ToggleObject t in toggleObjects)
                if (t != null) action(t);
    }
}
