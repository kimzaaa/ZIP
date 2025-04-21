using UnityEngine;
using UnityEditor;
using System;

namespace TerrainTesselationTools
{
    public class CollapseCategoryDrawer : MaterialPropertyDrawer
    {
        private string category;
        private string index;
        private bool collapsed = true;
        public static event EventHandler<onCollapseEventArguments> onCollapse;
        public class onCollapseEventArguments
        {
            public string category;
            public bool collapsed = true;
            public onCollapseEventArguments(string category, bool collapsed)
            {
                this.category = category;
                this.collapsed = collapsed;
            }
        }
        public CollapseCategoryDrawer(string category)
        {
            this.category = category;
            index = "";
            onCollapse += CollapseCategoryDrawer_onCollapse;
        }

        public CollapseCategoryDrawer(string category, string index)
        {
            this.category = category;
            this.index = index;
            onCollapse += CollapseCategoryDrawer_onCollapse;
        }

        private void CollapseCategoryDrawer_onCollapse(object sender, onCollapseEventArguments e)
        {
            if (e.category == category)
            {
                collapsed = e.collapsed;
            }
        }
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (index == "First") return EditorGUIUtility.singleLineHeight;
            if (collapsed) return 0f;
            if (index == "Last") return EditorGUIUtility.singleLineHeight * 2;
            return EditorGUIUtility.singleLineHeight;
        }
        public float GetHeight()
        {
            if (collapsed) return 0f;
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            Rect headerPosition = position;
            headerPosition.height = EditorGUIUtility.singleLineHeight;

            // The position for the shader property inside the category.
            Rect propertyPosition = position;
            propertyPosition.y += GetHeight();
            propertyPosition.height = EditorGUIUtility.singleLineHeight;

            if (index == "First")
            {
                EditorGUI.BeginChangeCheck();
                GUIStyle bold = EditorStyles.foldout;
                bold.fontStyle = FontStyle.Bold;
                collapsed = EditorGUI.Foldout(headerPosition, collapsed, category, bold);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateCollapseCategory();
                }
            }

            // Only draw the shader property if the category is not collapsed.
            if (!collapsed)
            {
                EditorGUI.indentLevel++;
                editor.DefaultShaderProperty(propertyPosition, prop, label.text);
                EditorGUI.indentLevel--;
            }
        }
        private void UpdateCollapseCategory()
        {
            onCollapse?.Invoke(this, new onCollapseEventArguments(category, collapsed));
        }
    }
}