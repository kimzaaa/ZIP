using UnityEngine;

public class PerlinNoise : MonoBehaviour
{
    public int width = 256; // Width of the terrain
    public int height = 256; // Height of the terrain

    public float scale = 20f;
    public float offsetX = 100f; // Offset for X coordinate
    public float offsetY = 100f; // Offset for Y coordinate
    void Start(){
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = GeneteratedTexture();
        offsetX = Random.Range(0f, 99999f); // Randomize offset for X coordinate
        offsetY = Random.Range(0f, 99999f); // Randomize offset for Y coordinate
    }

    Texture2D GeneteratedTexture(){
        Texture2D texture = new Texture2D(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = CalculateColor(x, y);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    Color CalculateColor(int x, int y)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;

        float sample = Mathf.PerlinNoise(xCoord,yCoord);
        return new Color(sample, sample, sample);
    }
}
