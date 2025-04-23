using UnityEngine;

public class TerrainPrefabSpawner : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private GameObject[] prefabsToSpawn;
    [SerializeField] private int numberOfPrefabs = 50;
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 100f;
    [SerializeField] private float yOffset = 0f; // Optional manual offset for fine-tuning

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

            // Base spawn position
            Vector3 spawnPos = new Vector3(x + terrainPos.x, y + terrainPos.y, z + terrainPos.z);

            // Randomly select a prefab
            GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];

            // Calculate the prefab's bottom offset (assuming pivot is at the bottom)
            float prefabBottomOffset = 0f;
            Renderer renderer = prefab.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Use the renderer's bounds to find the bottom relative to the pivot
                Vector3 localBottom = renderer.bounds.min;
                prefabBottomOffset = -localBottom.y; // Positive offset to move prefab up
            }

            // Adjust spawn position to place prefab's bottom at terrain height
            spawnPos.y += prefabBottomOffset + yOffset;

            // Set rotation to be upright (aligned with world up) with random Y rotation
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Instantiate prefab
            Instantiate(prefab, spawnPos, rotation, transform);
        }
    }
}