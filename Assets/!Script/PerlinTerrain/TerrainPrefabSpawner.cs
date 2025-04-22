using UnityEngine;

public class TerrainPrefabSpawner : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private GameObject[] prefabsToSpawn;
    [SerializeField] private int numberOfPrefabs = 50;
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 100f;
    [SerializeField] private bool alignToTerrainNormal = true;

    void Start()
    {
        if (terrain == null || prefabsToSpawn == null || prefabsToSpawn.Length == 0)
        {
            Debug.LogError("Terrain or Prefabs not assigned in TerrainPrefabSpawner!");
            return;
        }

        SpawnPrefabs();
    }

    void SpawnPrefabs()
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        float terrainWidth = terrainData.size.x;
        float terrainLength = terrainData.size.z;

        for (int i = 0; i < numberOfPrefabs; i++)
        {
            // Random position on terrain
            float x = Random.Range(0f, terrainWidth);
            float z = Random.Range(0f, terrainLength);
            
            // Get terrain height at position
            float y = terrainData.GetHeight((int)(x / terrainWidth * terrainData.heightmapResolution), 
                                          (int)(z / terrainLength * terrainData.heightmapResolution));

            // Check if height is within specified range
            if (y < minHeight || y > maxHeight)
                continue;

            Vector3 spawnPos = new Vector3(x + terrainPos.x, y + terrainPos.y, z + terrainPos.z);

            // Randomly select a prefab
            GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];

            // Calculate rotation
            Quaternion rotation = Quaternion.identity;
            if (alignToTerrainNormal)
            {
                Vector3 normal = terrainData.GetInterpolatedNormal(x / terrainWidth, z / terrainLength);
                rotation = Quaternion.FromToRotation(Vector3.up, normal);
            }

            // Add random rotation around Y axis
            rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Instantiate prefab
            Instantiate(prefab, spawnPos, rotation, transform);
        }
    }
}