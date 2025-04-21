using UnityEngine;

namespace TerrainTesselationTools
{
    public class TerrainGridTiler : MonoBehaviour
    {
        [SerializeField] private Vector2 gridSize = new Vector2(10, 10);
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector2 centerOffset;
        public Vector2 GetGridSize()
        {
            return gridSize;
        }

        public float GetTileSize()
        {
            return tileSize;
        }

        public Vector2 GetCenterOffset()
        {
            return centerOffset;
        }
    }
}

