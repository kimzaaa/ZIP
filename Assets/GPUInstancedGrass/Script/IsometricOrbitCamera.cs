using UnityEngine;

public class IsometricOrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target; 
    [SerializeField] private Transform floor; 
    [SerializeField] private float minDistance = 5f; 
    [SerializeField] private float zoomSpeed = 10f; 
    [SerializeField] private float rotationSpeed = 100f; 
    [SerializeField] private float zoomSmoothTime = 0.2f; 
    [SerializeField] private float transitionDistance = 15f; 
    [SerializeField] private float orthographicSize = 5f;
    [SerializeField] private float touchRotationSensitivity = 0.5f;
    [SerializeField] private float touchZoomSensitivity = 0.01f;

    private Camera cam;
    private float currentAngle = 45f;
    private Vector3 isometricAngle = new Vector3(45f, 45f, 0f);
    private Vector3 topDownAngle = new Vector3(90f, 0f, 0f);
    private float currentDistance;
    private float targetDistance;
    private float zoomVelocity = 0f; 
    private float maxDistance; 
    private float perspectiveFOV; 

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not set for IsometricOrbitCamera!");
            return;
        }

        if (floor == null)
        {
            Debug.LogError("Floor not set for IsometricOrbitCamera! Please assign a floor object.");
            return;
        }

        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("No Camera component found on this GameObject!");
            return;
        }
        CalculateMapViewParameters();

        currentDistance = minDistance; 
        targetDistance = minDistance;

        UpdateCameraMode();
        UpdateCameraPosition();
    }

    void Update()
    {
        if (target == null || floor == null || cam == null) return;

        HandleTouchInput();

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        UpdateCameraMode();
        UpdateCameraPosition();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1) 
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float rotationInput = touch.deltaPosition.x * touchRotationSensitivity * rotationSpeed * Time.deltaTime;
                currentAngle += rotationInput;
            }
        }
        else if (Input.touchCount == 2) 
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            targetDistance += deltaMagnitudeDiff * touchZoomSensitivity * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    void UpdateCameraMode()
    {
        if (currentDistance < transitionDistance)
        {
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Lerp(orthographicSize, orthographicSize * 2f, (currentDistance - minDistance) / (transitionDistance - minDistance));
        }
        else
        {
            cam.orthographic = false;
            cam.fieldOfView = perspectiveFOV;
        }
    }

    void UpdateCameraPosition()
    {
        float zoomFactor = Mathf.Clamp01((currentDistance - minDistance) / (maxDistance - minDistance));

        Debug.Log($"Zoom Factor: {zoomFactor}, Current Distance: {currentDistance}, Max Distance: {maxDistance}");

        Vector3 currentViewAngle;
        if (!cam.orthographic)
        {
            currentViewAngle = topDownAngle;
        }
        else
        {
            currentViewAngle = new Vector3(Mathf.Lerp(isometricAngle.x, topDownAngle.x, zoomFactor), Mathf.Lerp(isometricAngle.y, topDownAngle.y, zoomFactor),0f);
        }

        Debug.Log($"Current View Angle: {currentViewAngle}");

        Quaternion rotation = Quaternion.Euler(currentViewAngle.x, currentAngle, currentViewAngle.z);
        Vector3 offsetPosition = Vector3.Lerp(target.position, floor.position, zoomFactor);
        Vector3 position = offsetPosition + (rotation * Vector3.back * currentDistance);
        transform.position = position;
        transform.rotation = rotation;
    }
    void OnValidate()
    {
        if (Application.isPlaying && target != null && floor != null)
        {
            CalculateMapViewParameters();
            UpdateCameraMode();
            UpdateCameraPosition();
        }
    }

    public void SetFloor(Transform newFloor)
    {
        floor = newFloor;
        CalculateMapViewParameters();
    }

    private void CalculateMapViewParameters()
    {
        if (floor == null) return;

        Bounds bounds = new Bounds(floor.position, Vector3.zero);
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
        }
        else
        {
            Collider collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                bounds = collider.bounds;
            }
            else
            {
                Debug.LogWarning("Floor object has no renderer or collider! Using default bounds. Ensure the floor object has a renderer or collider to calculate accurate bounds.");
                bounds = new Bounds(floor.position, new Vector3(10f, 0f, 10f)); // Default bounds if no renderer or collider
            }
        }

        Vector3 boundsSize = bounds.size;
        float mapDiagonal = Mathf.Sqrt(boundsSize.x * boundsSize.x + boundsSize.z * boundsSize.z);
        maxDistance = (mapDiagonal / 2f) / Mathf.Tan(Mathf.Deg2Rad * perspectiveFOV / 2f);
        maxDistance *= 1.1f;
        maxDistance = Mathf.Max(maxDistance, transitionDistance + 1f);
        Debug.Log($"Calculated Max Distance: {maxDistance}");
    }
}
