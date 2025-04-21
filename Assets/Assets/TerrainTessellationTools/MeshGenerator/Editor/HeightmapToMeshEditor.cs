using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [CustomEditor(typeof(HeightmapToMesh))]
    public class HeightmapToMeshEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HeightmapToMesh heightmapToMesh = (HeightmapToMesh)target;

            if (GUILayout.Button("Generate Mesh"))
            {
                heightmapToMesh.GenerateMesh(); ;
            }
        }
    }
}