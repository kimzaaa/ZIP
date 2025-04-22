using DG.Tweening.Core.Easing;
using UnityEngine;

[RequireComponent(typeof(PlayerController2))]
public class PackageController : MonoBehaviour
{
    [Header("Package Properties")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDamaged = false;

    [Header("Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject healEffect;

    private PlayerController2 playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2>();
        currentHealth = maxHealth;
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
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDamaged = true;

            if (damageEffect != null)
            {
                Instantiate(damageEffect, transform.position, Quaternion.identity);
            }
        }
    }

    public void HealPackage()
    {
        currentHealth = maxHealth;
        isDamaged = false;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waypoint"))
        {
            Waypoint waypoint = other.GetComponent<Waypoint>();
            if (waypoint != null && !isDamaged)
            {
                waypoint.CollectWaypoint();

                GameObject houseObject = GameObject.FindGameObjectWithTag("House");
                if (houseObject != null)
                {
                    House house = houseObject.GetComponent<House>();
                    if (house != null)
                    {
                        house.PackageDelivered();
                    }
                }

                Destroy(gameObject);
            }
        }

        if (other.CompareTag("House") && isDamaged)
        {
            HealPackage();
        }
    }
}