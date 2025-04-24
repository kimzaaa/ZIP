using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Reference to the pause menu UI GameObject
    private bool isPaused = false; // Flag to check if the game is paused
    void Update()
    {
        
        // Check if the "Escape" key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseMenuToggle(); // Call the Toggle function to pause or unpause the game
        }
    }   

    void PauseMenuToggle()
    {
        
        isPaused = !isPaused; // Toggle the pause state
        pauseMenuUI.SetActive(isPaused); // Show or hide the pause menu UI based on the pause state

        if (isPaused)
        {
            Time.timeScale = 0f; // Pause the game by setting time scale to 0
            pauseMenuUI.SetActive(true); // Show the pause menu UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true; // Show the cursor when the game is paused
        }
        else
        {
            Time.timeScale = 1f; // Resume the game by setting time scale back to 1
            pauseMenuUI.SetActive(false); // Hide the pause menu UI
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the game window
            Cursor.visible = false;
        }
    }
}

