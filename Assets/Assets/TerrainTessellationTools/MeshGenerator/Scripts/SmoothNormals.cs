/*====================================================
*
* Francesco Cucchiara - 3POINT SOFT
* http://threepointsoft.altervista.org
*
=====================================================*/

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainTesselationTools
{
    public class SmoothNormals : MonoBehaviour
    {
        [Range(0, 180)] public float angle = 0;

        private MeshFilter[] mfs;
        private Mesh[] originalMeshes;
        private float _lastAngle = 0;

        private void Awake()
        {
            mfs = GetComponentsInChildren<MeshFilter>();
            originalMeshes = new Mesh[mfs.Length];
            for (int i = 0; i < mfs.Length; i++)
            {
                MeshFilter meshFilter = mfs[i];
                originalMeshes[i] = meshFilter.sharedMesh;
                Mesh mesh = new Mesh();
                //avoid issues if the smoothing algorithm produces more than 65k vertices
                mesh.indexFormat = IndexFormat.UInt32;
                //can't use the built-in Intantiate() because we must use a 32 bit mesh
                CopyMesh(meshFilter.sharedMesh, mesh);
                meshFilter.sharedMesh = mesh;
            }

            Smooth();
        }


        private void Start() // I changed this to Start, so it can be used in your project.
        {
            if (!Mathf.Approximately(_lastAngle, angle))
            {
                Smooth();
            }

            _lastAngle = angle;
        }

        private void Smooth()
        {
            foreach (MeshFilter meshFilter in mfs)
            {
                meshFilter.sharedMesh.RecalculateNormals(angle);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < mfs.Length; i++)
            {
                MeshFilter meshFilter = mfs[i];
                Destroy(meshFilter.sharedMesh);
                meshFilter.sharedMesh = originalMeshes[i];
            }
        }

        /// <summary>
        /// Copy source mesh values to destination mesh.
        /// </summary>
        /// <param name="source">The mesh from which to copy attributes.</param>
        /// <param name="destination">The destination mesh to copy attribute values to.</param>
        /// <exception cref="ArgumentNullException">Throws if source or destination is null.</exception>
        public static void CopyMesh(Mesh source, Mesh destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Vector3[] v = new Vector3[source.vertices.Length];
            int[][] t = new int[source.subMeshCount][];
            Vector2[] u = new Vector2[source.uv.Length];
            Vector2[] u2 = new Vector2[source.uv2.Length];
            Vector4[] tan = new Vector4[source.tangents.Length];
            Vector3[] n = new Vector3[source.normals.Length];
            Color32[] c = new Color32[source.colors32.Length];

            Array.Copy(source.vertices, v, v.Length);

            for (int i = 0; i < t.Length; i++)
                t[i] = source.GetTriangles(i);

            Array.Copy(source.uv, u, u.Length);
            Array.Copy(source.uv2, u2, u2.Length);
            Array.Copy(source.normals, n, n.Length);
            Array.Copy(source.tangents, tan, tan.Length);
            Array.Copy(source.colors32, c, c.Length);

            destination.Clear();
            destination.name = source.name;

            destination.vertices = v;

            destination.subMeshCount = t.Length;

            for (int i = 0; i < t.Length; i++)
                destination.SetTriangles(t[i], i);

            destination.uv = u;
            destination.uv2 = u2;
            destination.tangents = tan;
            destination.normals = n;
            destination.colors32 = c;
        }
    }

}