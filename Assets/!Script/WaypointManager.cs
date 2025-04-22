using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance;

    [Header("Waypoint Settings")]
    [SerializeField] private GameObject waypointPrefab;
    [SerializeField] private int waypointCount = 10;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float yPosition = 0.5f;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(40f, 40f);

    [Header("Scoring")]
    [SerializeField] private int pointsPerWaypoint = 50;
    [SerializeField] private float timeBonus = 10f;

    private List<GameObject> activeWaypoints = new List<GameObject>();
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
        GenerateWaypoints();
    }

    public void GenerateWaypoints()
    {
        ClearWaypoints();

        for (int i = 0; i < waypointCount; i++)
        {
            SpawnWaypoint();
        }
    }

    private void SpawnWaypoint()
    {
        Vector3 spawnPos;
        int maxAttempts = 30;
        int attempts = 0;

        do
        {
            float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float z = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
            spawnPos = new Vector3(x, yPosition, z);
            attempts++;

            if (attempts > maxAttempts)
            {
                Debug.LogWarning("Couldn't find valid waypoint position after " + maxAttempts + " attempts.");
                return;
            }
        }
        while (!IsValidPosition(spawnPos));

        GameObject waypoint = Instantiate(waypointPrefab, spawnPos, Quaternion.identity);
        Waypoint waypointComponent = waypoint.GetComponent<Waypoint>();

        if (waypointComponent != null)
        {
            waypointComponent.SetPoints(pointsPerWaypoint);
            waypointComponent.SetTimeBonus(timeBonus);
        }

        activeWaypoints.Add(waypoint);
    }

    private bool IsValidPosition(Vector3 position)
    {
        if (house != null && Vector3.Distance(house.position, position) < minDistance)
        {
            return false;
        }

        foreach (GameObject waypoint in activeWaypoints)
        {
            if (Vector3.Distance(waypoint.transform.position, position) < minDistance)
            {
                return false;
            }
        }

        if (Physics.CheckSphere(position, 1f, LayerMask.GetMask("Obstacles")))
        {
            return false;
        }

        return true;
    }

    private void ClearWaypoints()
    {
        foreach (GameObject waypoint in activeWaypoints)
        {
            if (waypoint != null)
            {
                Destroy(waypoint);
            }
        }

        activeWaypoints.Clear();
    }

    public void WaypointCollected(Waypoint waypoint)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(waypoint.GetPoints());
            ScoreManager.Instance.AddTime(waypoint.GetTimeBonus());
        }

        // Remove from active list
        GameObject waypointObj = waypoint.gameObject;
        activeWaypoints.Remove(waypointObj);

        if (activeWaypoints.Count == 0)
        {
            GenerateWaypoints();
        }
    }

    public int GetRemainingWaypoints()
    {
        return activeWaypoints.Count;
    }
}