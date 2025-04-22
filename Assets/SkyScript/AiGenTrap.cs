using UnityEngine;
using System.Collections;

public class AiGenTrap : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;               // ��� Player (�������ͧ����)

    [Header("Trap Settings")]
    public GameObject pillarPrefab;         // Prefab ����Թ
    public Terrain terrain;                 // Terrain
    public float spawnDistanceMin = 15f;    // ������ҧ�����ش
    public float spawnDistanceMax = 25f;    // ������ҧ�ҡ�ش

    [Header("Pillar Movement")]
    public float pillarRiseSpeed = 2f;          // ����������Ң��
    public float pillarHeightPercent = 0.75f;   // �����繵�ͧ��ҷ������
    public float pillarStartDepth = 3f;         // �֡ŧ���Թ���˹���

    [Header("Trap Timing")]
    public float spawnIntervalEarly = 15f;      // �ء 15 �ԡ�͹ 5 �ҷ�
    public float spawnIntervalLate = 10f;       // �ء 10 ����ѧ 5 �ҷ�

    private float gameTime = 0f;                // �Ѻ�������

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
            }

            gameTime += interval;
        }
    }

    Vector3 CalculateSpawnPosition()
    {
        // �ͧ仢�ҧ˹�� player (�������ͧ)
        Vector3 forward = player.forward;
        forward.y = 0;
        forward.Normalize();

        // ����������ҧ
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        // �������§�������-�����硹���
        float angle = Random.Range(-30f, 30f);
        forward = Quaternion.Euler(0, angle, 0) * forward;

        Vector3 targetPosition = player.position + forward * distance;

        // �ҧ������ç��� Terrain
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
        float targetHeight = 5f; // �����٧�ͧ�������
        float riseHeight = targetHeight * pillarHeightPercent;

        Vector3 groundPos = pillar.transform.position; // ���˹觺����
        Vector3 startPos = groundPos - Vector3.up * pillarStartDepth; // ������֡ŧ�
        Vector3 endPos = groundPos + Vector3.up * riseHeight; // ����Ҩ��֧�ش����ͧ���

        pillar.transform.position = startPos; // ��駵���Թ��͹

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

        // ��ͤ���˹��ش����������
        pillar.transform.position = endPos;
    }
}
