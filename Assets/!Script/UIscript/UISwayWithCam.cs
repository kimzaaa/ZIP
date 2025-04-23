using UnityEngine;

public class UISwayWithCamera : MonoBehaviour
{
    public Transform cameraTransform; // Reference to the main camera's transform
    public float swayAmount = 50f;    // Max UI offset in pixels (adjust for intensity)
    public float swaySpeed = 5f;      // How quickly the UI returns to original position
    private Vector2 initialPosition;  // Initial anchored position of the UI
    private float lastCameraYaw;      // Previous camera yaw for rotation delta
    private float lastCameraPitch;    // Previous camera pitch for rotation delta
    private Vector2 currentSway;      // Current sway offset

    void Start()
    {
        // Store the initial anchored position of the UI
        initialPosition = GetComponent<RectTransform>().anchoredPosition;

        // Automatically assign the main camera if not set
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Initialize yaw and pitch with current camera rotation
        lastCameraYaw = cameraTransform.eulerAngles.y;
        lastCameraPitch = cameraTransform.eulerAngles.x;
    }

    void Update()
    {
        // Get the current camera yaw (Y rotation) and pitch (X rotation)
        float currentCameraYaw = cameraTransform.eulerAngles.y;
        float currentCameraPitch = cameraTransform.eulerAngles.x;

        // Calculate the change in yaw and pitch (rotation deltas)
        float yawDelta = Mathf.DeltaAngle(lastCameraYaw, currentCameraYaw);
        float pitchDelta = Mathf.DeltaAngle(lastCameraPitch, currentCameraPitch);

        // Calculate sway based on camera rotation speed (opposite direction)
        float swayX = -yawDelta * swayAmount * Time.deltaTime;   // Left/right sway
        float swayY = -pitchDelta * swayAmount * Time.deltaTime; // Up/down sway

        // Apply the sway to the current offset
        currentSway += new Vector2(swayX, swayY);

        // Dampen the sway to return to the original position
        currentSway = Vector2.Lerp(currentSway, Vector2.zero, swaySpeed * Time.deltaTime);

        // Update the UI's anchored position
        GetComponent<RectTransform>().anchoredPosition = initialPosition + currentSway;

        // Store the current yaw and pitch for the next frame
        lastCameraYaw = currentCameraYaw;
        lastCameraPitch = currentCameraPitch;
    }
}