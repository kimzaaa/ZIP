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
        SpawnNewPackage();
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
            Quaternion rotation = Quaternion.Euler(-45, 0, -90);

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
            if (packageController != null)
            {
                packageController.HealPackage();
                if (healEffect != null)
                {
                    Instantiate(healEffect, transform.position + Vector3.up, Quaternion.identity);
                }
            }

            // Player picks up the package
            if (hasPackage)
            {
                hasPackage = false;

                // Destroy the existing package object (optional: you could store it as a reference)
                GameObject existingPackage = GameObject.FindGameObjectWithTag("Package");
                if (existingPackage != null)
                {
                    Destroy(existingPackage);
                }
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