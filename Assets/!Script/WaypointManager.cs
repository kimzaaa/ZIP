using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance;

    [Header("Waypoint Settings")]
    [SerializeField] private GameObject waypointPrefab;
    [SerializeField] private float yPosition = 0.5f;

    [Header("Additional Object Settings")]
    [SerializeField] private GameObject additionalObjectPrefab;
    [SerializeField] private Terrain terrain; // Manually assign terrain in Inspector
    [SerializeField] private float spawnRadius = 5f; // Radius around waypoint to spawn additional object

    [Header("Scoring")]
    [SerializeField] private int pointsPerWaypoint = 50;
    [SerializeField] private float timeBonus = 10f;

    private List<Transform> waypointPositions = new List<Transform>();
    private GameObject activeWaypoint;
    private GameObject activeAdditionalObject;
    private Transform house;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        house = GameObject.FindGameObjectWithTag("House")?.transform;

        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject waypoint in waypointObjects)
        {
            waypointPositions.Add(waypoint.transform);
        }

        if (waypointPositions.Count == 0)
        {
            Debug.LogWarning("No preset waypoint positions found in the scene. Make sure to create GameObjects with tag 'WaypointPosition'");
        }

        if (terrain == null)
        {
            Debug.LogWarning("No terrain assigned in WaypointManager. Additional object placement may not work as expected.");
        }
    }

    public void GenerateWaypoint()
    {
        ClearWaypoints();

        if (waypointPositions.Count > 0)
        {
            SpawnWaypointAtPresetPosition();
        }
        else
        {
            Debug.LogError("No waypoint positions available!");
        }
    }

    private void SpawnWaypointAtPresetPosition()
    {
        int randomIndex = Random.Range(0, waypointPositions.Count);
        Transform selectedPosition = waypointPositions[randomIndex];

        // Spawn Waypoint
        Vector3 waypointPos = new Vector3(
            selectedPosition.position.x,
            yPosition,
            selectedPosition.position.z
        );

        GameObject waypoint = Instantiate(waypointPrefab, waypointPos, Quaternion.identity);
        Waypoint waypointComponent = waypoint.GetComponent<Waypoint>();

        if (waypointComponent != null)
        {
            waypointComponent.SetPoints(pointsPerWaypoint);
            waypointComponent.SetTimeBonus(timeBonus);
        }

        activeWaypoint = waypoint;

        // Spawn Additional Object on Terrain
        if (additionalObjectPrefab != null && terrain != null)
        {
            Vector3 additionalSpawnPos = GetPositionAndNormalOnTerrain(waypointPos, out Vector3 terrainNormal);
            // Align to terrain normal and apply -90 degree X rotation
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, terrainNormal) * Quaternion.Euler(-90f, 0f, 0f);
            GameObject additionalObject = Instantiate(additionalObjectPrefab, additionalSpawnPos, rotation);
            activeAdditionalObject = additionalObject;
        }
        else
        {
            Debug.LogWarning("Additional object prefab or terrain not assigned in WaypointManager!");
        }
    }

    private Vector3 GetPositionAndNormalOnTerrain(Vector3 centerPos, out Vector3 terrainNormal)
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 potentialPos = new Vector3(
            centerPos.x + randomCircle.x,
            terrain.transform.position.y + terrain.terrainData.size.y + 10f,
            centerPos.z + randomCircle.y
        );

        Ray ray = new Ray(potentialPos, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, terrain.terrainData.size.y + 20f, LayerMask.GetMask("Terrain")))
        {
            terrainNormal = hit.normal;
            return new Vector3(hit.point.x, hit.point.y + 0.1f, hit.point.z);
        }

        float terrainHeight = terrain.SampleHeight(new Vector3(potentialPos.x, 0, potentialPos.z)) + terrain.transform.position.y;
        terrainNormal = Vector3.up;
        return new Vector3(potentialPos.x, terrainHeight + 0.1f, potentialPos.z);
    }

    private void ClearWaypoints()
    {
        if (activeWaypoint != null)
        {
            Destroy(activeWaypoint);
            activeWaypoint = null;
        }

        if (activeAdditionalObject != null)
        {
            Destroy(activeAdditionalObject);
            activeAdditionalObject = null;
        }
    }

    public void WaypointCollected(Waypoint waypoint)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(waypoint.GetPoints());
            ScoreManager.Instance.AddTime(waypoint.GetTimeBonus());
        }

        activeWaypoint = null;
        if (activeAdditionalObject != null)
        {
            Destroy(activeAdditionalObject);
            activeAdditionalObject = null;
        }
    }

    public bool HasActiveWaypoint()
    {
        return activeWaypoint != null;
    }

    public void ResetWaypoint()
    {
        ClearWaypoints();
    }

    public int GetRemainingWaypoints()
    {
        return activeWaypoint != null ? 1 : 0;
    }

    public void AddWaypointPosition(Transform position)
    {
        if (!waypointPositions.Contains(position))
        {
            waypointPositions.Add(position);
        }
    }
}