using UnityEngine;
using TMPro; // If using TextMeshPro

public class TextZoom : MonoBehaviour
{
    public float minSize = 20f; // Minimum font size
    public float maxSize = 40f; // Maximum font size
    public float zoomSpeed = 2f; // Speed of zoom effect

    private TextMeshProUGUI tmpText; // For TextMeshPro
    private float targetSize;
    private bool isZoomingIn = true;

    void Start()
    {
        // Get the Text component
        tmpText = GetComponent<TextMeshProUGUI>();
        targetSize = minSize;
    }

    void Update()
    {
        // Smoothly interpolate font size
        float currentSize = tmpText.fontSize; // For TextMeshPro
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * zoomSpeed);

        // Update font size
        tmpText.fontSize = currentSize; // For TextMeshPro

        // Switch between zoom in and out
        if (Mathf.Abs(currentSize - targetSize) < 0.1f)
        {
            isZoomingIn = !isZoomingIn;
            targetSize = isZoomingIn ? maxSize : minSize;
        }
    }
}