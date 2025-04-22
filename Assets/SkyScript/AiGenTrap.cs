using UnityEngine;
using System.Collections;

public class AiGenTrap : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;               // ตัว Player (ไม่ใช่กล้องแล้ว)

    [Header("Trap Settings")]
    public GameObject pillarPrefab;         // Prefab เสาหิน
    public Terrain terrain;                 // Terrain
    public float spawnDistanceMin = 15f;    // ระยะห่างน้อยสุด
    public float spawnDistanceMax = 25f;    // ระยะห่างมากสุด

    [Header("Pillar Movement")]
    public float pillarRiseSpeed = 2f;          // ความเร็วเสาขึ้น
    public float pillarHeightPercent = 0.75f;   // เปอร์เซ็นต์ของเสาที่ขึ้นมา
    public float pillarStartDepth = 3f;         // ลึกลงไปใต้ดินกี่หน่วย

    [Header("Trap Timing")]
    public float spawnIntervalEarly = 15f;      // ทุก 15 วิก่อน 5 นาที
    public float spawnIntervalLate = 10f;       // ทุก 10 วิหลัง 5 นาที

    private float gameTime = 0f;                // จับเวลารวม

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
        // มองไปข้างหน้า player (ไม่ใช่กล้อง)
        Vector3 forward = player.forward;
        forward.y = 0;
        forward.Normalize();

        // สุ่มระยะห่าง
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        // สุ่มเบี่ยงมุมซ้าย-ขวาเล็กน้อย
        float angle = Random.Range(-30f, 30f);
        forward = Quaternion.Euler(0, angle, 0) * forward;

        Vector3 targetPosition = player.position + forward * distance;

        // วางเสาให้ตรงพื้น Terrain
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
        float targetHeight = 5f; // ความสูงของเสาเต็มๆ
        float riseHeight = targetHeight * pillarHeightPercent;

        Vector3 groundPos = pillar.transform.position; // ตำแหน่งบนพื้น
        Vector3 startPos = groundPos - Vector3.up * pillarStartDepth; // เริ่มลึกลงไป
        Vector3 endPos = groundPos + Vector3.up * riseHeight; // ขึ้นมาจนถึงจุดที่ต้องการ

        pillar.transform.position = startPos; // ตั้งต้นใต้ดินก่อน

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

        // ล็อคตำแหน่งสุดท้ายให้เป๊ะ
        pillar.transform.position = endPos;
    }
}
