using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [CustomEditor(typeof(MeshToTerrainTiles))]
    public class RefreshSceneViewOnGenerate : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MeshToTerrainTiles terrainTiles = (MeshToTerrainTiles)target;

            if (GUILayout.Button("Generate Terrain Tiles"))
            {
                terrainTiles.GenerateTerrainTiles(); ;
            }
        }
    }
}