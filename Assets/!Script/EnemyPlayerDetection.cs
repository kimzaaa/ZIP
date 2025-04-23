using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class EnemyPlayerDetection : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private int bulletCount = 12;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float bulletDespawnTime = 2f;
    public ShakeData explosiveShake;

    private bool isPlayerDetected = false;
    private float shootTimer = 0f;

    void Update()
    {
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
        float angleStep = 360f / bulletCount;
        CameraShakerHandler.Shake(explosiveShake);

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;

            GameObject bullet = PoolManager.Instance.GetObject(
                bulletPrefab,
                bulletSpawnPoint.position,
                Quaternion.identity
            );

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * bulletSpeed;
            }
            else
            {
                Debug.LogWarning("Bullet prefab is missing Rigidbody component!", bullet);
            }

            BulletDespawner despawner = bullet.GetComponent<BulletDespawner>();
            if (despawner != null)
            {
                despawner.StartDespawn(bulletDespawnTime);
            }
            else
            {
                Debug.LogWarning("Bullet prefab is missing BulletDespawner component!", bullet);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isPlayerDetected ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}