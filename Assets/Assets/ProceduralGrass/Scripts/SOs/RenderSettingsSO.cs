using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrass
{
    [CreateAssetMenu]
    public class RenderSettingsSO : ScriptableObject
    {
        public float frustumTolerance = 0.1f;
        public float distanceCulling = 20f;
        public int distanceCullingNumber = 4;
        public float distanceCullingNone = 20f;
        private void OnValidate()
        {
            GrassRenderer.RefreshStatic(this);
        }
    }
}