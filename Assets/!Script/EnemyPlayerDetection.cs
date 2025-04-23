using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using System.Collections.Generic;

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
    private List<GameObject> bulletPool = new List<GameObject>();
    private int poolSize = 24;

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }

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
            GameObject bullet = GetInactiveBullet();
            if (bullet != null)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;

                bullet.transform.position = bulletSpawnPoint.position;
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * bulletSpeed;
                }
                else
                {
                    Debug.LogWarning("Bullet prefab is missing Rigidbody component!", bullet);
                }
                bullet.SetActive(true);

                BulletDespawner despawner = bullet.GetComponent<BulletDespawner>();
                if (despawner == null)
                {
                    despawner = bullet.AddComponent<BulletDespawner>();
                }
                despawner.StartDespawn(bulletDespawnTime);
            }
            else
            {
                Debug.LogWarning("No inactive bullet available in pool!");
            }
        }
    }

    private GameObject GetInactiveBullet()
    {
        foreach (GameObject bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
            {
                return bullet;
            }
        }
        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isPlayerDetected ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

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
            gameObject.SetActive(false);
        }
    }
}