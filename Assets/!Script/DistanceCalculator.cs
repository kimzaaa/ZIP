using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class DistanceCalculator : MonoBehaviour
{
    public Image img;
    public Transform target;
    public TextMeshProUGUI meter;
    public Vector3 offset;
    public float maxDistance = 100f; // Optional: Maximum distance to show the UI

    private Transform player;
    private Camera mainCamera;

    private void Start()
    {
        // Find the Player object by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("No object with tag 'Player' found in the scene!");
        }

        // Cache the main camera
        mainCamera = Camera.main;

        // Ensure UI elements are enabled by default
        if (img != null) img.enabled = true;
        if (meter != null) meter.enabled = true;
    }

    private void Update()
    {
        if (player == null || target == null || mainCamera == null) return; // Skip if player, target, or camera is not set

        // Check if the target is visible to the camera
        bool isVisible = IsTargetVisible();

        // Enable or disable UI elements based on visibility
        if (img != null) img.enabled = isVisible;
        if (meter != null) meter.enabled = isVisible;

        // Skip further processing if not visible
        if (!isVisible) return;

        float minX = img.GetPixelAdjustedRect().width / 2;
        float maxX = Screen.width - minX;

        float minY = img.GetPixelAdjustedRect().height / 2;
        float maxY = Screen.height - minY;

        Vector2 pos = mainCamera.WorldToScreenPoint(target.position + offset);

        if (Vector3.Dot((target.position - player.position), player.forward) < 0)
        {
            if (pos.x < Screen.width / 2)
            {
                pos.x = maxX;
            }
            else
            {
                pos.x = minX;
            }
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        img.transform.position = pos;

        // Calculate distance between Player and target
        float distance = Vector3.Distance(target.position, player.position);
        meter.text = string.Format("{0:0}m", distance);
    }

    private bool IsTargetVisible()
    {
        // Check if target is within max distance (optional)
        float distance = Vector3.Distance(target.position, player.position);
        if (distance > maxDistance)
        {
            return false;
        }

        // Convert target position to viewport space (0 to 1 range)
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(target.position);

        // Check if the target is in front of the camera and within the viewport
        bool isInFront = viewportPoint.z > 0; // z > 0 means in front of camera
        bool isInViewport = viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                            viewportPoint.y >= 0 && viewportPoint.y <= 1;

        return isInFront && isInViewport;
    }
}