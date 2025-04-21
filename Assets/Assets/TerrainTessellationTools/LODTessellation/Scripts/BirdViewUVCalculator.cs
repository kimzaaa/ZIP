using UnityEngine;

namespace TerrainTesselationTools
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class BirdViewUVCalculator : MonoBehaviour
    {
        [SerializeField] private float terrainRotationOffset;
        [SerializeField] private float terrainHeightForce = 1;
        [SerializeField][TextureDrawer] private Texture2D heightmap;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock materialPropertyBlock;

        private float minX;
        private float minZ;
        private float maxX;
        private float maxZ;
        private float terrainRotation;

        private void Awake()
        {
            CalculateBirdViewUVs();
            SetMaterialProperties();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                CalculateBirdViewUVs();
                SetMaterialProperties();
            }
        }
#endif

        public void CalculateBirdViewUVs()
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.GetPropertyBlock(materialPropertyBlock);
            meshFilter = GetComponent<MeshFilter>();
            if (heightmap == null || meshRenderer == null || meshFilter == null || meshFilter.sharedMesh == null)
            {
                enabled = false;
                return;
            }
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            float rotationY = transform.eulerAngles.y + terrainRotationOffset;

            minX = float.MaxValue;
            minZ = float.MaxValue;
            maxX = float.MinValue;
            maxZ = float.MinValue;

            // Find the minimum and maximum X and Z coordinates of the vertices
            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldVertex = transform.TransformPoint(vertex);

                if (worldVertex.x < minX)
                    minX = worldVertex.x;
                if (worldVertex.z < minZ)
                    minZ = worldVertex.z;
                if (worldVertex.x > maxX)
                    maxX = worldVertex.x;
                if (worldVertex.z > maxZ)
                    maxZ = worldVertex.z;
            }

            float rotationFactor = CalculateInnerSquareRatio(rotationY);

            minX -= transform.position.x;
            maxX -= transform.position.x;
            minZ -= transform.position.z;
            maxZ -= transform.position.z;

            minX /= rotationFactor;
            maxX /= rotationFactor;
            minZ /= rotationFactor;
            maxZ /= rotationFactor;

            minX += transform.position.x;
            maxX += transform.position.x;
            minZ += transform.position.z;
            maxZ += transform.position.z;

            terrainRotation = rotationY;
        }

        private static float CalculateInnerSquareRatio(float rotationAngleInDegrees)
        {
            float angleInRadians = rotationAngleInDegrees * Mathf.PI / 180.0f;
            float cosValue = Mathf.Abs(Mathf.Cos(angleInRadians));
            float sinValue = Mathf.Abs(Mathf.Sin(angleInRadians));

            float innerSquareRatio = cosValue + sinValue;
            return innerSquareRatio;
        }
        public void SetMaterialProperties()
        {
            if (meshRenderer != null)
            {
                var uvs = GetBirdViewUVs();
                meshRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat("_MinX", uvs[0]);
                materialPropertyBlock.SetFloat("_MinZ", uvs[1]);
                materialPropertyBlock.SetFloat("_MaxX", uvs[2]);
                materialPropertyBlock.SetFloat("_MaxZ", uvs[3]);
                materialPropertyBlock.SetFloat("_TerrainRotation", uvs[4]);
                materialPropertyBlock.SetFloat("_TerrainForce", uvs[5]);
                materialPropertyBlock.SetTexture("_HeightMap", heightmap);
                meshRenderer.SetPropertyBlock(materialPropertyBlock);
            }
        }
        public float[] GetBirdViewUVs()
        {
            float[] uvs = new float[6];
            uvs[0] = minX;
            uvs[1] = minZ;
            uvs[2] = maxX;
            uvs[3] = maxZ;
            uvs[4] = terrainRotation;
            uvs[5] = terrainHeightForce;
            return uvs;
        }
        public float GetHeightAtPosition(float x, float z)
        {
            if (heightmap == null) return 0;
            // Interpolate the height using the heightmap texture
            float normalizedX = Mathf.InverseLerp(minX, maxX, x);
            float normalizedZ = Mathf.InverseLerp(minZ, maxZ, z);

            int pixelX = Mathf.RoundToInt(normalizedX * heightmap.width);
            int pixelZ = Mathf.RoundToInt(normalizedZ * heightmap.height);

            Color pixelColor = heightmap.GetPixel(pixelX, pixelZ);

            // Use the red channel (you may adjust this based on your heightmap format)
            float height = pixelColor.r * terrainHeightForce;

            return height;
        }

        public void SetTexture(Texture2D tex)
        {
            heightmap = tex;
        }
        public Texture2D GetTexture()
        {
            return heightmap;
        }
        public void SetTerrainHeightForce(float terrainHeightForce)
        {
            this.terrainHeightForce = terrainHeightForce;
        }
        public void SetMinMaxPosition(float[] bounds)
        {
            if (bounds.Length == 4)
            {
                minX = bounds[0];
                minZ = bounds[1];
                maxX = bounds[2];
                maxZ = bounds[3];
            }
        }
        public float GetTerrainHeightForce()
        {
            return terrainHeightForce;
        }
        public bool IsPositionInsideSquare(Vector3 position)
        {
            return position.x >= minX && position.x <= maxX && position.z >= minZ && position.z <= maxZ;
        }
        public bool IsXZInsideSquare(float x, float z)
        {
            return x >= minX && x <= maxX && z >= minZ && z <= maxZ;
        }
    }
}