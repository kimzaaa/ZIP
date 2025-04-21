using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(TerrainGridTiler))]
    public class MeshToTerrainTiles : MonoBehaviour
    {
        [SerializeField] private Material defaultTerrainMaterial;
        [SerializeField] private TerrainTile tilePrefab;
        [SerializeField] private string savePath = "Assets/TerrainTiles";
        [SerializeField][Range(0, 1)] private float smoothingNormalIntersectionFactor = 0.5f;
        [SerializeField] List<TextureInfo> TerrainDecorationMatTextures;
        [SerializeField] List<TextureInfo> TerrainDecorationMatTexturesSliced;
        [SerializeField][TextureDrawer] List<Texture2D> TerrainDecorationMasks;
        [SerializeField][TextureDrawer] List<Texture2D> TerrainDecorationExtraTextures;
        private List<Vector3[]> splitTriangles = new List<Vector3[]>();
        private Texture2D heightmapTexture;

        public void GenerateTerrainTiles()
        {
            TerrainGridTiler terrainGridTiler = GetComponent<TerrainGridTiler>();
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            heightmapTexture = GetComponent<BirdViewUVCalculator>().GetTexture();
            GetComponent<MeshRenderer>().enabled = false;
            Mesh mesh = meshFilter.sharedMesh;
            float tileSize = terrainGridTiler.GetTileSize();
            Vector2 centerOffset = terrainGridTiler.GetCenterOffset();
            Vector2 gridSize = terrainGridTiler.GetGridSize();
            Vector3 center = transform.position;

            int numTilesX = Mathf.FloorToInt(gridSize.x / tileSize);
            int numTilesY = Mathf.FloorToInt(gridSize.y / tileSize);

            if (heightmapTexture.width % numTilesX != 0 || heightmapTexture.height % numTilesY != 0)
            {
                Debug.LogWarning("Image Could not be sliced exactly, which can lead to inaccurate tiles,try using a image with height and width multiple of the number of tiles you are using (example: use a 256x256/512x512/etc image, for 2/4/8/etc number of tiles)");
            }
            if (tilePrefab.GetComponent<TerrainTileDecorated>() != null && TerrainDecorationMasks.Count < tilePrefab.GetComponent<TerrainTileDecorated>().GetTileDecorations().Count)
            {
                Debug.LogWarning("Not enough masks textures have been assigned");
            }
            if (tilePrefab.GetComponent<TerrainTileDecorated>() != null && TerrainDecorationMasks.Count > tilePrefab.GetComponent<TerrainTileDecorated>().GetTileDecorations().Count)
            {
                Debug.LogError("Too many masks textures have been assigned");
                return;
            }
            if ((TerrainDecorationMatTextures.Count > 0 || TerrainDecorationMatTexturesSliced.Count > 0) && tilePrefab.GetComponent<PropertyBlockTextureSetter>() == null)
            {
                Debug.LogWarning("PropertyBlockTextureSetter is not in the prefab. Textures wont be saved.");
            }

            float totalGridWidth = numTilesX * tileSize;
            float totalGridHeight = numTilesY * tileSize;

            Vector3 startPos = center - new Vector3(totalGridWidth * 0.5f, 0f, totalGridHeight * 0.5f);
            startPos += new Vector3(centerOffset.x, 0f, centerOffset.y);
            Vector3 tilePos = startPos;

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Dictionary<Mesh, int[]> listMeshOffset = new Dictionary<Mesh, int[]>();
            for (int x = 0; x < numTilesX; x++)
            {
                tilePos.x = startPos.x + x * tileSize;
                Mesh rowMesh = SliceMeshByCoordinate(mesh, tilePos + Vector3.right * tileSize, Vector3.right);
                rowMesh = SliceMeshByCoordinate(rowMesh, tilePos, Vector3.right * -1);
                for (int y = 0; y < numTilesY; y++)
                {
                    tilePos.z = startPos.z + y * tileSize;
                    Mesh tileMesh = SliceMeshByCoordinate(rowMesh, tilePos + Vector3.forward * tileSize, Vector3.forward);
                    tileMesh = SliceMeshByCoordinate(tileMesh, tilePos, Vector3.forward * -1);
                    listMeshOffset.Add(tileMesh, new int[] { x, y });
                }
            }
            List<Mesh> tilesList = new List<Mesh>(listMeshOffset.Keys);
            Mesh tilesMesh = MergeMeshes(tilesList);
            RecalculateNormals(tilesMesh);

            string folderPath = savePath + "/" + gameObject.name;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(savePath, gameObject.name);
            }

            foreach (var tilePair in listMeshOffset)
            {
                Mesh tileMesh = tilePair.Key;
                int x = tilePair.Value[0];
                int y = tilePair.Value[1];
                tilePos.x = startPos.x + x * tileSize;
                tilePos.z = startPos.z + y * tileSize;
                Vector3 tileOffset = -(tilePos + new Vector3(0.5f, 0, 0.5f) * tileSize - center);
                TransferNormals(tilesMesh, tileMesh);
                MoveMeshVertices(tileMesh, tileOffset);
                GenerateUVs(tileMesh);
                AssetDatabase.CreateAsset(tileMesh, folderPath + "/" + gameObject.name + x + "_" + y + ".asset");
                Texture2D heightmapSliced = SliceTexture(heightmapTexture, x, y, numTilesY, numTilesX, folderPath, "heightmap");
                TerrainTile newTile = Instantiate(tilePrefab, tilePos + new Vector3(0.5f, 0, 0.5f) * tileSize, Quaternion.identity);
                newTile.gameObject.name = "Tile:" + x + "_" + y;
                newTile.GetComponent<MeshFilter>().sharedMesh = tileMesh;
                newTile.GetComponent<MeshRenderer>().material = defaultTerrainMaterial;
                newTile.transform.parent = transform;
                newTile.GetComponent<BirdViewUVCalculator>().SetTexture(heightmapSliced);
                newTile.GetComponent<BirdViewUVCalculator>().SetTerrainHeightForce(GetComponent<BirdViewUVCalculator>().GetTerrainHeightForce());
                newTile.GetComponent<BirdViewUVCalculator>().CalculateBirdViewUVs();

                if (newTile.GetComponent<PropertyBlockTextureSetter>() != null)
                {
                    PropertyBlockTextureSetter propertyBlockTextureSetter = newTile.GetComponent<PropertyBlockTextureSetter>();
                    foreach (TextureInfo info in TerrainDecorationMatTextures)
                    {
                        propertyBlockTextureSetter.AddTextureInfo(info);
                    }
                    for (int index = 0; index < TerrainDecorationMatTexturesSliced.Count; index++)
                    {
                        Texture2D maskSliced = SliceTexture(TerrainDecorationMatTexturesSliced[index].texture, x, y, numTilesY, numTilesX, folderPath, "RGBCombinedmask" + index.ToString());
                        propertyBlockTextureSetter.AddTextureInfo(new TextureInfo(TerrainDecorationMatTexturesSliced[index].textureName, maskSliced));
                    }
                    propertyBlockTextureSetter.Refresh();
                }
                if (newTile.GetComponent<TerrainTileDecorated>() != null)
                {
                    TerrainTileDecorated tileDecorated = newTile.GetComponent<TerrainTileDecorated>();
                    for (int index = 0; index < TerrainDecorationMasks.Count; index++)
                    {
                        Texture2D maskSliced = SliceTexture(TerrainDecorationMasks[index], x, y, numTilesY, numTilesX, folderPath, "mask" + index.ToString());
                        tileDecorated.SetDecorationMask(index, maskSliced);
                    }
                    for (int index = 0; index < TerrainDecorationExtraTextures.Count; index++)
                    {
                        Texture2D extraDataSliced = SliceTexture(TerrainDecorationExtraTextures[index], x, y, numTilesY, numTilesX, folderPath, "extraData" + index.ToString());
                        tileDecorated.AddDecorationExtraData(extraDataSliced);
                    }
                    tileDecorated.GeneratePoints();
                }
            }
            AssetDatabase.CreateAsset(tilesMesh, folderPath + "/" + gameObject.name + ".asset");
            AssetDatabase.SaveAssets();
        }
        public Mesh MergeMeshes(List<Mesh> meshes)
        {
            CombineInstance[] combineInstances = new CombineInstance[meshes.Count];

            for (int i = 0; i < meshes.Count; i++)
            {
                combineInstances[i].mesh = meshes[i];
                combineInstances[i].transform = Matrix4x4.identity;
            }

            Mesh mergedMesh = new Mesh();
            mergedMesh.CombineMeshes(combineInstances, true, true);
            Vector3[] vertices = mergedMesh.vertices;
            int[] triangles = mergedMesh.triangles;

            List<Vector3> newVertices = new List<Vector3>();
            List<int> newTriangles = new List<int>();

            Dictionary<Vector3, int> uniqueVertices = new Dictionary<Vector3, int>();

            for (int i = 0; i < vertices.Length; i++)
            {
                if (!uniqueVertices.ContainsKey(vertices[i]))
                {
                    uniqueVertices.Add(vertices[i], newVertices.Count);
                    newVertices.Add(vertices[i]);
                }
            }
            for (int i = 0; i < triangles.Length; i++)
            {
                uniqueVertices.TryGetValue(vertices[triangles[i]], out int newIndex);
                newTriangles.Add(newIndex);
            }

            mergedMesh.triangles = newTriangles.ToArray();
            mergedMesh.vertices = newVertices.ToArray();

            return mergedMesh;
        }
        public Color CalculateMidValueColor(params Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return Color.black;
            }

            float avgR = 0f;
            float avgG = 0f;
            float avgB = 0f;

            foreach (Color color in colors)
            {
                avgR += color.r;
                avgG += color.g;
                avgB += color.b;
            }

            avgR /= colors.Length;
            avgG /= colors.Length;
            avgB /= colors.Length;

            return new Color(avgR, avgG, avgB);
        }
        private void GenerateUVs(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                // Map vertex position to heightmap coordinates
                float normalizedX = (vertex.x - mesh.bounds.min.x) / mesh.bounds.size.x;
                float normalizedZ = (vertex.z - mesh.bounds.min.z) / mesh.bounds.size.z;

                // Calculate UVs
                uvs[i] = new Vector2(normalizedX, normalizedZ);
            }
            mesh.uv = uvs;
        }
        private void MoveMeshVertices(Mesh mesh, Vector3 delta)
        {
            Vector3[] originalVertices = mesh.vertices;
            Vector3[] modifiedVertices = new Vector3[originalVertices.Length];

            for (int i = 0; i < originalVertices.Length; i++)
            {
                modifiedVertices[i] = originalVertices[i] + delta;
            }
            mesh.vertices = modifiedVertices;
            mesh.RecalculateBounds();
        }
        private void RecalculateNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3[] normals = new Vector3[vertices.Length];

            // Calculate face normals
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int vertIndexA = triangles[i];
                int vertIndexB = triangles[i + 1];
                int vertIndexC = triangles[i + 2];

                if (vertIndexA == -1 || vertIndexB == -1 || vertIndexC == -1)
                {
                    // triangle not found
                    continue;
                }

                Vector3 vertexA = vertices[vertIndexA];
                Vector3 vertexB = vertices[vertIndexB];
                Vector3 vertexC = vertices[vertIndexC];

                Vector3 faceNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

                normals[vertIndexA] += faceNormal;
                normals[vertIndexB] += faceNormal;
                normals[vertIndexC] += faceNormal;

            }
            foreach (Vector3[] triangle in splitTriangles)
            {
                Debug.LogWarning("Tiles Could not be sliced exactly, which can lead to inaccurate Normals, Tangents, and extra geometry.Try generating a Mesh with Tile edges and Mesh edges exactly matching eachother");
                int vertIndexA = Array.IndexOf(vertices, triangle[0]);
                int vertIndexB = Array.IndexOf(vertices, triangle[1]);
                int vertIndexC = Array.IndexOf(vertices, triangle[2]);
                if (vertIndexA == -1 || vertIndexB == -1 || vertIndexC == -1)
                {
                    // triangle not found
                    continue;
                }
                Vector3 averageNormal = (normals[vertIndexA] + normals[vertIndexB] + normals[vertIndexC]) / 3.0f;
                normals[vertIndexA] = Vector3.Lerp(normals[vertIndexA], averageNormal, smoothingNormalIntersectionFactor);
                normals[vertIndexB] = Vector3.Lerp(normals[vertIndexB], averageNormal, smoothingNormalIntersectionFactor);
                normals[vertIndexC] = Vector3.Lerp(normals[vertIndexC], averageNormal, smoothingNormalIntersectionFactor);
            }
            // Normalize vertex normals
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].normalized;
            }

            mesh.normals = normals;
        }
        void TransferNormals(Mesh sourceMesh, Mesh targetMesh)
        {
            Vector3[] sourceVertices = sourceMesh.vertices;
            Vector3[] targetVertices = targetMesh.vertices;
            Vector3[] sourceNormals = sourceMesh.normals;
            Vector3[] targetNormals = new Vector3[targetVertices.Length];

            for (int i = 0; i < targetVertices.Length; i++)
            {
                for (int j = 0; j < sourceVertices.Length; j++)
                {
                    if (Vector3.Distance(targetVertices[i], sourceVertices[j]) < 0.001f)
                    {
                        targetNormals[i] = sourceNormals[j];
                        break;
                    }
                }
            }

            targetMesh.normals = targetNormals;
        }
        private Mesh SliceMeshByCoordinate(Mesh originalMesh, Vector3 planePosition, Vector3 planeDirection) // Plane Direction Must be Vector3.right or Vector3.forward
        {
            // Preparation for triangles
            Mesh newMesh = new Mesh();
            Vector3[] vertices = originalMesh.vertices;
            int[] triangles = originalMesh.triangles;

            List<Vector3> newVertices = new List<Vector3>();
            List<int> newTriangles = new List<int>();

            Dictionary<int, VertexRelativePlane> vertexRelativePosDictionary = new Dictionary<int, VertexRelativePlane>();

            for (int i = 0; i < vertices.Length; i += 1)
            {
                Vector3 vertexWorldSpace = transform.TransformPoint(vertices[i]);
                vertices[i] = vertexWorldSpace;
                VertexRelativePlane relativePos = GetRelativeVertexPosition(vertexWorldSpace, planePosition, planeDirection);
                vertexRelativePosDictionary[i] = relativePos;
            }

            //Triangle Calculations
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int triangleVertexIndex1 = triangles[i];
                int triangleVertexIndex2 = triangles[i + 1];
                int triangleVertexIndex3 = triangles[i + 2];

                Vector3 vertexPos1 = vertices[triangleVertexIndex1];
                Vector3 vertexPos2 = vertices[triangleVertexIndex2];
                Vector3 vertexPos3 = vertices[triangleVertexIndex3];

                vertexRelativePosDictionary.TryGetValue(triangleVertexIndex1, out VertexRelativePlane relativePosIndex1);
                vertexRelativePosDictionary.TryGetValue(triangleVertexIndex2, out VertexRelativePlane relativePosIndex2);
                vertexRelativePosDictionary.TryGetValue(triangleVertexIndex3, out VertexRelativePlane relativePosIndex3);

                //Case 1 where splitting is not neccesary: No Triangle
                if (!(relativePosIndex1 == VertexRelativePlane.Below || relativePosIndex2 == VertexRelativePlane.Below || relativePosIndex3 == VertexRelativePlane.Below))      // this triangle doesnt have any vertex inside.
                {
                    continue;
                }

                //Case 2 where splitting is not neccesary: Triangle is the Same
                if (!(relativePosIndex1 == VertexRelativePlane.Above || relativePosIndex2 == VertexRelativePlane.Above || relativePosIndex3 == VertexRelativePlane.Above))       // this triangle has all vertex inside, no splitting neccesary
                {
                    AddTriangle(newVertices, newTriangles, vertexPos1, vertexPos2, vertexPos3, false);
                    continue;
                }

                // Spliting is neccesary
                // Calculate Intersections
                Vector3 intersection1_2 = Vector3.zero;
                Vector3 intersection2_3 = Vector3.zero;
                Vector3 intersection3_1 = Vector3.zero;
                Vector3 temp;
                bool intersection1_2Bool = false;
                bool intersection2_3Bool = false;
                bool intersection3_1Bool = false;

                if (relativePosIndex1 == VertexRelativePlane.Above || relativePosIndex1 == VertexRelativePlane.InThePlane)
                {
                    if (CalculateIntersection(vertexPos1, vertexPos2, planePosition, planeDirection, out temp))
                    {
                        intersection1_2 = temp;
                        intersection1_2Bool = true;
                    }
                    if (CalculateIntersection(vertexPos3, vertexPos1, planePosition, planeDirection, out temp))
                    {
                        intersection3_1 = temp;
                        intersection3_1Bool = true;
                    }
                }
                if (relativePosIndex2 == VertexRelativePlane.Above || relativePosIndex2 == VertexRelativePlane.InThePlane)
                {
                    if (CalculateIntersection(vertexPos1, vertexPos2, planePosition, planeDirection, out temp))
                    {
                        intersection1_2 = temp;
                        intersection1_2Bool = true;
                    }
                    if (CalculateIntersection(vertexPos2, vertexPos3, planePosition, planeDirection, out temp))
                    {
                        intersection2_3 = temp;
                        intersection2_3Bool = true;
                    }
                }
                if (relativePosIndex3 == VertexRelativePlane.Above || relativePosIndex3 == VertexRelativePlane.InThePlane)
                {
                    if (CalculateIntersection(vertexPos2, vertexPos3, planePosition, planeDirection, out temp))
                    {
                        intersection2_3 = temp;
                        intersection2_3Bool = true;
                    }
                    if (CalculateIntersection(vertexPos3, vertexPos1, planePosition, planeDirection, out temp))
                    {
                        intersection3_1 = temp;
                        intersection3_1Bool = true;
                    }
                }

                //Case 1 single vertex Below: Make same triangle, replacing outside vertex with the intersections in the plane
                if (relativePosIndex1 == VertexRelativePlane.Below && !(relativePosIndex2 == VertexRelativePlane.Below) && !(relativePosIndex3 == VertexRelativePlane.Below))
                {
                    // 1 is below
                    AddTriangle(newVertices, newTriangles, vertexPos1, intersection1_2, intersection3_1);
                    continue;
                }
                if (relativePosIndex2 == VertexRelativePlane.Below && !(relativePosIndex1 == VertexRelativePlane.Below) && !(relativePosIndex3 == VertexRelativePlane.Below))
                {
                    // 2 is below
                    AddTriangle(newVertices, newTriangles, intersection1_2, vertexPos2, intersection2_3);
                    continue;
                }
                if (relativePosIndex3 == VertexRelativePlane.Below && !(relativePosIndex1 == VertexRelativePlane.Below) && !(relativePosIndex2 == VertexRelativePlane.Below))
                {
                    // 3 is below
                    AddTriangle(newVertices, newTriangles, intersection3_1, intersection2_3, vertexPos3);
                    continue;
                }
                //Case 2 two vertex below: Make 2 triangles: triangle1 2 below vertex, 1 intersection. triangle 1 below vertex 2 intersections.
                if (intersection1_2Bool && intersection2_3Bool)
                {
                    AddTriangle(newVertices, newTriangles, vertexPos1, intersection1_2, vertexPos3);
                    AddTriangle(newVertices, newTriangles, intersection1_2, intersection2_3, vertexPos3);
                }
                if (intersection1_2Bool && intersection3_1Bool)
                {
                    AddTriangle(newVertices, newTriangles, intersection3_1, vertexPos2, vertexPos3);
                    AddTriangle(newVertices, newTriangles, intersection3_1, intersection1_2, vertexPos2);
                }
                if (intersection2_3Bool && intersection3_1Bool)
                {
                    AddTriangle(newVertices, newTriangles, vertexPos1, vertexPos2, intersection2_3);
                    AddTriangle(newVertices, newTriangles, vertexPos1, intersection2_3, intersection3_1);
                }
            }

            newMesh.vertices = newVertices.ToArray();
            newMesh.triangles = newTriangles.ToArray();
            return newMesh;
        }
        private VertexRelativePlane GetRelativeVertexPosition(Vector3 vertexWorldSpace, Vector3 planePosition, Vector3 planeDirection)
        {
            float planeCoordinate;
            float vertexCoordinate;
            bool reverse = false;

            if (planeDirection == Vector3.right)
            {
                planeCoordinate = planePosition.x;
                vertexCoordinate = vertexWorldSpace.x;
            }
            else if (planeDirection == Vector3.forward)
            {
                planeCoordinate = planePosition.z;
                vertexCoordinate = vertexWorldSpace.z;
            }
            else if (planeDirection == Vector3.right * -1)
            {
                planeCoordinate = planePosition.x;
                vertexCoordinate = vertexWorldSpace.x;
                reverse = true;
            }
            else if (planeDirection == Vector3.forward * -1)
            {
                planeCoordinate = planePosition.z;
                vertexCoordinate = vertexWorldSpace.z;
                reverse = true;
            }
            else
            {
                Debug.LogError("Slicing only works with Vector3.right or Vector3.forward or inverse");
                return VertexRelativePlane.Error;
            }
            if (reverse)
            {
                if (vertexCoordinate > planeCoordinate)
                {
                    return VertexRelativePlane.Below;
                }
                else if (vertexCoordinate < planeCoordinate)
                {
                    return VertexRelativePlane.Above;
                }
                else
                {
                    return VertexRelativePlane.InThePlane;
                }
            }
            else
            {
                if (vertexCoordinate < planeCoordinate)
                {
                    return VertexRelativePlane.Below;
                }
                else if (vertexCoordinate > planeCoordinate)
                {
                    return VertexRelativePlane.Above;
                }
                else
                {
                    return VertexRelativePlane.InThePlane;
                }
            }

        }
        private enum VertexRelativePlane
        {
            Below,          //Below is less than the Coordinate Plane Position Value
            InThePlane,     //In the plane is equal than the Coordinate Plane Position Value
            Above,          //Above is more of the Coordinate Plane Position Value
            Error
        }
        private bool CalculateIntersection(Vector3 vertexFirst, Vector3 vertexSecond, Vector3 planePosition, Vector3 planeDirection, out Vector3 intersection)
        {
            float planeCoordinate;
            intersection = Vector3.zero;
            if (planeDirection == Vector3.right || planeDirection == Vector3.right * -1)
            {
                planeCoordinate = planePosition.x;
                float deltaX = vertexSecond.x - vertexFirst.x;
                if (!Mathf.Approximately(deltaX, 0)) // this happens when vertex1.x == vertex2.x : Edge is parallel.
                {
                    float lerpFloat = (planeCoordinate - vertexFirst.x) / deltaX;
                    intersection = Vector3.Lerp(vertexFirst, vertexSecond, lerpFloat);
                    if (lerpFloat < 0 || lerpFloat > 1)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (planeDirection == Vector3.forward || planeDirection == Vector3.forward * -1)
            {
                planeCoordinate = planePosition.z;
                float deltaZ = vertexSecond.z - vertexFirst.z;
                if (!Mathf.Approximately(deltaZ, 0)) // this happens when vertex1.x == vertex2.x : Edge is parallel.
                {
                    float lerpFloat = (planeCoordinate - vertexFirst.z) / deltaZ;
                    intersection = Vector3.Lerp(vertexFirst, vertexSecond, lerpFloat);
                    if (lerpFloat < 0 || lerpFloat > 1)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Debug.LogError("Slicing only works with Vector3.right or Vector3.forward or inverse");
                return false;
            }
        }

        private void AddTriangle(List<Vector3> newVertices, List<int> newTriangles, Vector3 vertexPos1, Vector3 vertexPos2, Vector3 vertexPos3, bool splitTriangle = true)
        {
            int vertexIndex1;
            int vertexIndex2;
            int vertexIndex3;

            vertexPos1 = transform.InverseTransformPoint(vertexPos1);
            vertexPos2 = transform.InverseTransformPoint(vertexPos2);
            vertexPos3 = transform.InverseTransformPoint(vertexPos3);

            if (splitTriangle)
            {
                splitTriangles.Add(new Vector3[]
                {
                vertexPos1,vertexPos2,vertexPos3
                });
            }

            if (newVertices.Contains(vertexPos1))
            {
                vertexIndex1 = newVertices.IndexOf(vertexPos1);
            }
            else
            {
                vertexIndex1 = newVertices.Count;
                newVertices.Add(vertexPos1);
            }

            if (newVertices.Contains(vertexPos2))
            {
                vertexIndex2 = newVertices.IndexOf(vertexPos2);
            }
            else
            {
                vertexIndex2 = newVertices.Count;
                newVertices.Add(vertexPos2);
            }

            if (newVertices.Contains(vertexPos3))
            {
                vertexIndex3 = newVertices.IndexOf(vertexPos3);
            }
            else
            {
                vertexIndex3 = newVertices.Count;
                newVertices.Add(vertexPos3);
            }

            newTriangles.Add(vertexIndex1);
            newTriangles.Add(vertexIndex2);
            newTriangles.Add(vertexIndex3);
        }
        private Texture2D SliceTexture(Texture2D texture, int x, int y, int numTilesY, int numTilesX, string folderPath, string extraName)
        {

            int texWidth = Mathf.FloorToInt(texture.width / numTilesX);
            int texHeight = Mathf.FloorToInt(texture.height / numTilesY);
            Texture2D slicedTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBAFloat, false);
            Color[] pixels = texture.GetPixels(x * texWidth, y * texHeight, texWidth, texHeight);
            for (int xTex = 0; xTex < texWidth; xTex++) // borders averaging except corners
            {
                if (y != 0 && xTex != texWidth - 1 && xTex != 0)
                {
                    int pixelIndexBot = xTex;
                    pixels[pixelIndexBot] = Color.Lerp(pixels[pixelIndexBot], texture.GetPixel(x * texWidth + xTex, y * texHeight - 1), 0.5f);
                }
                if (y != numTilesY - 1 && xTex != texWidth - 1 && xTex != 0)
                {
                    int pixelIndexTop = pixels.Length - texWidth + xTex;
                    pixels[pixelIndexTop] = Color.Lerp(pixels[pixelIndexTop], texture.GetPixel(x * texWidth + xTex, y * texHeight + texHeight), 0.5f);
                }
            }
            for (int yTex = 0; yTex < texHeight; yTex++)
            {
                if (x != 0 && yTex != texHeight - 1 && yTex != 0)
                {
                    int pixelIndexLeft = yTex * texWidth;
                    pixels[pixelIndexLeft] = Color.Lerp(pixels[pixelIndexLeft], texture.GetPixel(x * texWidth - 1, y * texHeight + yTex), 0.5f);
                }
                if (x != numTilesX - 1 && yTex != texHeight - 1 && yTex != 0)
                {
                    int pixelIndexRight = (yTex + 1) * texWidth - 1;
                    pixels[pixelIndexRight] = Color.Lerp(pixels[pixelIndexRight], texture.GetPixel(x * texWidth + texWidth, y * texHeight + yTex), 0.5f);
                }
            }
            if (x != 0 && y != 0)
            {
                pixels[0] = CalculateMidValueColor(new Color[]
                {
                    pixels[0],                                                                      // bottom left corner
                    texture.GetPixel(x * texWidth, y * texHeight - 1),                              // one down pixel 
                    texture.GetPixel(x * texWidth - 1, y * texHeight),                              // one left pixel
                    texture.GetPixel(x * texWidth - 1, y * texHeight -1)                            // one both down and left pixel
                });
            }
            if (x != numTilesX - 1 && y != 0)
            {
                pixels[texWidth - 1] = CalculateMidValueColor(new Color[]
                {
                    pixels[texWidth - 1],                                                           // bottom right corner
                    texture.GetPixel(x * texWidth + texWidth, y * texHeight),                       // one right pixel 
                    texture.GetPixel(x * texWidth + texWidth - 1, y * texHeight -1),                // one down pixel
                    texture.GetPixel(x * texWidth + texWidth, y * texHeight -1)                     // one both right and down pixel
                });
            }
            if (x != 0 && y != numTilesY - 1)
            {
                int pixelIndexTopLeft = (texHeight - 1) * texWidth;
                pixels[pixelIndexTopLeft] = CalculateMidValueColor(new Color[]
                {
                    pixels[pixelIndexTopLeft],                                                      // top left corner
                    texture.GetPixel(x * texWidth - 1, y * texHeight + texHeight - 1),              // one left pixel 
                    texture.GetPixel(x * texWidth, y * texHeight + texHeight),                      // one up pixel
                    texture.GetPixel(x * texWidth - 1, y * texHeight + texHeight)                   // one both left and up pixel
                });
            }
            if (x != numTilesX - 1 && y != numTilesY - 1)
            {
                pixels[pixels.Length - 1] = CalculateMidValueColor(new Color[]
                {
                    pixels[pixels.Length - 1],                                                      // top right corner
                    texture.GetPixel(x * texWidth + texWidth -1, y * texHeight + texHeight),        // one up pixel 
                    texture.GetPixel(x * texWidth + texWidth, y * texHeight + texHeight -1),        // one right pixel
                    texture.GetPixel(x * texWidth + texWidth, y * texHeight + texHeight)            // one both up and right pixel
                });
            }

            slicedTexture.SetPixels(pixels);
            slicedTexture.wrapMode = TextureWrapMode.Mirror;
            slicedTexture.filterMode = FilterMode.Trilinear;
            slicedTexture.Apply();
            AssetDatabase.CreateAsset(slicedTexture, folderPath + "/" + gameObject.name + x + "_" + y + "_Texture_" + extraName + ".asset");
            Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folderPath + "/" + gameObject.name + x + "_" + y + "_Texture_" + extraName + ".asset");
            return loadedTexture;
        }

    }
    [Serializable]
    public class TextureInfo
    {
        public string textureName;
        [TextureDrawer(15)] public Texture2D texture;

        public TextureInfo(string textureName, Texture2D texture)
        {
            this.textureName = textureName;
            this.texture = texture;
        }
    }
}
