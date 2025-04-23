using UnityEngine;
using TMPro;
using System.Collections;

public class NumberDisplay : MonoBehaviour
{
    public TMP_Text displayText; // Assign your TMP_Text component in the Inspector
    public float animationDuration = 2f; // Duration of the count-up animation
    public float startDelay = 1f; // Delay before allowing the animation to start
    private bool hasClicked1 = false;
    private bool isAnimationTriggered = false; // Prevents multiple animations
    private int targetNumber; // Store the target number for animation

    // Call this to process and display the number
    public void DisplayNumber(float input)
    {
        // Convert float to int if decimals are not all zeros
        targetNumber = Mathf.FloorToInt(input);
        if (input % 1 == 0) // If input is a whole number (e.g., 1.0, 2.0)
        {
            targetNumber = (int)input;
        }

        // Reset click flag to allow new input to require a click
        hasClicked1 = false;
        isAnimationTriggered = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !hasClicked1 && !isAnimationTriggered)
        {
            Debug.Log("Clicked");
            hasClicked1 = true;
            isAnimationTriggered = true;
            // Start the count-up animation with delay
            StartCoroutine(CountUpToNumber(targetNumber));
        }
    }

    private IEnumerator CountUpToNumber(int targetNumber)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(startDelay);

        float elapsedTime = 0f;
        int currentNumber = 0;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            currentNumber = Mathf.FloorToInt(Mathf.Lerp(0, targetNumber, progress));
            displayText.text = currentNumber.ToString();
            yield return null;
        }

        // Ensure the final number is exact
        displayText.text = targetNumber.ToString();
    }
}