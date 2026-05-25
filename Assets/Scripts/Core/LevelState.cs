using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado de sesión del nivel. Persiste mientras dure la sesión de juego.
/// Vive en un ScriptableObject — independiente del ciclo de vida de los MonoBehaviours.
///
/// Uso:
///   - PickableItem escribe cuando el jugador recoge un item
///   - PickableItem lee en Awake para saber si ya fue recogido (ocultarse solo)
///   - ActivatorSwitch lee para saber si se puede interactuar
///   - Llamar Reset() al iniciar un nivel desde cero
/// </summary>
[CreateAssetMenu(fileName = "LevelState", menuName = "QerlkKeeper/LevelState")]
public class LevelState : ScriptableObject
{
    private HashSet<string> pickedItems;

    private void OnEnable()
    {
        // Se inicializa limpio cada vez que se carga el SO (inicio de sesión / Play mode)
        pickedItems = new HashSet<string>();
    }

    public bool IsPickedUp(string itemId)
    {
        return pickedItems != null && pickedItems.Contains(itemId);
    }

    public void PickUp(string itemId)
    {
        if (pickedItems == null) pickedItems = new HashSet<string>();
        pickedItems.Add(itemId);
    }

    /// <summary>Limpia todos los items recogidos. Llamar al iniciar el nivel desde cero.</summary>
    public void Reset()
    {
        pickedItems?.Clear();
    }
}
