using UnityEngine;

public class UiMap : MonoBehaviour
{
    [Header("Image ที่จะเลื่อน (RectTransform)")]
    public RectTransform targetImage;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2f;

    private bool isDragging = false;
    private Vector2 lastMousePosition;

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && targetImage != null)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = currentMousePosition - lastMousePosition;

            targetImage.anchoredPosition += delta;
            lastMousePosition = currentMousePosition;
        }
    }

    void HandleZoom()
    {
        if (targetImage == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            float currentZoom = targetImage.localScale.x;
            float newZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);
            targetImage.localScale = new Vector3(newZoom, newZoom, 1f);
        }
    }
}
