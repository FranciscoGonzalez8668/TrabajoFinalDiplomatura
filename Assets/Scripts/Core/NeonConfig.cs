using UnityEngine;

/// <summary>
/// Configuración visual global de los bordes neon.
/// Todos los NeonEdges leen de acá por defecto.
/// </summary>
[CreateAssetMenu(fileName = "NeonConfig", menuName = "QerlkKeeper/NeonConfig")]
public class NeonConfig : ScriptableObject
{
    public Material edgeMaterial;
    public float    edgeThickness = 0.05f;
}
