using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class TessellationLitShader : BaseShaderGUI
    {
        static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties litProperties;
        private TessellationLitDetailGUI.LitProperties litDetailProperties;
        private LitTessellationProperties litTessellationProperties;
        private CombinedTexturesProperties combinedTexturesProperties;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(TessellationLitDetailGUI.Styles.detailInputs, Expandable.Details, _ => TessellationLitDetailGUI.DoDetailArea(litDetailProperties, materialEditor));
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new TessellationLitDetailGUI.LitProperties(properties);
            litTessellationProperties = new LitTessellationProperties(properties);
            combinedTexturesProperties = new CombinedTexturesProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, TessellationLitDetailGUI.SetMaterialKeywords);
        }
        public void DrawTessellation()
        {
            GUILayout.Space(20);
            bool linearTessellation = litTessellationProperties._LinearTessellation.floatValue != 0;
            materialEditor.ShaderProperty(litTessellationProperties._Tolerance, TessellationStyles.tolerance);
            materialEditor.ShaderProperty(litTessellationProperties._LinearTessellation, TessellationStyles.linearTessellation);

            if (linearTessellation)
            {
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors1, TessellationStyles.tessellationFactorsMin);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors6, TessellationStyles.tessellationFactorsMax);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera1, TessellationStyles.distanceToCameraMin);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera5, TessellationStyles.distanceToCameraMax);
            }
            else
            {
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors1, TessellationStyles.tessellationFactors1);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors2, TessellationStyles.tessellationFactors2);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors3, TessellationStyles.tessellationFactors3);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors4, TessellationStyles.tessellationFactors4);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors5, TessellationStyles.tessellationFactors5);
                materialEditor.ShaderProperty(litTessellationProperties._TessellationFactors6, TessellationStyles.tessellationFactors6);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera1, TessellationStyles.distanceToCamera1);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera2, TessellationStyles.distanceToCamera2);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera3, TessellationStyles.distanceToCamera3);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera4, TessellationStyles.distanceToCamera4);
                materialEditor.ShaderProperty(litTessellationProperties._DistanceToCamera5, TessellationStyles.distanceToCamera5);
            }
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);

            base.DrawSurfaceOptions(material);
            DrawTessellation();
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            if (combinedTexturesProperties._TextureMaskRGB != null)
            {
                EditorGUI.indentLevel--;
                materialEditor.ShaderProperty(combinedTexturesProperties._TextureMaskRGB, CombinedTexturesPropertiesStyles.textureMaskRGB);
                materialEditor.ShaderProperty(combinedTexturesProperties._TextureRed, CombinedTexturesPropertiesStyles.textureRed);
                materialEditor.ShaderProperty(combinedTexturesProperties._TextureGreen, CombinedTexturesPropertiesStyles.textureGreen);
                materialEditor.ShaderProperty(combinedTexturesProperties._TextureBlue, CombinedTexturesPropertiesStyles.textureBlue);
                EditorGUI.indentLevel++;
                GUILayout.Space(20);
            }
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
        }
        public struct LitTessellationProperties
        {
            public MaterialProperty _Tolerance;
            public MaterialProperty _LinearTessellation;
            public MaterialProperty _TessellationFactors1;
            public MaterialProperty _TessellationFactors2;
            public MaterialProperty _TessellationFactors3;
            public MaterialProperty _TessellationFactors4;
            public MaterialProperty _TessellationFactors5;
            public MaterialProperty _TessellationFactors6;
            public MaterialProperty _DistanceToCamera1;
            public MaterialProperty _DistanceToCamera2;
            public MaterialProperty _DistanceToCamera3;
            public MaterialProperty _DistanceToCamera4;
            public MaterialProperty _DistanceToCamera5;
            public LitTessellationProperties(MaterialProperty[] properties)
            {
                _Tolerance = FindProperty("_Tolerance", properties, false);
                _LinearTessellation = FindProperty("_LinearTessellation", properties, false);
                _TessellationFactors1 = FindProperty("_TessellationFactors1", properties, false);
                _TessellationFactors2 = FindProperty("_TessellationFactors2", properties, false);
                _TessellationFactors3 = FindProperty("_TessellationFactors3", properties, false);
                _TessellationFactors4 = FindProperty("_TessellationFactors4", properties, false);
                _TessellationFactors5 = FindProperty("_TessellationFactors5", properties, false);
                _TessellationFactors6 = FindProperty("_TessellationFactors6", properties, false);
                _DistanceToCamera1 = FindProperty("_DistanceToCamera1", properties, false);
                _DistanceToCamera2 = FindProperty("_DistanceToCamera2", properties, false);
                _DistanceToCamera3 = FindProperty("_DistanceToCamera3", properties, false);
                _DistanceToCamera4 = FindProperty("_DistanceToCamera4", properties, false);
                _DistanceToCamera5 = FindProperty("_DistanceToCamera5", properties, false);
            }
        }
        public class TessellationStyles
        {
            public static GUIContent tolerance = EditorGUIUtility.TrTextContent("Tolerance", "Set the tolerance for occlusion culling.");
            public static GUIContent linearTessellation = EditorGUIUtility.TrTextContent("Linear Tessellation", "Enable linear tessellation.");
            public static GUIContent tessellationFactors1 = EditorGUIUtility.TrTextContent("Tessellation Factors 1", "Set tessellation factors 1.");
            public static GUIContent tessellationFactors2 = EditorGUIUtility.TrTextContent("Tessellation Factors 2", "Set tessellation factors 2.");
            public static GUIContent tessellationFactors3 = EditorGUIUtility.TrTextContent("Tessellation Factors 3", "Set tessellation factors 3.");
            public static GUIContent tessellationFactors4 = EditorGUIUtility.TrTextContent("Tessellation Factors 4", "Set tessellation factors 4.");
            public static GUIContent tessellationFactors5 = EditorGUIUtility.TrTextContent("Tessellation Factors 5", "Set tessellation factors 5.");
            public static GUIContent tessellationFactors6 = EditorGUIUtility.TrTextContent("Tessellation Factors 6", "Set tessellation factors 6.");
            public static GUIContent distanceToCamera1 = EditorGUIUtility.TrTextContent("Distance To Camera 1", "Set distance to camera 1.");
            public static GUIContent distanceToCamera2 = EditorGUIUtility.TrTextContent("Distance To Camera 2", "Set distance to camera 2.");
            public static GUIContent distanceToCamera3 = EditorGUIUtility.TrTextContent("Distance To Camera 3", "Set distance to camera 3.");
            public static GUIContent distanceToCamera4 = EditorGUIUtility.TrTextContent("Distance To Camera 4", "Set distance to camera 4.");
            public static GUIContent distanceToCamera5 = EditorGUIUtility.TrTextContent("Distance To Camera 5", "Set distance to camera 5.");
            public static GUIContent distanceToCameraMin = EditorGUIUtility.TrTextContent("Distance To Camera Minimum", "Set distance to camera minimum.");
            public static GUIContent distanceToCameraMax = EditorGUIUtility.TrTextContent("Distance To Camera Maximum", "Set distance to camera maximum.");
            public static GUIContent tessellationFactorsMin = EditorGUIUtility.TrTextContent("Tessellation Factors Minimum", "Set tessellation minimum factors.");
            public static GUIContent tessellationFactorsMax = EditorGUIUtility.TrTextContent("Tessellation Factors Maximum", "Set tessellation maximum factors.");
        }

        public struct CombinedTexturesProperties
        {
            public MaterialProperty _TextureRed;
            public MaterialProperty _TextureGreen;
            public MaterialProperty _TextureBlue;
            public MaterialProperty _TextureMaskRGB;
            public CombinedTexturesProperties(MaterialProperty[] properties)
            {
                _TextureRed = FindProperty("_TextureRed", properties, false);
                _TextureGreen = FindProperty("_TextureGreen", properties, false);
                _TextureBlue = FindProperty("_TextureBlue", properties, false);
                _TextureMaskRGB = FindProperty("_TextureMaskRGB", properties, false);
            }
        }
        public class CombinedTexturesPropertiesStyles
        {
            public static GUIContent textureRed = EditorGUIUtility.TrTextContent("Texture Red", "Set the Texture at Red");
            public static GUIContent textureGreen = EditorGUIUtility.TrTextContent("Texture Green", "Set the Texture at Green");
            public static GUIContent textureBlue = EditorGUIUtility.TrTextContent("Texture Blue", "Set the Texture at Blue");
            public static GUIContent textureMaskRGB = EditorGUIUtility.TrTextContent("Texture Mask RGB", "Set the RGB Mask for Textures");
        }
    }
}
