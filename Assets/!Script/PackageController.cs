using UnityEngine;

[RequireComponent(typeof(PlayerController2))]
public class PackageController : MonoBehaviour
{
    [Header("Package Properties")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDamaged = false;
    [SerializeField] private GameObject packageVisual;

    [Header("Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject deliveryEffect;

    private PlayerController2 playerController;
    private bool hasPackage = true;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2>();
        currentHealth = maxHealth;
        hasPackage = true;
    }

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.packageHP = currentHealth;
            ScoreManager.Instance.maxPackageHP = maxHealth;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.packageHP = currentHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!hasPackage)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDamaged = true;

            if (damageEffect != null)
            {
                Instantiate(damageEffect, transform.position, Quaternion.identity);
            }

            if (WaypointManager.Instance != null)
            {
                WaypointManager.Instance.ResetWaypoint();
            }
        }
    }

    public void HealPackage()
    {
        currentHealth = maxHealth;
        isDamaged = false;
        hasPackage = true;

        if (packageVisual != null)
        {
            packageVisual.SetActive(true);
        }

        if (healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.HealPackage(maxHealth);
        }
    }

    public bool IsDamaged()
    {
        return isDamaged;
    }

    public bool HasPackage()
    {
        return hasPackage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waypoint") && hasPackage)
        {
            Waypoint waypoint = other.GetComponent<Waypoint>();
            if (waypoint != null && !isDamaged)
            {
                waypoint.CollectWaypoint();

                if (deliveryEffect != null)
                {
                    Instantiate(deliveryEffect, transform.position, Quaternion.identity);
                }

                GameObject houseObject = GameObject.FindGameObjectWithTag("House");
                if (houseObject != null)
                {
                    House house = houseObject.GetComponent<House>();
                    if (house != null)
                    {
                        house.PackageDelivered();
                    }
                }

                hasPackage = false;
                isDamaged = false;
                currentHealth = 0;

                if (packageVisual != null)
                {
                    packageVisual.SetActive(false);
                }

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.packageHP = 0;
                }
            }
        }

        if (other.CompareTag("House"))
        {
            if (isDamaged || !hasPackage)
            {
                HealPackage();
            }
        }
    }
}