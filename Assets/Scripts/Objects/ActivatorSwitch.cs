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
        /// <summary>Llama Reset() + Play() en cada interacción. Reinicia el ciclo desde cero.</summary>
        Restart
    }

    [Header("Configuración")]
    [SerializeField] private SwitchMode mode = SwitchMode.OneShot;

    [Tooltip("Arrastrá cualquier componente que implemente IActivatable (ObjectMover, ToggleObject, etc.)")]
    [SerializeField] private MonoBehaviour[] targets;

    [Header("Requerimiento")]
    [Tooltip("Activá si este switch requiere que el jugador tenga un item.")]
    [SerializeField] private bool requiresItem = false;
    [Tooltip("ID del item requerido. Debe coincidir exactamente con el itemId del PickableItem.")]
    [SerializeField] private string requiredItemId = "";
    [SerializeField] private LevelState levelState;

    [Header("Estado inicial")]
    [SerializeField] private bool startActivated = false;

    private bool used;
    private bool isPlaying;

    public bool CanInteract =>
        (mode != SwitchMode.OneShot || !used) &&
        ItemRequirementMet();

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
                foreach (MonoBehaviour t in targets)
                    (t as IActivatable)?.Reset();
                Activate();
                break;
        }
    }

    private void Activate()
    {
        isPlaying = true;
        foreach (MonoBehaviour t in targets)
            (t as IActivatable)?.Play();
    }

    private void Deactivate()
    {
        isPlaying = false;
        foreach (MonoBehaviour t in targets)
            (t as IActivatable)?.Stop();
    }
}
