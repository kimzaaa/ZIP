using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralGrass
{
    [ExecuteInEditMode]
    public class GrassInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;
        [SerializeField][Range(0.01f, 3)] private float force = 1f;
        private int id;
        private void OnEnable()
        {
            id = GrassInteractorManager.CreatePoint();
        }
        private void Update()
        {
            GrassInteractorManager.Update(id, new GrassInteractorPoint(transform.position, radius, force));
        }
        private void OnDisable()
        {
            GrassInteractorManager.Update(id, new GrassInteractorPoint(Vector3.zero, 0, 0));
        }
    }

    [InitializeOnLoad]
    public class GrassInteractorManager
    {
        private static Dictionary<int, GrassInteractorPoint> interactors;
        private static GraphicsBuffer interactorsBuffer;
        private static int maxInteractors = 100;

        static GrassInteractorManager()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void PlayModeStateChanged(PlayModeStateChange obj)
        {
            if(obj == PlayModeStateChange.ExitingEditMode || obj == PlayModeStateChange.ExitingPlayMode)
            {
                ReleaseBuffer();
            }
        }
        private static void OnBeforeAssemblyReload()
        {
            ReleaseBuffer();
        }
        private static void ReleaseBuffer()
        {
            interactorsBuffer?.Dispose();
            interactorsBuffer = null;
        }
        public static GraphicsBuffer GetBuffer()
        {
            if (interactorsBuffer != null)
            {
                return interactorsBuffer;
            }
            return InitBuffer();
        }
        private static GraphicsBuffer InitBuffer()
        {
            interactorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxInteractors, sizeof(float) * 5);
            return interactorsBuffer;
        }
        public static void Update(int id, GrassInteractorPoint grassInteractorPoint)
        {
            if (interactors == null || !interactors.ContainsKey(id))
            {
                Debug.LogError($"GrassInteractor with ID {id} not found.");
                return;
            }

            interactors[id] = grassInteractorPoint;

            // Now update the ComputeBuffer if needed
            if (interactorsBuffer != null)
            {
                interactorsBuffer.SetData(new List<GrassInteractorPoint>(interactors.Values));
            }
            if (grassInteractorPoint.radius == 0)
            {
                DestroyPoint(id);
            }
        }

        public static void DestroyPoint(int id)
        {
            if (interactors == null || !interactors.ContainsKey(id))
            {
                Debug.LogError($"GrassInteractor with ID {id} not found.");
                return;
            }
            interactors.Remove(id);

            // Update the ComputeBuffer if there are remaining points
            interactorsBuffer?.SetData(new List<GrassInteractorPoint>(interactors.Values));

        }

        public static int CreatePoint()
        {
            if (interactors == null)
            {
                interactors = new Dictionary<int, GrassInteractorPoint>();
            }

            int newId = GetUniqueId();
            interactors.Add(newId, new GrassInteractorPoint(Vector3.zero, 0f, 0f));

            if (interactorsBuffer == null)
            {
                InitBuffer();
            }
            interactorsBuffer.SetData(new List<GrassInteractorPoint>(interactors.Values));

            return newId;
        }

        private static int GetUniqueId()
        {
            for (int i = 1; i < maxInteractors + 1; i++)
            {
                if (!interactors.ContainsKey(i))
                {
                    return i;
                }
            }
            return 0;
        }
        public static int GetMaxInteractors()
        {
            return maxInteractors;
        }
    }
    public struct GrassInteractorPoint
    {
        public Vector3 position;
        public float radius;
        public float force;

        public GrassInteractorPoint(Vector3 position, float radius, float force)
        {
            this.position = position;
            this.radius = radius;
            this.force = force;
        }
    }
}
