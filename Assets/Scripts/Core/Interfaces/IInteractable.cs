/// <summary>
/// Interfaz para cualquier objeto con el que el jugador pueda interactuar presionando E.
/// </summary>
public interface IInteractable
{
    /// <summary>Ejecuta la interacción.</summary>
    void Interact();

    /// <summary>True si el objeto puede ser interactuado en este momento.</summary>
    bool CanInteract { get; }

    /// <summary>Texto del hint que aparece cuando el jugador está en rango y puede interactuar. Ej: "Recoger Llave".</summary>
    string HintText { get; }

    /// <summary>Mensaje que se muestra si el jugador presiona E pero no puede interactuar. Vacío = no mostrar nada.</summary>
    string LockedText { get; }
}
