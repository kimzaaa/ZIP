using UnityEngine;

public class DestructibleBox : MonoBehaviour
{
    private int damageToPackage;
    private GameObject destroyEffect;

    private void Start()
    {
        if (DestructibleBoxManager.Instance != null)
        {
            DestructibleBoxManager.Instance.ConfigureBox(this);
        }
    }

    public void SetProperties(int damage, GameObject effect)
    {
        damageToPackage = damage;
        destroyEffect = effect;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.DamagePackage(damageToPackage);
            }

            if (destroyEffect != null && DestructibleBoxManager.Instance != null)
            {
                DestructibleBoxManager.Instance.SpawnDestroyEffect(transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}