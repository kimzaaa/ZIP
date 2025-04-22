using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    float deltaTime = 0.0f;
    private GUIStyle style;

    private void Awake()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperRight; 
        style.normal.textColor = Color.white;    
        style.fontSize = Mathf.RoundToInt(Screen.height * 0.035f); 
    }

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        int fps = Mathf.CeilToInt(1.0f / deltaTime);

        float margin = Screen.width * 0.02f; 
        Rect rect = new Rect(Screen.width - (150 + margin), margin, 150, 30);

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
        GUI.Label(shadowRect, $"FPS: {fps}", shadowStyle);

        GUI.Label(rect, $"FPS: {fps}", style);
    }
}