using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [CustomEditor(typeof(TerrainGridTiler))]
    public class TerrainGridTilerEditor : Editor
    {
        void OnSceneGUI()
        {
            TerrainGridTiler terrainGridTiler = target as TerrainGridTiler;

            if (terrainGridTiler == null)
                return;

            float tileSize = terrainGridTiler.GetTileSize();
            Vector2 centerOffset = terrainGridTiler.GetCenterOffset();
            Vector2 gridSize = terrainGridTiler.GetGridSize();
            Vector3 center = terrainGridTiler.transform.position;

            int numTilesX = Mathf.FloorToInt(gridSize.x / tileSize);
            int numTilesY = Mathf.FloorToInt(gridSize.y / tileSize);

            float totalGridWidth = numTilesX * tileSize;
            float totalGridHeight = numTilesY * tileSize;

            Vector3 startPos = center - new Vector3(totalGridWidth * 0.5f, 0f, totalGridHeight * 0.5f);

            startPos += new Vector3(centerOffset.x, 0f, centerOffset.y);

            Handles.color = Color.white;

            for (int x = 0; x <= numTilesX; x++)
            {
                Vector3 start = startPos + new Vector3(x * tileSize, 0f, 0f);
                Vector3 end = start + new Vector3(0f, 0f, totalGridHeight);
                Handles.DrawLine(start, end);
            }

            for (int y = 0; y <= numTilesY; y++)
            {
                Vector3 start = startPos + new Vector3(0f, 0f, y * tileSize);
                Vector3 end = start + new Vector3(totalGridWidth, 0f, 0f);
                Handles.DrawLine(start, end);
            }
        }
    }
}