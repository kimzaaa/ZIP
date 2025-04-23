using UnityEngine;
using System.Collections;

public class AiGenTrap : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;

    [Header("Trap Settings")]
    public GameObject pillarPrefab;
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

    void Start()
    {
        StartCoroutine(SpawnTrapRoutine());
    }

    IEnumerator SpawnTrapRoutine()
    {
        while (true)
        {
            float interval = gameTime < 300f ? spawnIntervalEarly : spawnIntervalLate;
            yield return new WaitForSeconds(interval);

            Vector3 spawnPosition = CalculateSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                GameObject pillar = Instantiate(pillarPrefab, spawnPosition, Quaternion.identity);
                StartCoroutine(RaisePillar(pillar));
                StartCoroutine(DestroyPillar(pillar));
            }

            gameTime += interval;
        }
    }

    Vector3 CalculateSpawnPosition()
    {
        Vector3 forward = player.forward;
        forward.y = 0;
        forward.Normalize();

        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        float angle = Random.Range(-30f, 30f);
        forward = Quaternion.Euler(0, angle, 0) * forward;

        Vector3 targetPosition = player.position + forward * distance;

        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(targetPosition);
            targetPosition.y = terrainHeight;
            return targetPosition;
        }

        return Vector3.zero;
    }

    IEnumerator RaisePillar(GameObject pillar)
    {
        float targetHeight = 5f;
        float riseHeight = targetHeight * pillarHeightPercent;

        Vector3 groundPos = pillar.transform.position;
        Vector3 startPos = groundPos - Vector3.up * pillarStartDepth;
        Vector3 endPos = groundPos + Vector3.up * riseHeight;

        pillar.transform.position = startPos;

        float totalRiseDistance = Vector3.Distance(startPos, endPos);
        float duration = totalRiseDistance / pillarRiseSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            pillar.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        pillar.transform.position = endPos;
    }

    IEnumerator DestroyPillar(GameObject pillar)
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(pillar);
    }
}