using UnityEngine;
using UnityEditor;

public static class NeonEdgesCleaner
{
    [MenuItem("QerlkKeeper/Limpiar NeonEdges duplicados")]
    public static void RemoveDuplicateNeonEdges()
    {
        NeonEdges[] all = Object.FindObjectsOfType<NeonEdges>();
        int removedComponents = 0;
        int removedChildren   = 0;

        foreach (NeonEdges ne in all)
        {
            GameObject go = ne.gameObject;

            // Primero destruir todos los hijos _NeonEdges del objeto
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = go.transform.GetChild(i);
                if (child.name == "_NeonEdges")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removedChildren++;
                }
            }

            // Eliminar duplicados — conservar el que tiene NeonConfig asignado
            NeonEdges[] components = go.GetComponents<NeonEdges>();
            if (components.Length > 1)
            {
                // Buscar el componente con config asignado
                NeonEdges keeper = System.Array.Find(components, c => c.HasConfig);
                if (keeper == null) keeper = components[0]; // fallback: guardar el primero

                foreach (NeonEdges c in components)
                {
                    if (c == keeper) continue;
                    Undo.DestroyObjectImmediate(c);
                    removedComponents++;
                }
            }
        }

        // Forzar rebuild en los componentes que quedaron
        NeonEdges[] remaining = Object.FindObjectsOfType<NeonEdges>();
        foreach (NeonEdges ne in remaining)
            ne.Rebuild();

        Debug.Log($"[NeonEdgesCleaner] {removedComponents} componente(s) y {removedChildren} hijo(s) duplicado(s) eliminados. {remaining.Length} objeto(s) reconstruidos.");
    }
}
