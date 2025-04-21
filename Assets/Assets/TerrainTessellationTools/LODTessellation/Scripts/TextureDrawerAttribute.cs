using UnityEngine;

namespace TerrainTesselationTools
{
    public class TextureDrawerAttribute : PropertyAttribute
    {
        public float customOffset;

        public TextureDrawerAttribute(float customOffset)
        {
            this.customOffset = customOffset;
        }
        public TextureDrawerAttribute()
        {
            customOffset = 0;
        }
    }
}