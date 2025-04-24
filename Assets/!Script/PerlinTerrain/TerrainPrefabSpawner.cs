//using UnityEditor.EditorTools;
using UnityEngine;

public class TerrainPrefabSpawner : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private GameObject[] prefabsToSpawn;
    [SerializeField] private int numberOfPrefabs = 50;
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 100f;
    [SerializeField] private float yOffset = 0f;

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
            float x = Random.Range(0f, terrainWidth);
            float z = Random.Range(0f, terrainLength);
            float y = terrainData.GetHeight((int)(x / terrainWidth * terrainData.heightmapResolution),
                                          (int)(z / terrainLength * terrainData.heightmapResolution));

            if (y < minHeight || y > maxHeight)
                continue;

            Vector3 spawnPos = new Vector3(x + terrainPos.x, y + terrainPos.y, z + terrainPos.z);
            GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];

            float prefabBottomOffset = 0f;
            Renderer renderer = prefab.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Vector3 localBottom = renderer.bounds.min;
                prefabBottomOffset = -localBottom.y;
            }

            spawnPos.y += prefabBottomOffset + yOffset;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject obj = PoolManager.Instance.GetObject(prefab, spawnPos, rotation, transform);
        }
    }
}