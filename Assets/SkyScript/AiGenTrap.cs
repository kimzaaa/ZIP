using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class AiGenTrap : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;

    [Header("Trap Settings")]
    public List<GameObject> pillarPrefabs;
    public Terrain terrain;
    public float spawnDistanceMin = 15f;
    public float spawnDistanceMax = 25f;

    [Header("Pillar Movement")]
    public float pillarRiseSpeed = 2f;
    public float pillarHeightPercent = 0.75f;
    public float pillarStartDepth = 3f;

    [Header("Trap Timing")]
    public float spawnIntervalEarly = 15f;
    public float spawnIntervalLate = 10f;
    public float destroyDelay = 5f;

    private float gameTime = 0f;
    private Dictionary<GameObject, Queue<GameObject>> pillarPools;
    private int poolSizePerPrefab = 5;
    private System.Random random;
    private WaitForSeconds earlyInterval;
    private WaitForSeconds lateInterval;

    void Awake()
    {
        DOTween.Init().SetCapacity(50, 10);
        random = new System.Random();
        earlyInterval = new WaitForSeconds(spawnIntervalEarly);
        lateInterval = new WaitForSeconds(spawnIntervalLate);
    }

    void Start()
    {
        InitializeObjectPool();
        StartCoroutine(SpawnTrapRoutine());
    }

    void InitializeObjectPool()
    {
        pillarPools = new Dictionary<GameObject, Queue<GameObject>>(pillarPrefabs.Count);
        foreach (GameObject prefab in pillarPrefabs)
        {
            Queue<GameObject> pool = new Queue<GameObject>(poolSizePerPrefab);
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject pillar = Instantiate(prefab);
                pillar.SetActive(false);
                pool.Enqueue(pillar);
            }
            pillarPools.Add(prefab, pool);
        }
    }

    IEnumerator SpawnTrapRoutine()
    {
        while (true)
        {
            float interval = gameTime < 300f ? spawnIntervalEarly : spawnIntervalLate;
            WaitForSeconds wait = gameTime < 300f ? earlyInterval : lateInterval;
            yield return wait;

            Vector3 spawnPosition = CalculateSpawnPosition();
            if (spawnPosition != Vector3.zero && pillarPrefabs.Count > 0)
            {
                GameObject selectedPrefab = pillarPrefabs[random.Next(0, pillarPrefabs.Count)];
                GameObject pillar = GetPooledPillar(selectedPrefab);
                if (pillar != null)
                {
                    pillar.transform.position = spawnPosition;
                    pillar.SetActive(true);

                    Vector3 directionToPlayer = (player.position - spawnPosition).normalized;
                    directionToPlayer.y = 0;
                    if (directionToPlayer != Vector3.zero)
                    {
                        pillar.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                    }

                    AnimatePillar(pillar, selectedPrefab);
                }
            }

            gameTime += interval; // Fixed: Use interval value directly
        }
    }

    GameObject GetPooledPillar(GameObject prefab)
    {
        if (pillarPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            else
            {
                // Optional: Expand pool dynamically
                GameObject pillar = Instantiate(prefab);
                pillar.SetActive(false);
                return pillar;
            }
        }
        return null;
    }


    Vector3 CalculateSpawnPosition()
    {
        Vector3 forward = player.forward;
        forward.y = 0;
        forward.Normalize();

        float distance = spawnDistanceMin + (float)random.NextDouble() * (spawnDistanceMax - spawnDistanceMin);
        float angle = random.Next(-30, 31);
        forward = Quaternion.Euler(0, angle, 0) * forward;

        Vector3 targetPosition = player.position + forward * distance;

        if (terrain != null)
        {
            targetPosition.y = terrain.SampleHeight(targetPosition);
            return targetPosition;
        }

        return Vector3.zero;
    }

    void AnimatePillar(GameObject pillar, GameObject prefab)
    {
        float targetHeight = 5f;
        float riseHeight = targetHeight * pillarHeightPercent;

        Vector3 groundPos = pillar.transform.position;
        Vector3 startPos = groundPos - Vector3.up * pillarStartDepth;
        Vector3 endPos = groundPos + Vector3.up * riseHeight;

        pillar.transform.position = startPos;

        float totalRiseDistance = Vector3.Distance(startPos, endPos);
        float duration = totalRiseDistance / pillarRiseSpeed;

        pillar.transform.DOMove(endPos, duration)
        .SetEase(Ease.InOutQuad)
        .OnComplete(() =>
        {
            DOVirtual.DelayedCall(destroyDelay, () =>
            {
                pillar.SetActive(false);
                if (pillarPools.TryGetValue(prefab, out Queue<GameObject> pool))
                {
                    pool.Enqueue(pillar);
                }
            });
        });
    }
}