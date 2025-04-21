using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using TerrainTesselationTools;

namespace ProceduralGrass
{
    [CustomEditor(typeof(TerrainTileDecorated))]
    public class TerrainTileEditor : Editor
    {
        private bool isBrushMode = false;
        private bool isPointMode = false;
        private bool isMouseHeld = false;
        private bool isMouseRightHeld = false;
        private float brushSize = 1f;
        private float brushDensity = 0.5f;
        private TerrainTileDecorated selectedTerrainTile;
        private TerrainTileDecoration selectedTerrainDecoration;
        private ProceduralRenderer selectedRenderer;
        private int selectedDecorationIndex = 0;
        private BirdViewUVCalculator birdViewUVCalculator;
        private float lastRefreshTime;
        private float refreshCooldown = 0.2f;
        private int clumpSecondaryIDGrass = 1;
        private float clumpBaseToSecondaryRatio = 0;

        private void OnEnable()
        {
            selectedTerrainTile = (TerrainTileDecorated)target;
            birdViewUVCalculator = selectedTerrainTile.gameObject.GetComponent<BirdViewUVCalculator>();
        }
        private void OnSceneGUI() // For the brush to be seen nicely, activate Allways refresh in scene view, right after the mute button, in the dropdown.
        {
            if (isBrushMode)
            {
                Tools.hidden = true;
                Selection.activeGameObject = selectedTerrainTile.gameObject;
                Event currentEvent = Event.current;
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                var eventType = currentEvent.GetTypeForControl(controlID);
                if (isPointMode)
                {
                    Handles.color = Color.cyan;
                    Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    RaycastHit hit;
                    Vector3 mousePosition;
                    if (Physics.Raycast(ray, out hit))
                    {
                        mousePosition = hit.point;
                    }
                    else
                    {
                        Vector3 rayDirection = ray.direction;
                        rayDirection.y = 0;
                        float t = -ray.origin.y / ray.direction.y;
                        float xAtY0 = ray.origin.x + t * ray.direction.x;
                        float zAtY0 = ray.origin.z + t * ray.direction.z;
                        mousePosition = new Vector3(xAtY0, 0, zAtY0);
                    }
                    Handles.DrawLine(mousePosition, mousePosition + Vector3.up, 4);
                    if (eventType == EventType.MouseDown && currentEvent.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                        selectedRenderer.AddPoint(birdViewUVCalculator, mousePosition.x, mousePosition.z, new float[] { clumpSecondaryIDGrass, clumpBaseToSecondaryRatio });
                        selectedRenderer.Refresh();
                        EditorUtility.SetDirty(selectedRenderer);
                    }
                }
                else
                {
                    Handles.color = Color.cyan;
                    float halfBrushSize = brushSize / 2;
                    Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    RaycastHit hit;
                    Vector3 mousePosition;
                    if (Physics.Raycast(ray, out hit))
                    {
                        mousePosition = hit.point;
                        Handles.DrawWireDisc(mousePosition, Vector3.up, halfBrushSize, 3);
                    }
                    else
                    {
                        Vector3 rayDirection = ray.direction;
                        rayDirection.y = 0;
                        float t = -ray.origin.y / ray.direction.y;
                        float xAtY0 = ray.origin.x + t * ray.direction.x;
                        float zAtY0 = ray.origin.z + t * ray.direction.z;
                        mousePosition = new Vector3(xAtY0, 0, zAtY0);
                        Handles.DrawWireDisc(mousePosition, Vector3.up, halfBrushSize, 3);
                    }

                    Handles.color = new Color(0, 1, 1, 0.1f);
                    Handles.DrawSolidArc(mousePosition, Vector3.up, Vector3.forward, 360, halfBrushSize * Mathf.Clamp01(brushDensity));
                    Handles.color = Color.cyan;
                    Handles.DrawLine(mousePosition + Vector3.left * halfBrushSize, mousePosition + Vector3.right * halfBrushSize, 2);
                    Handles.DrawLine(mousePosition + Vector3.forward * halfBrushSize, mousePosition + Vector3.back * halfBrushSize, 2);
                    if (eventType == EventType.MouseDown && currentEvent.button == 0)
                    {
                        isMouseHeld = true;
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    if (eventType == EventType.MouseUp && currentEvent.button == 0)
                    {
                        isMouseHeld = false;
                        GUIUtility.hotControl = controlID;
                        currentEvent.Use();
                        selectedRenderer.Refresh();
                    }
                    if (eventType == EventType.MouseDown && currentEvent.button == 1)
                    {
                        isMouseRightHeld = true;
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    if (eventType == EventType.MouseUp && currentEvent.button == 1)
                    {
                        isMouseRightHeld = false;
                        GUIUtility.hotControl = controlID;
                        currentEvent.Use();
                        selectedRenderer.Refresh();
                    }
                    if (isMouseHeld)
                    {
                        for (int i = 0; i < brushDensity; i++)
                        {
                            // Get a random point within the circle
                            Vector2 randomPoint = Random.insideUnitCircle * halfBrushSize;

                            // Add the random point to the mouse position
                            Vector3 randomOffset = new Vector3(randomPoint.x, 0, randomPoint.y);
                            Vector3 finalPosition = mousePosition + randomOffset;

                            // Add the point to your renderer
                            selectedRenderer.AddPoint(birdViewUVCalculator, finalPosition.x, finalPosition.z, new float[] { clumpSecondaryIDGrass, clumpBaseToSecondaryRatio });
                        }
                        // Mark the renderer as dirty and refresh
                        EditorUtility.SetDirty(selectedRenderer);
                        if (Time.realtimeSinceStartup - lastRefreshTime >= refreshCooldown)
                        {
                            // Refresh the renderer
                            selectedRenderer.Refresh();

                            // Update the last refresh time
                            lastRefreshTime = Time.realtimeSinceStartup;
                        }
                    }
                    if (isMouseRightHeld)
                    {
                        selectedRenderer.RemovePoints(birdViewUVCalculator, mousePosition.x, mousePosition.z, halfBrushSize);
                        // Mark the renderer as dirty and refresh
                        EditorUtility.SetDirty(selectedRenderer);
                        if (Time.realtimeSinceStartup - lastRefreshTime >= refreshCooldown)
                        {
                            // Refresh the renderer
                            selectedRenderer.Refresh();

                            // Update the last refresh time
                            lastRefreshTime = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            else
            {
                Tools.hidden = false;
            }
        }
        public override void OnInspectorGUI()
        {
            if (!isBrushMode)
            {
                base.OnInspectorGUI();
                TerrainTileDecorated terrainTile = selectedTerrainTile;

                if (GUILayout.Button("Generate Points"))
                {
                    terrainTile.GeneratePoints();
                }
                GUILayout.Space(20);
            }

            // Toggle brush mode with a button
            if (GUILayout.Button("Toggle Brush Mode"))
            {
                isBrushMode = !isBrushMode;
                selectedTerrainDecoration = null; // Reset selected decoration when toggling modes
            }

            // If brush mode is active, show brush options
            if (isBrushMode)
            {
                GUILayout.Space(10);
                GUILayout.Label("Select Procedural Renderer:");
                EditorGUI.indentLevel++;

                // Create an array of decoration names with index for the dropdown
                string[] decorationNames = selectedTerrainTile.GetTileDecorations()
                    .Select((decoration, index) => $"{decoration.GetTerrainDecorationSO().name} ({index})")
                    .ToArray();

                // Show a dropdown to select the decoration
                selectedDecorationIndex = EditorGUILayout.Popup("Select Decoration", selectedDecorationIndex, decorationNames);

                // Get the selected decoration based on the index
                selectedTerrainDecoration = selectedTerrainTile.GetTileDecorations()[selectedDecorationIndex];

                // Show the selected decoration's properties
                EditorGUI.indentLevel++;
                if (selectedTerrainDecoration != null)
                {
                    selectedRenderer = EditorGUILayout.ObjectField("(Renderer)", selectedTerrainDecoration.GetTerrainDecorationRenderer(), typeof(ProceduralRenderer), true) as ProceduralRenderer;
                }
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
                GUILayout.Space(10);
                brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 25.0f);
                brushDensity = EditorGUILayout.Slider("Brush Density", brushDensity, 1f, 75.0f);
                isPointMode = EditorGUILayout.Toggle("Single Point Mode", isPointMode);
                if (selectedTerrainDecoration.GetTerrainDecorationRenderer() as GrassRenderer != null)
                {
                    GUILayout.Label("Select Clump Properties:");
                    EditorGUI.indentLevel++;
                    GrassRenderer grassRenderer = (GrassRenderer)selectedTerrainDecoration.GetTerrainDecorationRenderer();
                    string[] clumpNames = grassRenderer.GetClumps()
                    .Select((clump, index) => index == 0 ? "Clump Base Properties + Secondary Color" : $"Clump {clump.position} ({index})")
                    .ToArray();
                    clumpSecondaryIDGrass = EditorGUILayout.Popup("Select Secondary Clump ID", clumpSecondaryIDGrass, clumpNames);
                    clumpBaseToSecondaryRatio = EditorGUILayout.Slider("Clump Ratio", clumpBaseToSecondaryRatio, 0f, 1.0f);
                }
                GUILayout.Space(10);
            }
        }
    }
}