using UnityEditor;
using UnityEngine;

namespace ProceduralGrass
{
    [CustomEditor(typeof(TerrainDecorationSO))]
    public class TerrainDecorationSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TerrainDecorationSO terrainDecoration = (TerrainDecorationSO)target;
            EditorGUI.BeginChangeCheck();

            terrainDecoration.isProcedural = EditorGUILayout.Toggle("Procedural Decoration", terrainDecoration.isProcedural);
            EditorGUI.indentLevel++;
            if (terrainDecoration.isProcedural)
            {
                terrainDecoration.proceduralMaterial = (Material)EditorGUILayout.ObjectField("Procedural Material", terrainDecoration.proceduralMaterial, typeof(Material), false);
                terrainDecoration.computeShaderInitArgs = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader Init Render Args", terrainDecoration.computeShaderInitArgs, typeof(ComputeShader), false);
                terrainDecoration.computeShaderInit = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader Init", terrainDecoration.computeShaderInit, typeof(ComputeShader), false);
                terrainDecoration.computeShaderUpdate = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader Update", terrainDecoration.computeShaderUpdate, typeof(ComputeShader), false);
                terrainDecoration.proceduralSettings = (ScriptableObject)EditorGUILayout.ObjectField("Procedural Settings", terrainDecoration.proceduralSettings, typeof(ScriptableObject), false);
            }
            else
            {
                terrainDecoration.decorationPrefab = (GameObject)EditorGUILayout.ObjectField("Decoration Prefab", terrainDecoration.decorationPrefab, typeof(GameObject), false);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            terrainDecoration.spacingAtMaxDensity = EditorGUILayout.FloatField("Decoration Spacing At Max Density", terrainDecoration.spacingAtMaxDensity);
            terrainDecoration.jitterAmount = EditorGUILayout.Slider("Decoration Jitter", terrainDecoration.jitterAmount, 0, 1);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}