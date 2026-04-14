using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpikeMeshGenerator : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private BoxCollider sourceTrigger;
    [SerializeField] private bool useSourceTriggerSize = true;

    [Header("Shape")]
    [SerializeField] private float spikeWidth = 0.5f;
    [SerializeField] private float spikeHeight = 1.2f;
    [SerializeField] private float spikeDepth = 0.5f;
    [SerializeField] private float spacingX = 0.05f;
    [SerializeField] private float spacingZ = 0.05f;
    [SerializeField] private bool fillSurface = true;

    [Header("Base")]
    [SerializeField] private bool generateBase = true;
    [SerializeField] private float baseHeight = 0.15f;
    [SerializeField] private bool centerOnPivot = true;

    [Header("Collider")]
    [SerializeField] private bool updateMeshCollider = false;

    private MeshFilter meshFilter;
    private bool regenerateRequested;

    private void Awake()
    {
        Generate();
    }

    private void OnValidate()
    {
        regenerateRequested = true;
    }

    private void Update()
    {
        if (!regenerateRequested)
        {
            return;
        }

        regenerateRequested = false;
        Generate();
    }

    [ContextMenu("Generate Spike Mesh")]
    public void Generate()
    {
        spikeWidth = Mathf.Max(0.05f, spikeWidth);
        spikeHeight = Mathf.Max(0.05f, spikeHeight);
        spikeDepth = Mathf.Max(0.05f, spikeDepth);
        spacingX = Mathf.Max(0f, spacingX);
        spacingZ = Mathf.Max(0f, spacingZ);
        baseHeight = Mathf.Max(0.01f, baseHeight);

        EnsureComponents();

        Mesh mesh = new Mesh
        {
            name = "SpikeMesh"
        };

        MeshBuilder builder = new MeshBuilder();

        float meshWidth = spikeWidth;
        float meshDepth = spikeDepth;
        float minX = -meshWidth * 0.5f;
        float maxX = meshWidth * 0.5f;
        float minZ = -meshDepth * 0.5f;
        float maxZ = meshDepth * 0.5f;
        Vector3 meshCenter = Vector3.zero;

        if (useSourceTriggerSize && sourceTrigger != null)
        {
            GetTriggerAreaLocal(sourceTrigger, out minX, out maxX, out minZ, out maxZ, out meshCenter);
            meshWidth = Mathf.Max(0.05f, maxX - minX);
            meshDepth = Mathf.Max(0.05f, maxZ - minZ);
        }

        int spikeCountX = Mathf.Max(1, Mathf.CeilToInt((meshWidth + spacingX) / (spikeWidth + spacingX)));
        int spikeCountZ = Mathf.Max(1, Mathf.CeilToInt((meshDepth + spacingZ) / (spikeDepth + spacingZ)));

        float effectiveSpikeWidth = spikeWidth;
        float effectiveSpikeDepth = spikeDepth;

        float effectiveSpacingX = spacingX;
        float effectiveSpacingZ = spacingZ;

        if (fillSurface)
        {
            float totalRequestedSpacingX = spacingX * Mathf.Max(0, spikeCountX - 1);
            float totalRequestedSpacingZ = spacingZ * Mathf.Max(0, spikeCountZ - 1);

            effectiveSpikeWidth = Mathf.Max(0.05f, (meshWidth - totalRequestedSpacingX) / spikeCountX);
            effectiveSpikeDepth = Mathf.Max(0.05f, (meshDepth - totalRequestedSpacingZ) / spikeCountZ);

            if (effectiveSpikeWidth <= 0.05f)
            {
                effectiveSpikeWidth = meshWidth / spikeCountX;
                effectiveSpacingX = 0f;
            }

            if (effectiveSpikeDepth <= 0.05f)
            {
                effectiveSpikeDepth = meshDepth / spikeCountZ;
                effectiveSpacingZ = 0f;
            }
        }

        float totalWidth = spikeCountX * effectiveSpikeWidth + (spikeCountX - 1) * effectiveSpacingX;
        float totalDepth = spikeCountZ * effectiveSpikeDepth + (spikeCountZ - 1) * effectiveSpacingZ;
        float startX = fillSurface ? minX : meshCenter.x - totalWidth * 0.5f;
        float startZ = fillSurface ? minZ : meshCenter.z - totalDepth * 0.5f;
        float totalHeight = (generateBase ? baseHeight : 0f) + spikeHeight;
        float yOffset = centerOnPivot ? -totalHeight * 0.5f : 0f;

        if (generateBase)
        {
            Vector3 baseCenter = new Vector3(meshCenter.x, baseHeight * 0.5f + yOffset, meshCenter.z);
            AddBox(builder, baseCenter, meshWidth, baseHeight, meshDepth);
        }

        float spikeBottomY = (generateBase ? baseHeight : 0f) + yOffset;
        for (int x = 0; x < spikeCountX; x++)
        {
            float xMin = startX + x * (effectiveSpikeWidth + effectiveSpacingX);
            float xMax = xMin + effectiveSpikeWidth;

            for (int z = 0; z < spikeCountZ; z++)
            {
                float zMin = startZ + z * (effectiveSpikeDepth + effectiveSpacingZ);
                float zMax = zMin + effectiveSpikeDepth;
                AddSpike(builder, xMin, xMax, zMin, zMax, spikeBottomY, spikeHeight);
            }
        }

        mesh.SetVertices(builder.Vertices);
        mesh.SetTriangles(builder.Triangles, 0);
        mesh.SetNormals(builder.Normals);
        mesh.SetUVs(0, builder.Uvs);
        mesh.RecalculateBounds();

        AssignMesh(mesh);
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (sourceTrigger == null)
        {
            sourceTrigger = GetComponentInParent<BoxCollider>();
        }
    }

    private void GetTriggerAreaLocal(BoxCollider trigger, out float minX, out float maxX, out float minZ, out float maxZ, out Vector3 center)
    {
        Vector3 colliderCenter = trigger.center;
        Vector3 colliderSize = trigger.size;

        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;
        center = Vector3.zero;

        int sampleCount = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 localCorner = colliderCenter + new Vector3(
                    colliderSize.x * 0.5f * x,
                    0f,
                    colliderSize.z * 0.5f * z);

                Vector3 worldCorner = trigger.transform.TransformPoint(localCorner);
                Vector3 generatorLocalCorner = transform.InverseTransformPoint(worldCorner);

                minX = Mathf.Min(minX, generatorLocalCorner.x);
                maxX = Mathf.Max(maxX, generatorLocalCorner.x);
                minZ = Mathf.Min(minZ, generatorLocalCorner.z);
                maxZ = Mathf.Max(maxZ, generatorLocalCorner.z);
                center += generatorLocalCorner;
                sampleCount++;
            }
        }

        if (sampleCount > 0)
        {
            center /= sampleCount;
        }
    }

    private void AssignMesh(Mesh mesh)
    {
        if (Application.isPlaying)
        {
            Mesh oldMesh = meshFilter.mesh;
            meshFilter.mesh = mesh;
            if (oldMesh != null)
            {
                Destroy(oldMesh);
            }
        }
        else
        {
            Mesh oldMesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = mesh;
            if (oldMesh != null)
            {
                DestroyImmediate(oldMesh);
            }
        }

        if (!updateMeshCollider)
        {
            return;
        }

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    private static void AddSpike(MeshBuilder builder, float xMin, float xMax, float zMin, float zMax, float bottomY, float height)
    {
        float centerX = (xMin + xMax) * 0.5f;
        float centerZ = (zMin + zMax) * 0.5f;

        Vector3 top = new Vector3(centerX, bottomY + height, centerZ);
        Vector3 frontLeft = new Vector3(xMin, bottomY, zMax);
        Vector3 frontRight = new Vector3(xMax, bottomY, zMax);
        Vector3 backRight = new Vector3(xMax, bottomY, zMin);
        Vector3 backLeft = new Vector3(xMin, bottomY, zMin);

        AddQuad(builder, frontLeft, frontRight, backRight, backLeft, Vector3.down);
        AddTriangle(builder, frontLeft, frontRight, top);
        AddTriangle(builder, frontRight, backRight, top);
        AddTriangle(builder, backRight, backLeft, top);
        AddTriangle(builder, backLeft, frontLeft, top);
    }

    private static void AddBox(MeshBuilder builder, Vector3 center, float width, float height, float depth)
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float halfDepth = depth * 0.5f;

        Vector3 p0 = center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
        Vector3 p1 = center + new Vector3(halfWidth, -halfHeight, -halfDepth);
        Vector3 p2 = center + new Vector3(halfWidth, -halfHeight, halfDepth);
        Vector3 p3 = center + new Vector3(-halfWidth, -halfHeight, halfDepth);
        Vector3 p4 = center + new Vector3(-halfWidth, halfHeight, -halfDepth);
        Vector3 p5 = center + new Vector3(halfWidth, halfHeight, -halfDepth);
        Vector3 p6 = center + new Vector3(halfWidth, halfHeight, halfDepth);
        Vector3 p7 = center + new Vector3(-halfWidth, halfHeight, halfDepth);

        AddQuad(builder, p3, p2, p1, p0, Vector3.down);
        AddQuad(builder, p4, p5, p6, p7, Vector3.up);
        AddQuad(builder, p0, p1, p5, p4, Vector3.back);
        AddQuad(builder, p2, p3, p7, p6, Vector3.forward);
        AddQuad(builder, p1, p2, p6, p5, Vector3.right);
        AddQuad(builder, p3, p0, p4, p7, Vector3.left);
    }

    private static void AddQuad(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        int start = builder.Vertices.Count;

        builder.Vertices.Add(a);
        builder.Vertices.Add(b);
        builder.Vertices.Add(c);
        builder.Vertices.Add(d);

        builder.Normals.Add(normal);
        builder.Normals.Add(normal);
        builder.Normals.Add(normal);
        builder.Normals.Add(normal);

        builder.Uvs.Add(new Vector2(0f, 0f));
        builder.Uvs.Add(new Vector2(1f, 0f));
        builder.Uvs.Add(new Vector2(1f, 1f));
        builder.Uvs.Add(new Vector2(0f, 1f));

        builder.Triangles.Add(start);
        builder.Triangles.Add(start + 1);
        builder.Triangles.Add(start + 2);
        builder.Triangles.Add(start);
        builder.Triangles.Add(start + 2);
        builder.Triangles.Add(start + 3);
    }

    private static void AddTriangle(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = builder.Vertices.Count;
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        builder.Vertices.Add(a);
        builder.Vertices.Add(b);
        builder.Vertices.Add(c);

        builder.Normals.Add(normal);
        builder.Normals.Add(normal);
        builder.Normals.Add(normal);

        builder.Uvs.Add(new Vector2(0f, 0f));
        builder.Uvs.Add(new Vector2(1f, 0f));
        builder.Uvs.Add(new Vector2(0.5f, 1f));

        builder.Triangles.Add(start);
        builder.Triangles.Add(start + 1);
        builder.Triangles.Add(start + 2);
    }

    private sealed class MeshBuilder
    {
        public readonly System.Collections.Generic.List<Vector3> Vertices = new System.Collections.Generic.List<Vector3>();
        public readonly System.Collections.Generic.List<int> Triangles = new System.Collections.Generic.List<int>();
        public readonly System.Collections.Generic.List<Vector3> Normals = new System.Collections.Generic.List<Vector3>();
        public readonly System.Collections.Generic.List<Vector2> Uvs = new System.Collections.Generic.List<Vector2>();
    }
}
