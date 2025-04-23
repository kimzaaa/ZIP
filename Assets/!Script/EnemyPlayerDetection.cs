using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class EnemyPlayerDetection : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 5f; // Radius for player detection
    [SerializeField] private GameObject bulletPrefab; // Bullet prefab (requires Rigidbody)
    [SerializeField] private Transform bulletSpawnPoint; // Spawn point for bullets
    [SerializeField] private int bulletCount = 12; // Number of bullets in the circle for denser pattern
    [SerializeField] private float bulletSpeed = 20f; // Very fast bullet speed
    [SerializeField] private float shootCooldown = 0.5f; // Faster cooldown for rapid firing
    [SerializeField] private float bulletDespawnTime = 2f; // Time before bullet despawns
    public ShakeData explosiveShake;

    private bool isPlayerDetected = false;
    private float shootTimer = 0f;

    void Update()
    {
        // Check for colliders in radius
        isPlayerDetected = false;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                isPlayerDetected = true;
                break;
            }
        }

        // Handle shooting
        if (isPlayerDetected && bulletPrefab != null && bulletSpawnPoint != null)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                ShootBulletCircle();
                shootTimer = shootCooldown;
            }
        }
    }

    void ShootBulletCircle()
    {
        // Calculate angle between each bullet for even circular distribution
        float angleStep = 360f / bulletCount;
        CameraShakerHandler.Shake(explosiveShake);

        for (int i = 0; i < bulletCount; i++)
        {
            // Calculate angle in radians
            float angle = i * angleStep * Mathf.Deg2Rad;

            // Calculate direction in XZ plane for circular pattern
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;

            // Instantiate bullet at spawn point
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            
            // Set bullet velocity (assumes bullet has a Rigidbody)
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * bulletSpeed;
            }
            else
            {
                Debug.LogWarning("Bullet prefab is missing Rigidbody component!", bullet);
            }

            // Add a component to handle despawning if not already present
            BulletDespawner despawner = bullet.GetComponent<BulletDespawner>();
            if (despawner == null)
            {
                despawner = bullet.AddComponent<BulletDespawner>();
            }
            despawner.StartDespawn(bulletDespawnTime);
        }
    }

    // Draw detection radius in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = isPlayerDetected ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

// New MonoBehaviour to handle bullet despawning
public class BulletDespawner : MonoBehaviour
{
    public void StartDespawn(float despawnTime)
    {
        StartCoroutine(DespawnAfterTime(despawnTime));
    }

    private IEnumerator DespawnAfterTime(float despawnTime)
    {
        yield return new WaitForSeconds(despawnTime);
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}