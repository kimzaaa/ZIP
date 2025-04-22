using UnityEngine;

public class UIParallaxEffect : MonoBehaviour
{
    [SerializeField] private float parallaxStrength = 0.1f; // Controls the intensity of the parallax effect
    [SerializeField] private bool useMouseInput = true; // Toggle between mouse or camera-based parallax
    [SerializeField] private Camera targetCamera; // Reference to the main camera (optional for camera-based parallax)

    private Vector2 initialPosition; // Initial position of the UI element
    private RectTransform rectTransform; // Reference to the UI element's RectTransform

    void Start()
    {
        // Get the RectTransform component
        rectTransform = GetComponent<RectTransform>();

        // Store the initial position of the UI element
        initialPosition = rectTransform.anchoredPosition;

        // If no camera is assigned and camera-based parallax is selected, use the main camera
        if (!useMouseInput && targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void Update()
    {
        Vector2 offset;

        if (useMouseInput)
        {
            // Get the mouse position relative to the center of the screen
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            offset = (mousePos - screenCenter) * parallaxStrength / Screen.dpi;
        }
        else
        {
            // Use camera rotation or position for parallax (e.g., for 3D scenes)
            if (targetCamera != null)
            {
                Vector3 cameraForward = targetCamera.transform.forward;
                offset = new Vector2(cameraForward.x, cameraForward.y) * parallaxStrength * 100f;
            }
            else
            {
                offset = Vector2.zero;
            }
        }

        // Apply the parallax offset to the UI element's position
        rectTransform.anchoredPosition = initialPosition + offset;
    }
}