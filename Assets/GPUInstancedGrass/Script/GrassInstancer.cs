using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Terrain))]
public class GrassInstancer : MonoBehaviour
{
    [Header("Grass Settings")]
    [SerializeField] private float grassDensity = 8.0f;
    [SerializeField] private float grassHeight = 0.8f;
    [SerializeField] private float heightVariation = 0.1f;
    [SerializeField] private float grassWidth = 0.4f;
    [SerializeField] private float positionOffset = 0.2f;
    [SerializeField] private Material grassMaterial; 
    [SerializeField] private bool alignToTerrainNormal = false;
    [SerializeField] private float minTerrainHeight = 0.47f;
    [SerializeField] private float grassHeightOffset = 0.2f; 

    [Header("Flower Settings")]
    [SerializeField] private List<FlowerType> flowerTypes = new List<FlowerType>();
    [SerializeField] private float flowerPatchDensity = 0.3f; 
    [SerializeField] private float flowerClusterSize = 3.0f;
    [SerializeField] private float flowerNoiseFrequency = 0.2f;
    [SerializeField] private float flowerHeightOffset = 0.2f;

    [Header("Performance Settings")]
    [SerializeField] private float spawnBuffer = 2.0f;
    [SerializeField] private float renderDistanceBuffer = 10.0f;
    [SerializeField] private int maxInstancesPerBatch = 1000;
    [SerializeField] private float gridCellSize = 6.0f;
    [SerializeField] private int maxTotalInstances = 40000;
    [SerializeField] private bool useFrustumCulling = true;

    [Header("Rendering Settings")]
    [SerializeField] private bool castShadows = true;

    private Terrain terrain;
    private Camera mainCamera;
    private Mesh grassMesh;
    private Mesh[] flowerMeshes;
    private Material[] grassMaterials;
    private List<Matrix4x4[]> grassBatches;
    private List<List<Matrix4x4[]>> flowerBatchesByType;
    private Dictionary<Vector2Int, CellData> instanceCache;
    private Vector3 lastCameraPos;
    private Quaternion lastCameraRot;
    private const float UPDATE_THRESHOLD = 1.0f;
    private const float ROTATION_THRESHOLD = 5.0f;
    private int totalGrassBatches;
    private int[] totalFlowerBatches;

    [System.Serializable]
    public class FlowerType
    {
        public Material material;
        public Mesh mesh;
        public float height = 0.5f;
        public float heightVariation = 0.1f;
        public float width = 0.3f;
        public float density = 1.0f;
    }

