using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(PlayerController2))]
public class PackageController : MonoBehaviour
{
    [Header("Package Properties")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDamaged = false;
    [SerializeField] private GameObject packageVisual;

    [Header("UI Elements")]
    [SerializeField] private Slider healthSlider; // Reference to the UI Slider
    [SerializeField] private float healthAnimationDuration = 0.5f; // Duration for DOTween animation

    [Header("Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject deliveryEffect;

    private PlayerController2 playerController;
    public bool hasPackage = true;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2>();
        currentHealth = maxHealth;
        hasPackage = true;
    }

    private void Start()
    {
        // Start with no package
        if (packageVisual.activeSelf) packageVisual.SetActive(false);

        // Initialize the slider
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            healthSlider.gameObject.SetActive(false); // Initially disable slider
        }

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

        // Animate the slider value
        if (healthSlider != null && healthSlider.gameObject.activeSelf)
        {
            healthSlider.DOValue(currentHealth, healthAnimationDuration).SetEase(Ease.OutQuad);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDamaged = true;
            hasPackage = false; // Set hasPackage to false

            // Disable the slider
            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(false);
            }

            // Package visual on the back
            if (packageVisual.activeSelf) packageVisual.SetActive(false);
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

        // Enable and animate the slider value
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            healthSlider.DOValue(currentHealth, healthAnimationDuration).SetEase(Ease.OutQuad);
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

                // AudioManager.Instance.PlayRandomPackageSentSound();

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

                // Disable the slider
                if (healthSlider != null)
                {
                    healthSlider.gameObject.SetActive(false);
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