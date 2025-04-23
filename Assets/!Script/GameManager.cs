using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 300f; // 5 minutes
    [SerializeField] private float remainingTime;
    [SerializeField] private bool gameOver = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI waypointsRemainingText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalRankText;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip lowTimeWarningSound;
    private AudioSource audioSource;
    private bool lowTimeWarningPlayed = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        remainingTime = gameDuration;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateUI();
    }

    private void Update()
    {
        if (gameOver)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 30f && !lowTimeWarningPlayed)
        {
            PlayLowTimeWarning();
        }

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            EndGame();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

            if (remainingTime <= 30f)
            {
                timeText.color = Color.red;
            }
            else
            {
                timeText.color = Color.white;
            }
        }

        if (waypointsRemainingText != null && WaypointManager.Instance != null)
        {
            waypointsRemainingText.text = "Waypoints: " + WaypointManager.Instance.GetRemainingWaypoints();
        }
    }

    public void AddTime(float timeToAdd)
    {
        remainingTime += timeToAdd;

        if (ScoreManager.Instance != null)
        {
            
        }
    }

    private void EndGame()
    {
        gameOver = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveHighScore();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (finalScoreText != null && ScoreManager.Instance != null)
            {
                finalScoreText.text = "Score: " + Mathf.FloorToInt(ScoreManager.Instance.finalScore);
            }

            if (finalRankText != null && ScoreManager.Instance != null)
            {
                finalRankText.text = ScoreManager.Instance.GetCurrentRank();
            }
        }

        if (gameOverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
    }

    private void PlayLowTimeWarning()
    {
        lowTimeWarningPlayed = true;

        if (lowTimeWarningSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lowTimeWarningSound);
        }
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
            Application.Quit();
    }

    public bool IsGameOver()
    {
        return gameOver;
    }
}