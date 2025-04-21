using System.Collections.Generic;
using UnityEngine;


namespace TerrainTesselationTools
{
    [ExecuteInEditMode()]
    public class PropertyBlockTextureSetter : MonoBehaviour
    {

        [SerializeField] private List<TextureInfo> textureInfos;

        private void Awake()
        {
            // Get the Renderer component attached to the GameObject
            Renderer renderer = GetComponent<Renderer>();

            // Check if the Renderer component exists
            if (renderer != null)
            {
                // Get the material of the Renderer
                Material material = renderer.sharedMaterial;

                // Check if the material has a shader with property blocks
                if (material != null && material.shader.isSupported)
                {
                    // Create a new MaterialPropertyBlock
                    MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(materialProperties);

                    // Set textures in the property block
                    foreach (TextureInfo info in textureInfos)
                    {
                        materialProperties.SetTexture(info.textureName, info.texture);
                    }

                    // Apply the property block to the material
                    renderer.SetPropertyBlock(materialProperties);
                }
                else
                {
                    Debug.LogError("Shader does not support property blocks or material is null.");
                }
            }
            else
            {
                Debug.LogError("Renderer component not found.");
            }
        }
        public void AddTextureInfo(TextureInfo info)
        {
            textureInfos.Add(info);
        }
        public void Refresh()
        {
            Awake();
        }
    }
}