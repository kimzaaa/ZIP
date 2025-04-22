using UnityEngine;

public class UISwayWithSpeed : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; // Reference to the player GameObject
    [SerializeField] private Rigidbody playerRigidbody; // Player's Rigidbody to get velocity
    [SerializeField] private RectTransform[] uiElements; // Array of UI elements to sway
    [SerializeField] private float maxSpeed = 10f; // Maximum speed for scaling the effect
    [SerializeField] private float swayDistance = 100f; // Max distance UI moves outward (in UI units)
    [SerializeField] private float swaySpeed = 5f; // How fast the UI moves (smoothing)

    private Vector2[] originalPositions; // Store original positions of UI elements
    private Vector2 centerPoint; // Center of the screen in UI coordinates

    void Start()
    {
        // Initialize original positions array
        originalPositions = new Vector2[uiElements.Length];
        for (int i = 0; i < uiElements.Length; i++)
        {
            originalPositions[i] = uiElements[i].anchoredPosition;
        }

        // Calculate the center point of the Canvas (assuming Canvas is centered)
        centerPoint = new Vector2(0, 0); // For a Canvas with anchor at center
        // If your Canvas has a different setup, you may need to adjust this, e.g.:
        // centerPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    void Update()
    {
        // Calculate player speed (magnitude of velocity)
        float speed = playerRigidbody != null ? playerRigidbody.velocity.magnitude : 0f;
        // Normalize speed to a 0-1 range based on maxSpeed
        float speedFactor = Mathf.Clamp01(speed / maxSpeed);

        // Update each UI element's position
        for (int i = 0; i < uiElements.Length; i++)
        {
            RectTransform uiElement = uiElements[i];
            Vector2 originalPos = originalPositions[i];

            // Calculate direction from center toefficiently
            Vector2 direction = (originalPos - centerPoint).normalized;

            // Calculate target position: move outward from center based on speed
            Vector2 targetPos = originalPos + direction * swayDistance * speedFactor;

            // Smoothly interpolate to the target position
            uiElement.anchoredPosition = Vector2.Lerp(uiElement.anchoredPosition, targetPos, Time.deltaTime * swaySpeed);
        }
    }
}