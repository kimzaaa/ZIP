using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int depth = 20;
    public int width = 256; // Width of the terrain
    public int height = 256; // Height of the terrain

    public float scale = 20f; // Scale of the Perlin noise

    void Update(){
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData){

        terrainData.heightmapResolution = width + 1; // Heightmap resolution must be width + 1

        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0,0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights(){
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++){
            for (int y = 0; y < height; y++){
                heights[x,y] = CalculateHeight(x,y);
            }
        }
        return heights;
    }

    float CalculateHeight(int x, int y){
        float xCoord = (float)x / width * scale;
        float yCoord = (float)y / height * scale;
        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
