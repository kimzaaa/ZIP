using UnityEngine;

public class DestructibleBoxManager : MonoBehaviour
{
    public static DestructibleBoxManager Instance;

    [Header("Box Properties")]
    [SerializeField] private int damageToPackage = 10;
    [SerializeField] private GameObject destroyEffect;
    [SerializeField] private float effectDestroyDelay = 5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConfigureBox(DestructibleBox box)
    {
        if (box.destroyEffect!=null)
        {
            box.SetProperties(damageToPackage, destroyEffect);
        }
    }

    public void SpawnDestroyEffect(Vector3 position, Quaternion rotation)
    {
        if (destroyEffect != null)
        {
            GameObject effect = Instantiate(destroyEffect, position, rotation);
            Destroy(effect, effectDestroyDelay);
        }
    }
}