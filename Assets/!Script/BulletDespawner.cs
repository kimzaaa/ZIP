using UnityEngine;
using System.Collections;

public class BulletDespawner : MonoBehaviour
{
    public void StartDespawn(float despawnTime)
    {
        StartCoroutine(DespawnAfterTime(despawnTime));
    }

    private IEnumerator DespawnAfterTime(float despawnTime)
    {
        yield return new WaitForSeconds(despawnTime);
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}