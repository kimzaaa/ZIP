using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    [SerializeField] private string sceneName;


    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            ResetScript();
            SceneManager.LoadScene(sceneName);
            Time.timeScale = 1f;
        }
        else
        {
            Debug.LogWarning("Scene name is empty!");
        }
    }

    public void ResetCurrentScene()
    {
        try
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene == null || string.IsNullOrEmpty(currentScene.name))
            {
                Debug.LogError("No active scene found to reset.");
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(currentScene.name))
            {
                ResetScript();
                SceneManager.LoadScene(currentScene.name);
                Time.timeScale = 1f;
            }
            else
            {
                Debug.LogError($"Scene '{currentScene.name}' cannot be loaded. Ensure it is added to Build Settings.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to reset scene: {ex.Message}");
        }
    }

    // Quits the game
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetScript()
    {
        if (PoolManager.Instance != null)
        {
            Destroy(PoolManager.Instance.gameObject);
        }
        if (ScoreManager.Instance != null)
        {
            Destroy(ScoreManager.Instance.gameObject);
        }
        if (DestructibleBoxManager.Instance != null)
        {
            Destroy(DestructibleBoxManager.Instance.gameObject);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}