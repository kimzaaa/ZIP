using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Package Settings")]
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject packageReceivedEffect;

    private bool isPackageDelivered = false;
    private float respawnTimer = 0f;
    private bool hasPackage = false;

    void Start()
    {
        GameObject existingPackage = GameObject.FindGameObjectWithTag("Package");
        hasPackage = (existingPackage != null);
    }

    void Update()
    {
        if (isPackageDelivered)
        {
            respawnTimer += Time.deltaTime;

            if (respawnTimer >= respawnDelay)
            {
                SpawnNewPackage();
                isPackageDelivered = false;
                respawnTimer = 0f;
            }
        }
    }

    public void PackageDelivered()
    {
        isPackageDelivered = true;
        hasPackage = false;
    }

    private void SpawnNewPackage()
    {
        if (packagePrefab != null)
        {
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(0, 1f, 0);
            Quaternion rotation = Quaternion.identity;

            Instantiate(packagePrefab, position, rotation);
            hasPackage = true;

            if (packageReceivedEffect != null)
            {
                Instantiate(packageReceivedEffect, position, Quaternion.identity);
            }
        }
        else
        {
            Debug.LogError("Package prefab not assigned in House component!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PackageController packageController = other.GetComponent<PackageController>();
            if (packageController != null && packageController.IsDamaged())
            {
                packageController.HealPackage();
                if (healEffect != null)
                {
                    Instantiate(healEffect, transform.position + Vector3.up, Quaternion.identity);
                }
            }

            if (!hasPackage && !isPackageDelivered)
            {
                SpawnNewPackage();
            }

            if (WaypointManager.Instance != null && !WaypointManager.Instance.HasActiveWaypoint())
            {
                WaypointManager.Instance.GenerateWaypoint();
            }
        }
    }

    public bool HasPackage()
    {
        return hasPackage;
    }
}