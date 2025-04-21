using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrass
{
    [CreateAssetMenu]
    public class TerrainDecorationSO : ScriptableObject
    {
        public Material proceduralMaterial;
        public ComputeShader computeShaderInitArgs;
        public ComputeShader computeShaderInit;
        public ComputeShader computeShaderUpdate;
        public ProceduralRenderer proceduralRendererPrefab;
        public ScriptableObject proceduralSettings;
        public float spacingAtMaxDensity = 0.2f;
        public float jitterAmount = 0.75f;
        public bool isProcedural;           //If you want to implement GPU Mesh Instances that arent procedural, you may use this.
        public GameObject decorationPrefab; //If you want to implement GPU Mesh Instances that arent procedural, you may use this.
    }
}