    private struct CellData
    {
        public List<Matrix4x4> GrassMatrices;
        public List<Matrix4x4> FlowerMatrices;
        public List<int> FlowerTypeIndices;
    }

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null) { Debug.LogError("Terrain component missing."); return; }
        mainCamera = Camera.main;
        if (mainCamera == null) { Debug.LogError("No main camera found."); return; }

        grassMesh = CreateQuadMesh();
        instanceCache = new Dictionary<Vector2Int, CellData>();
        grassMaterials = new Material[] { grassMaterial };

        flowerMeshes = new Mesh[flowerTypes.Count];
        for (int i = 0; i < flowerTypes.Count; i++)
        {
            flowerMeshes[i] = flowerTypes[i].mesh ?? CreateQuadMesh();
            if (flowerTypes[i].material != null) flowerTypes[i].material.enableInstancing = true;
        }

        if (grassMaterial != null)
        {
            grassMaterial.enableInstancing = true;
            grassMaterial.SetFloat("_ReceiveShadows", 1.0f);
        }

        GenerateAllInstances();
        PreallocateBatches();
        lastCameraPos = mainCamera.transform.position - Vector3.one * UPDATE_THRESHOLD * 2;
        lastCameraRot = mainCamera.transform.rotation;
        UpdateInstances();
    }

    void Update()
    {
        float positionDelta = Vector3.Distance(mainCamera.transform.position, lastCameraPos);
        float rotationDelta = Quaternion.Angle(mainCamera.transform.rotation, lastCameraRot);

        if (positionDelta > UPDATE_THRESHOLD || rotationDelta > ROTATION_THRESHOLD)
        {
            UpdateInstances();
            lastCameraPos = mainCamera.transform.position;
            lastCameraRot = mainCamera.transform.rotation;
        }
        RenderBatches();
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[] {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f)
        };
        int[] triangles = new int[] { 0, 2, 1, 1, 2, 3 };
        Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    void GenerateAllInstances()
    {
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;
        int gridWidth = Mathf.CeilToInt(terrainSize.x / gridCellSize);
        int gridHeight = Mathf.CeilToInt(terrainSize.z / gridCellSize);
        int totalInstances = 0;

        int targetInstancesPerCell = Mathf.FloorToInt((float)maxTotalInstances / (gridWidth * gridHeight));
        int clustersPerSide = Mathf.CeilToInt(Mathf.Sqrt(targetInstancesPerCell));

        float totalFlowerDensity = 0f;
        foreach (var flowerType in flowerTypes)
        {
            totalFlowerDensity += flowerType.density;
        }

        for (int x = 0; x < gridWidth && totalInstances < maxTotalInstances; x++)
        {
            for (int z = 0; z < gridHeight && totalInstances < maxTotalInstances; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!instanceCache.ContainsKey(cell))
                {
                    CellData cellData = GenerateInstancesForCell(cell, clustersPerSide, terrainSize, terrainPos, totalFlowerDensity);
                    instanceCache[cell] = cellData;
                    totalInstances += cellData.GrassMatrices.Count + cellData.FlowerMatrices.Count;
                }
            }
        }

        Debug.Log($"Generated {totalInstances} total instances (grass + flowers) across {gridWidth}x{gridHeight} grid.");
    }

    CellData GenerateInstancesForCell(Vector2Int cell, int clustersPerSide, Vector3 terrainSize, Vector3 terrainPos, float totalFlowerDensity)
    {
        List<Matrix4x4> grassMatrices = new List<Matrix4x4>();
        List<Matrix4x4> flowerMatrices = new List<Matrix4x4>();
        List<int> flowerTypeIndices = new List<int>();
        Vector3 cellOrigin = new Vector3(
            terrainPos.x + cell.x * gridCellSize,
            terrainPos.y,
            terrainPos.z + cell.y * gridCellSize
        );

        float cellWidth = Mathf.Min(gridCellSize, terrainSize.x - cell.x * gridCellSize);
        float cellHeight = Mathf.Min(gridCellSize, terrainSize.z - cell.y * gridCellSize);
        float densityX = cellWidth / clustersPerSide;
        float densityZ = cellHeight / clustersPerSide;

        for (int i = 0; i < clustersPerSide; i++)
        {
            for (int j = 0; j < clustersPerSide; j++)
            {
                Vector3 clusterPos = cellOrigin + new Vector3(i * densityX, 0, j * densityZ);
                Random.InitState((cell.x * 1000 + cell.y * 100 + i * 10 + j).GetHashCode());
                clusterPos += new Vector3(Random.Range(-positionOffset, positionOffset), 0f, Random.Range(-positionOffset, positionOffset));

                clusterPos.x = Mathf.Clamp(clusterPos.x, terrainPos.x, terrainPos.x + terrainSize.x);
                clusterPos.z = Mathf.Clamp(clusterPos.z, terrainPos.z, terrainPos.z + terrainSize.z);

                float terrainHeight = terrain.terrainData.GetHeight(
                    Mathf.FloorToInt((clusterPos.x - terrainPos.x) / terrainSize.x * (terrain.terrainData.heightmapResolution - 1)),
                    Mathf.FloorToInt((clusterPos.z - terrainPos.z) / terrainSize.z * (terrain.terrainData.heightmapResolution - 1))
                );

                if (terrainHeight < minTerrainHeight) continue;

                Vector3 basePos = new Vector3(clusterPos.x, terrainPos.y + terrainHeight, clusterPos.z);

                float grassH = grassHeight + Random.Range(-heightVariation, heightVariation);
                float grassW = grassWidth;

                float grassYRotation = Random.Range(0f, 360f);
                Quaternion grassRotation = Quaternion.Euler(0f, grassYRotation, 0f);

                if (alignToTerrainNormal)
                {
                    Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(
                        (clusterPos.x - terrainPos.x) / terrainSize.x,
                        (clusterPos.z - terrainPos.z) / terrainSize.z
                    );
                    grassRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal) * grassRotation;
                }

                clusterPos.y = basePos.y + grassHeightOffset + grassH * 0.5f;
                Matrix4x4 grassMatrix = Matrix4x4.TRS(clusterPos, grassRotation, new Vector3(grassW, grassH, grassW));
                grassMatrices.Add(grassMatrix);

                float noiseValue = Mathf.PerlinNoise(
                    (clusterPos.x + terrainPos.x) * flowerNoiseFrequency,
                    (clusterPos.z + terrainPos.z) * flowerNoiseFrequency
                );
                bool isFlowerPatch = flowerTypes.Count > 0 && noiseValue > (1f - flowerPatchDensity);

                if (isFlowerPatch)
                {
                    float flowerChoice = Random.Range(0f, totalFlowerDensity);
                    float currentSum = 0f;
                    int typeIndex = 0;
                    for (int k = 0; k < flowerTypes.Count; k++)
                    {
                        currentSum += flowerTypes[k].density;
                        if (flowerChoice <= currentSum)
                        {
                            typeIndex = k;
                            break;
                        }
                    }

                    var flowerType = flowerTypes[typeIndex];
                    float patchRadius = flowerClusterSize * 0.5f;
                    int flowerCount = Random.Range(3, 7);

                    for (int f = 0; f < flowerCount; f++)
                    {
                        Vector2 offset = Random.insideUnitCircle * patchRadius;
                        Vector3 flowerPos = basePos + new Vector3(offset.x, 0, offset.y);

                        float fHeight = flowerType.height + Random.Range(-flowerType.heightVariation, flowerType.heightVariation);
                        float fWidth = flowerType.width;

                        float flowerYRotation = Random.Range(0f, 360f);
                        Quaternion flowerRotation = Quaternion.Euler(0f, flowerYRotation, 0f);

                        if (alignToTerrainNormal)
                        {
                            Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(
                                (flowerPos.x - terrainPos.x) / terrainSize.x,
                                (flowerPos.z - terrainPos.z) / terrainSize.z
                            );
                            flowerRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal) * flowerRotation;
                        }

                        flowerPos.y = basePos.y + flowerHeightOffset + fHeight * 0.5f;
                        Matrix4x4 flowerMatrix = Matrix4x4.TRS(flowerPos, flowerRotation, new Vector3(fWidth, fHeight, fWidth));
                        flowerMatrices.Add(flowerMatrix);
                        flowerTypeIndices.Add(typeIndex);
                    }
                }
            }
        }

        return new CellData { GrassMatrices = grassMatrices, FlowerMatrices = flowerMatrices, FlowerTypeIndices = flowerTypeIndices };
    }

    void PreallocateBatches()
    {
        int totalGrassInstances = 0;
        Dictionary<int, int> flowerInstancesByType = new Dictionary<int, int>();
        foreach (var cell in instanceCache)
        {
            totalGrassInstances += cell.Value.GrassMatrices.Count;
            for (int i = 0; i < cell.Value.FlowerMatrices.Count; i++)
            {
                int typeIndex = cell.Value.FlowerTypeIndices[i];
                if (!flowerInstancesByType.ContainsKey(typeIndex)) flowerInstancesByType[typeIndex] = 0;
                flowerInstancesByType[typeIndex]++;
            }
        }

        totalGrassBatches = Mathf.CeilToInt((float)totalGrassInstances / maxInstancesPerBatch);
        grassBatches = new List<Matrix4x4[]>(totalGrassBatches);
        for (int i = 0; i < totalGrassBatches; i++)
        {
            grassBatches.Add(new Matrix4x4[maxInstancesPerBatch]);
        }

        totalFlowerBatches = new int[flowerTypes.Count];
        flowerBatchesByType = new List<List<Matrix4x4[]>>(flowerTypes.Count);
        for (int i = 0; i < flowerTypes.Count; i++)
        {
            int flowerCount = flowerInstancesByType.ContainsKey(i) ? flowerInstancesByType[i] : 0;
            totalFlowerBatches[i] = Mathf.CeilToInt((float)flowerCount / maxInstancesPerBatch);
            List<Matrix4x4[]> typeBatches = new List<Matrix4x4[]>(totalFlowerBatches[i]);
            for (int j = 0; j < totalFlowerBatches[i]; j++)
            {
                typeBatches.Add(new Matrix4x4[maxInstancesPerBatch]);
            }
            flowerBatchesByType.Add(typeBatches);
        }
    }

    void UpdateInstances()
    {
        Plane[] frustumPlanes = useFrustumCulling ? GeometryUtility.CalculateFrustumPlanes(mainCamera) : null;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int grassBatchIndex = 0;
        int grassAdded = 0;
        int[] flowerBatchIndices = new int[flowerTypes.Count];
        int[] flowerAdded = new int[flowerTypes.Count];

        int gridWidth = Mathf.CeilToInt(terrainSize.x / gridCellSize);
        int gridHeight = Mathf.CeilToInt(terrainSize.z / gridCellSize);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!instanceCache.ContainsKey(cell)) continue;

                Vector3 cellCenter = new Vector3(
                    terrainPos.x + cell.x * gridCellSize + gridCellSize / 2,
                    terrainPos.y + terrainSize.y / 2,
                    terrainPos.z + cell.y * gridCellSize + gridCellSize / 2
                );
                Bounds cellBounds = new Bounds(cellCenter, new Vector3(gridCellSize + spawnBuffer + renderDistanceBuffer, terrainSize.y + renderDistanceBuffer, gridCellSize + spawnBuffer + renderDistanceBuffer));

                if (useFrustumCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, cellBounds)) continue;

                CellData cellData = instanceCache[cell];

                foreach (var matrix in cellData.GrassMatrices)
                {
                    Vector3 pos = matrix.GetColumn(3);
                    Bounds instanceBounds = new Bounds(pos, new Vector3(grassWidth * 2 + renderDistanceBuffer, grassHeight * 2 + renderDistanceBuffer, grassWidth * 2 + renderDistanceBuffer));
                    if (useFrustumCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, instanceBounds)) continue;

                    if (grassBatchIndex >= grassBatches.Count)
                    {
                        grassBatches.Add(new Matrix4x4[maxInstancesPerBatch]);
                    }
                    if (grassAdded >= maxInstancesPerBatch)
                    {
                        grassBatchIndex++;
                        grassAdded = 0;
                        if (grassBatchIndex >= grassBatches.Count)
                        {
                            grassBatches.Add(new Matrix4x4[maxInstancesPerBatch]);
                        }
                    }

                    grassBatches[grassBatchIndex][grassAdded] = matrix;
                    grassAdded++;
                }

                if (grassAdded > 0)
                {
                    for (int i = 0; i < cellData.FlowerMatrices.Count; i++)
                    {
                        var matrix = cellData.FlowerMatrices[i];
                        int typeIndex = cellData.FlowerTypeIndices[i];
                        Vector3 pos = matrix.GetColumn(3);
                        Bounds instanceBounds = new Bounds(pos, new Vector3(flowerTypes[typeIndex].width * 2 + renderDistanceBuffer, flowerTypes[typeIndex].height * 2 + renderDistanceBuffer, flowerTypes[typeIndex].width * 2 + renderDistanceBuffer));
                        if (useFrustumCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, instanceBounds)) continue;

                        if (flowerBatchIndices[typeIndex] >= flowerBatchesByType[typeIndex].Count)
                        {
                            flowerBatchesByType[typeIndex].Add(new Matrix4x4[maxInstancesPerBatch]);
                        }
                        if (flowerAdded[typeIndex] >= maxInstancesPerBatch)
                        {
                            flowerBatchIndices[typeIndex]++;
                            flowerAdded[typeIndex] = 0;
                            if (flowerBatchIndices[typeIndex] >= flowerBatchesByType[typeIndex].Count)
                            {
                                flowerBatchesByType[typeIndex].Add(new Matrix4x4[maxInstancesPerBatch]);
                            }
                        }

                        flowerBatchesByType[typeIndex][flowerBatchIndices[typeIndex]][flowerAdded[typeIndex]] = matrix;
                        flowerAdded[typeIndex]++;
                    }
                }
            }
        }

        while (grassBatches.Count > grassBatchIndex + 1) grassBatches.RemoveAt(grassBatches.Count - 1);
        for (int i = 0; i < flowerTypes.Count; i++)
        {
            while (flowerBatchesByType[i].Count > flowerBatchIndices[i] + 1) flowerBatchesByType[i].RemoveAt(flowerBatchesByType[i].Count - 1);
        }
    }

    void RenderBatches()
    {
        for (int i = 0; i < grassBatches.Count; i++)
        {
            int count = (i == grassBatches.Count - 1) ? Mathf.Min(maxInstancesPerBatch, totalGrassBatches * maxInstancesPerBatch - i * maxInstancesPerBatch) : maxInstancesPerBatch;
            if (count > 0)
            {
                for (int j = 0; j < grassMesh.subMeshCount; j++)
                {
                    Graphics.DrawMeshInstanced(
                        grassMesh,
                        j,
                        grassMaterials[0],
                        grassBatches[i],
                        count,
                        null,
                        castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        true
                    );
                }
            }
        }

        for (int typeIndex = 0; typeIndex < flowerTypes.Count; typeIndex++)
        {
            var flowerType = flowerTypes[typeIndex];
            if (flowerType.material == null || flowerType.mesh == null) continue;

            var batches = flowerBatchesByType[typeIndex];
            for (int i = 0; i < batches.Count; i++)
            {
                int count = (i == batches.Count - 1) ? Mathf.Min(maxInstancesPerBatch, totalFlowerBatches[typeIndex] * maxInstancesPerBatch - i * maxInstancesPerBatch) : maxInstancesPerBatch;
                if (count > 0)
                {
                    for (int j = 0; j < flowerMeshes[typeIndex].subMeshCount; j++)
                    {
                        Graphics.DrawMeshInstanced(
                            flowerMeshes[typeIndex],
                            j,
                            flowerType.material,
                            batches[i],
                            count,
                            null,
                            castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                            true
                        );
                    }
                }
            }
        }
    }
}
