using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField] private int pointValue = 50;
    [SerializeField] private float timeBonus = 10f;
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private float rotationSpeed = 50f;

    private void Update()
    {
        // Rotate waypoint for visual effect
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void SetPoints(int points)
    {
        pointValue = points;
    }

    public int GetPoints()
    {
        return pointValue;
    }

    public void SetTimeBonus(float bonus)
    {
        timeBonus = bonus;
    }

    public float GetTimeBonus()
    {
        return timeBonus;
    }

    public void CollectWaypoint()
    {
        if (WaypointManager.Instance != null)
        {
            WaypointManager.Instance.WaypointCollected(this);
        }

        // Play collection effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Destroy this waypoint
        Destroy(gameObject);
    }
}