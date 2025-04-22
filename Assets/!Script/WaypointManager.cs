using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance;

    [Header("Waypoint Settings")]
    [SerializeField] private GameObject waypointPrefab;
    [SerializeField] private float yPosition = 0.5f;

    [Header("Scoring")]
    [SerializeField] private int pointsPerWaypoint = 50;
    [SerializeField] private float timeBonus = 10f;

    private List<Transform> waypointPositions = new List<Transform>();
    private GameObject activeWaypoint;
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

        Vector3 spawnPos = new Vector3(
            selectedPosition.position.x,
            yPosition,
            selectedPosition.position.z
        );

        GameObject waypoint = Instantiate(waypointPrefab, spawnPos, Quaternion.identity);
        Waypoint waypointComponent = waypoint.GetComponent<Waypoint>();

        if (waypointComponent != null)
        {
            waypointComponent.SetPoints(pointsPerWaypoint);
            waypointComponent.SetTimeBonus(timeBonus);
        }

        activeWaypoint = waypoint;
    }

    private void ClearWaypoints()
    {
        if (activeWaypoint != null)
        {
            Destroy(activeWaypoint);
            activeWaypoint = null;
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