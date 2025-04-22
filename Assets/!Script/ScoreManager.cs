using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Components")]
    public float totalTime = 0f;
    public float totalMultiplierBonus = 0f;
    public int packageHP = 100;
    public int maxPackageHP = 100;
    public float finalScore = 0f;

    [Header("Multiplier Settings")]
    public float airTimeMultiplier = 0.5f;
    public float airTimeThreshold = 2.0f;
    private float currentAirTime = 0f;
    private bool isCountingAirTime = false;
    private bool airTimeAwarded = false;

    [Header("Rank Thresholds")]
    public float sRankThreshold = 500f;
    public float aRankThreshold = 350f;
    public float bRankThreshold = 250f;
    public float cRankThreshold = 150f;

    [Header("UI References")]
    public Text scoreText;
    public Text packageHPText;
    public Text rankText;
    public GameObject gameOverPanel;

    [Header("Save System")]
    public string currentStage = "Stage1";
    private Dictionary<string, float> highScores = new Dictionary<string, float>();

    private PlayerController2 playerController;

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
            return;
        }

        LoadHighScores();
    }

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController2>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();
    }

    private void Update()
    {
        totalTime += Time.deltaTime;

        if (playerController != null)
        {
            CheckAirTime();
        }

        CalculateScore();

        UpdateUI();
    }

    private void CheckAirTime()
    {
        bool isGrounded = playerController.GetComponent<Rigidbody>().linearVelocity.y == 0 ||
                          Physics.Raycast(playerController.transform.position, Vector3.down, 0.2f);

        if (!isGrounded)
        {
            if (!isCountingAirTime)
            {
                isCountingAirTime = true;
                airTimeAwarded = false;
                currentAirTime = 0f;
            }

            currentAirTime += Time.deltaTime;

            if (currentAirTime >= airTimeThreshold && !airTimeAwarded)
            {
                float airTimeBonus = currentAirTime * airTimeMultiplier;
                totalMultiplierBonus += airTimeBonus;
                airTimeAwarded = true;

                ShowFloatingText($"+{airTimeBonus:F1} Air Time!", Color.cyan);
            }
        }
        else
        {
            isCountingAirTime = false;
        }
    }

    private void CalculateScore()
    {
        finalScore = totalTime + (totalMultiplierBonus * packageHP);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {finalScore:F0}";

        if (packageHPText != null)
            packageHPText.text = $"Package HP: {packageHP}/{maxPackageHP}";

        if (rankText != null)
            rankText.text = $"Current Rank: {GetCurrentRank()}";
    }

    public string GetCurrentRank()
    {
        if (finalScore >= sRankThreshold) return "S";
        if (finalScore >= aRankThreshold) return "A";
        if (finalScore >= bRankThreshold) return "B";
        if (finalScore >= cRankThreshold) return "C";
        return "D";
    }

    public void DamagePackage(int damage = 1)
    {
        packageHP -= damage;

        if (packageHP <= 0)
        {
            packageHP = 0;
            GameOver();
        }

        ShowFloatingText($"-{damage} Package HP!", Color.red);
    }

    public void HealPackage(int healAmount = 1)
    {
        packageHP = Mathf.Min(packageHP + healAmount, maxPackageHP);

        ShowFloatingText($"+{healAmount} Package HP!", Color.green);
    }

    public void AddScoreMultiplier(float multiplierValue, string multiplierName)
    {
        totalMultiplierBonus += multiplierValue;

        ShowFloatingText($"+{multiplierValue:F1} {multiplierName}!", Color.yellow);
    }

    private void GameOver()
    {
        SaveHighScore();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        totalTime = 0f;
        totalMultiplierBonus = 0f;
        packageHP = maxPackageHP;
        finalScore = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void SaveHighScore()
    {
        if (!highScores.ContainsKey(currentStage) || highScores[currentStage] < finalScore)
        {
            highScores[currentStage] = finalScore;

            PlayerPrefs.SetFloat("HighScore_" + currentStage, finalScore);
            PlayerPrefs.Save();
        }
    }

    public void LoadHighScores()
    {
        string[] stages = { "Stage1", "Stage2", "Stage3", "Stage4", "Stage5" };

        foreach (string stage in stages)
        {
            if (PlayerPrefs.HasKey("HighScore_" + stage))
            {
                highScores[stage] = PlayerPrefs.GetFloat("HighScore_" + stage);
            }
            else
            {
                highScores[stage] = 0f;
            }
        }
    }

    public float GetHighScore(string stageName)
    {
        if (highScores.ContainsKey(stageName))
            return highScores[stageName];
        return 0f;
    }

    public void SetStage(string stageName)
    {
        currentStage = stageName;
    }

    private void ShowFloatingText(string message, Color color)
    {
        Debug.Log(message);
    }
}