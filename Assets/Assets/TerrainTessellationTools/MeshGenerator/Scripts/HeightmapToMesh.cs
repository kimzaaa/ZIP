using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [RequireComponent(typeof(BirdViewUVCalculator))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class HeightmapToMesh : MonoBehaviour
    {
        [SerializeField] private int vertexCountRow = 61;
        [SerializeField] private float meshScale = 10.0f;
        [SerializeField] private string savePath = "Assets/GeneratedMesh";
        private BirdViewUVCalculator birdViewUVCalculator;

        [ContextMenu("GenerateMesh")]
        public void GenerateMesh()
        {
            Mesh mesh = new Mesh();
            if (GetComponent<MeshFilter>() != null) GetComponent<MeshFilter>().mesh = mesh;
            mesh.RecalculateNormals();
            Vector3 centerOffset = new Vector3(-meshScale * 0.5f, 0, -meshScale * 0.5f);

            Vector3[] vertices = new Vector3[vertexCountRow * vertexCountRow];
            Vector2[] uv = new Vector2[vertexCountRow * vertexCountRow];
            int[] triangles = new int[(vertexCountRow - 1) * (vertexCountRow - 1) * 6];
            birdViewUVCalculator = GetComponent<BirdViewUVCalculator>();
            float[] bounds =
            {
                transform.position.x - meshScale / 2, // minX
                transform.position.z - meshScale / 2, // minZ
                transform.position.x + meshScale / 2, // maxX
                transform.position.z + meshScale / 2  // maxZ
            };
            birdViewUVCalculator.SetMinMaxPosition(bounds);
            for (int y = 0; y < vertexCountRow; y++)
            {
                for (int x = 0; x < vertexCountRow; x++)
                {
                    int index = y * vertexCountRow + x;
                    float normalizedX = (float)x / (vertexCountRow - 1);
                    float normalizedY = (float)y / (vertexCountRow - 1);
                    float heightValue = birdViewUVCalculator.GetHeightAtPosition(transform.position.x + normalizedX * meshScale - meshScale / 2, transform.position.z + normalizedY * meshScale - meshScale / 2);
                    vertices[index] = new Vector3(normalizedX * meshScale, heightValue, normalizedY * meshScale) + centerOffset;
                    uv[index] = new Vector2(normalizedX, normalizedY);
                }
            }

            int maxVertices = 65537;
            // Check if the mesh exceeds the vertex count limit
            if (vertices.Length > maxVertices)
            {
                Debug.LogWarning("Generated mesh has exceeded the maximum vertex count limit. Index format changed. Consider making meshes with 256 VertexCountRow or less");
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            int triangleIndex = 0;
            for (int y = 0; y < vertexCountRow - 1; y++)
            {
                for (int x = 0; x < vertexCountRow - 1; x++)
                {
                    int vertexIndex = y * vertexCountRow + x;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + vertexCountRow;
                    triangles[triangleIndex + 2] = vertexIndex + 1;
                    triangles[triangleIndex + 3] = vertexIndex + 1;
                    triangles[triangleIndex + 4] = vertexIndex + vertexCountRow;
                    triangles[triangleIndex + 5] = vertexIndex + vertexCountRow + 1;
                    triangleIndex += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            AssetDatabase.CreateAsset(mesh, savePath +".asset");
            AssetDatabase.SaveAssets();
            Debug.Log("Mesh saved at: " + savePath);
        }
    }
}