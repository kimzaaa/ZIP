using UnityEditor;
using UnityEngine;

namespace TerrainTesselationTools
{
    [CustomPropertyDrawer(typeof(TextureDrawerAttribute))]
    public class CustomTexture2DPropertyDrawer : PropertyDrawer
    {
        private const float PreviewSize = 64f; // Adjust the size as needed

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            TextureDrawerAttribute drawerAttribute = (TextureDrawerAttribute)attribute;
            float customOffset = drawerAttribute.customOffset;

            Rect texturePosition = new Rect();
            if (customOffset >= 0)
            {
                texturePosition = new Rect(position.x - customOffset, position.y, PreviewSize + customOffset, PreviewSize);
            }
            else
            {
                texturePosition = new Rect(position.x - customOffset, position.y, PreviewSize, PreviewSize);
            }
            // Draw the custom texture field
            EditorGUI.BeginChangeCheck();
            Texture2D selectedTexture = property.objectReferenceValue as Texture2D;
            selectedTexture = EditorGUI.ObjectField(texturePosition, selectedTexture, typeof(Texture2D), false) as Texture2D;

            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = selectedTexture;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return PreviewSize + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}