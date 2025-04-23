using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using DG.Tweening.Core.Easing;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Components")]
    public float totalTime = 0f;
    public float totalMultiplierBonus = 0f;
    public int packageHP = 100;
    public int maxPackageHP = 100;
    public float finalScore = 0f;

    [Header("Airborne Scoring Settings")]
    public float groundedScoreRate = 0.5f; // Score per second when grounded
    public float baseAirScoreRate = 2.0f; // Base score per second when airborne
    public float airTimeThreshold = 0.5f; // Minimum airtime to start scoring
    public float comboInterval = 0.5f; // Time interval for combo increment
    public float comboMultiplierIncrement = 0.5f; // Multiplier increase per combo stage
    public float maxComboMultiplier = 5.0f; // Maximum combo multiplier
    private float currentAirTime = 0f;
    private float timeSinceLastCombo = 0f;
    private float currentComboMultiplier = 1.0f;
    private bool isCountingAirTime = false;

    [Header("Rank Thresholds")]
    public float sRankThreshold = 500f;
    public float aRankThreshold = 350f;
    public float bRankThreshold = 250f;
    public float cRankThreshold = 150f;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI packageHPText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI comboText;
    public GameObject gameOverPanel;

    [Header("Animation Settings")]
    public float scoreAnimationDuration = 0.5f;
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.3f;

    [Header("Save System")]
    public string currentStage = "Stage1";
    private Dictionary<string, float> highScores = new Dictionary<string, float>();

    private PlayerController2 playerController;
    private float previousScore = 0f;

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

        if (comboText != null)
            comboText.text = "";

        UpdateUI();
    }

    private void Update()
    {
        totalTime += Time.deltaTime;

        if (playerController != null)
        {
            CheckAirTimeAndCombo();
        }

        CalculateScore();

        UpdateUI();
    }

    private void CheckAirTimeAndCombo()
    {
        bool isGrounded = playerController.GetComponent<PlayerController2>().isGrounded;

        if (!isGrounded)
        {
            if (!isCountingAirTime)
            {
                isCountingAirTime = true;
                currentAirTime = 0f;
                timeSinceLastCombo = 0f;
                currentComboMultiplier = 1.0f;
            }

            currentAirTime += Time.deltaTime;
            timeSinceLastCombo += Time.deltaTime;

            if (currentAirTime >= airTimeThreshold && timeSinceLastCombo >= comboInterval)
            {
                currentComboMultiplier = Mathf.Min(
                    currentComboMultiplier + comboMultiplierIncrement,
                    maxComboMultiplier
                );
                timeSinceLastCombo = 0f;

                if (comboText != null)
                {
                    comboText.text = $"Combo x{currentComboMultiplier:F1}";
                    AnimateComboText();
                }

                ShowFloatingText($"Combo x{currentComboMultiplier:F1}!", Color.cyan);
            }
        }
        else
        {
            if (isCountingAirTime)
            {
                isCountingAirTime = false;
                currentComboMultiplier = 1.0f;
                if (comboText != null)
                    comboText.text = "";
            }
        }
    }

    private void CalculateScore()
    {
        bool isGrounded = playerController.GetComponent<PlayerController2>().isGrounded;

        if (isGrounded)
        {
            finalScore += groundedScoreRate * Time.deltaTime;
        }
        else
        {
            finalScore += baseAirScoreRate * currentComboMultiplier * Time.deltaTime;
        }

        finalScore += totalMultiplierBonus * packageHP;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            if (Mathf.Abs(finalScore - previousScore) > 0.1f)
            {
                AnimateScoreText(previousScore, finalScore);
                previousScore = finalScore;
            }
            else
            {
                scoreText.text = $"Score: {finalScore:F0}";
            }
        }

        if (packageHPText != null)
            packageHPText.text = $"Package HP: {packageHP}/{maxPackageHP}";

        if (rankText != null)
            rankText.text = $"Rank: {GetCurrentRank()}";
    }

    private void AnimateScoreText(float startScore, float endScore)
    {
        DOTween.Kill(scoreText);

        scoreText.text = $"Score: {startScore:F0}";

        Sequence sequence = DOTween.Sequence();

        float currentValue = startScore;
        sequence.Append(DOTween.To(
            () => currentValue,
            x =>
            {
                currentValue = x;
                scoreText.text = $"Score: {currentValue:F0}";
            },
            endScore,
            scoreAnimationDuration
        ).SetEase(Ease.OutQuad));

        sequence.Join(scoreText.transform.DOScale(pulseScale, pulseDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine));

        sequence.Play();
    }

    private void AnimateComboText()
    {
        DOTween.Kill(comboText);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(comboText.transform.DOScale(pulseScale * 1.2f, pulseDuration)
            .SetEase(Ease.OutBack));
        sequence.Append(comboText.transform.DOScale(1.0f, pulseDuration)
            .SetEase(Ease.InSine));

        sequence.Play();
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
            WaypointManager.Instance.ResetWaypoint();
            //GameOver();
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
    public void AddScore(int points)
    {
        finalScore += points;
        ShowFloatingText($"+{points} Score!", Color.yellow);
    }

    public void AddTime(float seconds)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddTime(seconds);
            ShowFloatingText($"+{seconds:F1} Seconds!", Color.cyan);
        }
    }
}