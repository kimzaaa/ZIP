using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Package Settings")]
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject healEffect;

    private bool isPackageDelivered = false;
    private float respawnTimer = 0f;

    void Start()
    {
        GameObject existingPackage = GameObject.FindGameObjectWithTag("Package");
        if (existingPackage == null)
        {
            SpawnNewPackage();
        }
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
        // Optional: Play some effect or sound to indicate successful delivery
        // Instantiate(deliveryEffectPrefab, transform.position, Quaternion.identity);
    }

    private void SpawnNewPackage()
    {
        if (packagePrefab != null)
        {
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(0, 1f, 0);
            Quaternion rotation = Quaternion.identity;

            Instantiate(packagePrefab, position, rotation);

            // Optional: Play spawn effect
            // Instantiate(spawnEffectPrefab, position, Quaternion.identity);
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
        }
    }
}