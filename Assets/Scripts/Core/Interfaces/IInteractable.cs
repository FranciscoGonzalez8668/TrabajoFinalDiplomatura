/// <summary>
/// Interfaz para cualquier objeto con el que el jugador pueda interactuar presionando E.
/// </summary>
public interface IInteractable
{
    /// <summary>Ejecuta la interacción.</summary>
    void Interact();

    /// <summary>True si el objeto puede ser interactuado en este momento.</summary>
    bool CanInteract { get; }
}
