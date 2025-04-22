using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class DestructibleBox : MonoBehaviour
{
    public int damageToPackage;
    public GameObject destroyEffect;
    public ShakeData explosiveShake;

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
            CameraShakerHandler.Shake(explosiveShake);
            AudioManager.Instance.PlaySFX("ExplodeSFX");
            
            PackageController packageController = other.gameObject.GetComponent<PackageController>();
            if (packageController != null)
            {
                packageController.TakeDamage(damageToPackage);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.DamagePackage(damageToPackage);
            }

            if (DestructibleBoxManager.Instance != null)
            {
                DestructibleBoxManager.Instance.SpawnDestroyEffect(transform.position, Quaternion.identity, destroyEffect);
            }

            PlayerController2 playerController = other.gameObject.GetComponent<PlayerController2>();
            if (playerController != null)
            {
                Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerController.ApplyExternalForce(knockbackDirection * 5f, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
    }
}