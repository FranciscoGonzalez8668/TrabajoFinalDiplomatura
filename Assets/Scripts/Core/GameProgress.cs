using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fuente de verdad del progreso global de la partida.
/// Guarda qué pisos se completaron y en cuál está el jugador actualmente.
/// Al entrar en Play mode se resetea automáticamente (OnEnable).
/// Para persistencia entre sesiones usar PlayerPrefs o serialización — no está en scope ahora.
/// </summary>
[CreateAssetMenu(fileName = "GameProgress", menuName = "QerlkKeeper/GameProgress")]
public class GameProgress : ScriptableObject
{
    [Header("Escenas de nivel, en orden de piso")]
    [Tooltip("Nombres exactos de las escenas tal como aparecen en Build Settings.")]
    public string[] levelSceneNames;

    [Header("Victoria")]
    [Tooltip("Nombre de la escena a cargar al completar el último piso.")]
    public string victorySceneName = "Victory";

    // --- Estado de sesión (no serializado — se recalcula cada vez que se entra en Play) ---

    private int currentLevelIndex;
    private HashSet<int> completedLevels;

    // --- Propiedades públicas ---

    public int CurrentLevelIndex  => currentLevelIndex;
    public int TotalLevels        => levelSceneNames != null ? levelSceneNames.Length : 0;
    public bool HasNextLevel      => currentLevelIndex + 1 < TotalLevels;

    public string CurrentSceneName =>
        levelSceneNames != null && currentLevelIndex < levelSceneNames.Length
            ? levelSceneNames[currentLevelIndex]
            : string.Empty;

    public string NextSceneName =>
        HasNextLevel ? levelSceneNames[currentLevelIndex + 1] : string.Empty;

    // --- Ciclo de vida ---

    private void OnEnable()
    {
        currentLevelIndex = 0;
        completedLevels   = new HashSet<int>();
    }

    // --- API ---

    /// <summary>Marca el piso actual como completado.</summary>
    public void MarkCurrentCompleted()
    {
        if (completedLevels == null) completedLevels = new HashSet<int>();
        completedLevels.Add(currentLevelIndex);
    }

    /// <summary>Avanza el índice al siguiente piso.</summary>
    public void AdvanceLevel()
    {
        if (HasNextLevel) currentLevelIndex++;
    }

    /// <summary>Devuelve true si ese índice de piso fue completado.</summary>
    public bool IsCompleted(int index) =>
        completedLevels != null && completedLevels.Contains(index);

    /// <summary>Resetea todo el progreso (nueva partida).</summary>
    public void Reset()
    {
        currentLevelIndex = 0;
        completedLevels?.Clear();
    }
}
