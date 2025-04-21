using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrass
{
    [CreateAssetMenu]
    public class ProceduralGrassSettingsSO : ScriptableObject
    {
        public RenderSettingsSO renderSettings;
        public Gradient defaultColorsGradient;
        public Gradient secondaryColorsGradient;
        [Min(1)] public int grassBladesPerPoint = 3;
        [Min(1)] public int grassSegments = 3;
        [Min(0)] public float bladeInstanceRadius = 0.1f;
        [Range(0.01f, 1)] public float exponent = 0.6f;
        private void OnValidate()
        {
            GrassRenderer.RefreshStatic(this);
        }
    }
}