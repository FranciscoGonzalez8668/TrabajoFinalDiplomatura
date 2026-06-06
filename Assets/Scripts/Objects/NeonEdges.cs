using UnityEngine;

/// <summary>
/// Genera 12 tiras neon a lo largo de cada arista del cubo.
/// Cada tira corre el largo completo de la arista y sobresale levemente del borde.
/// Se regenera automáticamente al cambiar escala, grosor o material.
/// </summary>
[ExecuteAlways]
public class NeonEdges : MonoBehaviour
{
    [SerializeField] private NeonConfig config;

    [Tooltip("Si está tildado, usa los valores de abajo en lugar del NeonConfig global.")]
    [SerializeField] private bool overrideGlobal = false;

    [SerializeField] private Material overrideMaterial;
    [SerializeField] private float    overrideThickness = 0.05f;

    public bool      HasConfig        => config != null;

    // Override en runtime — no serializado, se usa para efectos temporales del jugador
    private Material runtimeMaterial;
    public void SetRuntimeMaterial(Material mat) { runtimeMaterial = mat; }
    public void ClearRuntimeMaterial()           { runtimeMaterial = null; }

    private Material ActiveMaterial  => runtimeMaterial != null ? runtimeMaterial :
                                        overrideGlobal   ? overrideMaterial :
                                        config?.edgeMaterial;
    private float    ActiveThickness => overrideGlobal ? overrideThickness : config?.edgeThickness ?? 0.05f;

    private Vector3  lastScale;
    private float    lastThickness;
    private Material lastMaterial;
    private GameObject edgesContainer;

    private void OnEnable()  => Rebuild();
    private void OnDisable() => ClearEdges();

    private void Update()
    {
        // Si no hay material válido no hacer nada — evita borrar bordes por null transitorio
        if (ActiveMaterial == null) return;

        // Reconstruir si los bordes desaparecieron inesperadamente
        if (edgesContainer == null)
        {
            Rebuild();
            return;
        }

        if (transform.localScale != lastScale    ||
            ActiveThickness      != lastThickness ||
            ActiveMaterial       != lastMaterial)
        {
            Rebuild();
        }
    }

    [ContextMenu("Regenerar bordes")]
    public void Rebuild()
    {
        ClearEdges();
        if (ActiveMaterial == null) return;

        edgesContainer = new GameObject("_NeonEdges");
        edgesContainer.hideFlags = HideFlags.DontSave;
        edgesContainer.transform.SetParent(transform, false);
        edgesContainer.transform.localPosition = Vector3.zero;
        edgesContainer.transform.localRotation = Quaternion.identity;
        edgesContainer.transform.localScale    = Vector3.one;

        Vector3 s = transform.localScale;
        float   t = ActiveThickness;

        // Grosor normalizado por escala del padre en cada eje
        float tx = t / s.x;
        float ty = t / s.y;
        float tz = t / s.z;

        // 4 aristas paralelas al eje X  (en y = ±0.5, z = ±0.5)
        CreateEdge(new Vector3( 0f,  0.5f,  0.5f), new Vector3(1f,  ty,  tz));
        CreateEdge(new Vector3( 0f, -0.5f,  0.5f), new Vector3(1f,  ty,  tz));
        CreateEdge(new Vector3( 0f,  0.5f, -0.5f), new Vector3(1f,  ty,  tz));
        CreateEdge(new Vector3( 0f, -0.5f, -0.5f), new Vector3(1f,  ty,  tz));

        // 4 aristas paralelas al eje Y  (en x = ±0.5, z = ±0.5)
        CreateEdge(new Vector3( 0.5f,  0f,  0.5f), new Vector3(tx, 1f,  tz));
        CreateEdge(new Vector3(-0.5f,  0f,  0.5f), new Vector3(tx, 1f,  tz));
        CreateEdge(new Vector3( 0.5f,  0f, -0.5f), new Vector3(tx, 1f,  tz));
        CreateEdge(new Vector3(-0.5f,  0f, -0.5f), new Vector3(tx, 1f,  tz));

        // 4 aristas paralelas al eje Z  (en x = ±0.5, y = ±0.5)
        CreateEdge(new Vector3( 0.5f,  0.5f,  0f), new Vector3(tx,  ty, 1f));
        CreateEdge(new Vector3(-0.5f,  0.5f,  0f), new Vector3(tx,  ty, 1f));
        CreateEdge(new Vector3( 0.5f, -0.5f,  0f), new Vector3(tx,  ty, 1f));
        CreateEdge(new Vector3(-0.5f, -0.5f,  0f), new Vector3(tx,  ty, 1f));

        lastScale     = transform.localScale;
        lastThickness = ActiveThickness;
        lastMaterial  = ActiveMaterial;
    }

    private void CreateEdge(Vector3 localPos, Vector3 localScale)
    {
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "Edge";
        edge.transform.SetParent(edgesContainer.transform, false);
        edge.transform.localPosition = localPos;
        edge.transform.localScale    = localScale;

        DestroyImmediate(edge.GetComponent<Collider>());
        edge.GetComponent<MeshRenderer>().sharedMaterial = ActiveMaterial;
    }

    private void ClearEdges()
    {
        if (edgesContainer != null)
            DestroyImmediate(edgesContainer);

        // Destruir TODOS los hijos _NeonEdges (puede haber más de uno por duplicados)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "_NeonEdges")
                DestroyImmediate(child.gameObject);
        }

        edgesContainer = null;
    }
}
