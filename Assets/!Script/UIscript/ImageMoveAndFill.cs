using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ImageMoveAndFill : MonoBehaviour
{
    [SerializeField] private Image targetImage; // Assign the UI Image in the Inspector
    [SerializeField] private TextMeshProUGUI targetText; // Assign the TMP Text in the Inspector
    [SerializeField] private float moveDuration = 2f; // Time to move from -10 to 10
    [SerializeField] private float fillDuration = 2f; // Time to fill from right to left
    [SerializeField] private float clickDelay = 1f; // Delay after click before animation starts
    [SerializeField] private float startX = -10f; // Starting X position
    [SerializeField] private float endX = 10f; // Ending X position

    private RectTransform rectTransform;
    private bool hasClicked = false;

    private void OnEnable()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        rectTransform = targetImage.GetComponent<RectTransform>();
        // Ensure the image starts at the correct position
        rectTransform.anchoredPosition = new Vector2(startX, rectTransform.anchoredPosition.y);
        // Ensure the image is fully filled at the start
        targetImage.fillAmount = 1f;

        // Ensure the TMP text is disabled at the start
        if (targetText != null)
        {
            targetText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Detect click (mouse or touch) and start animation if not already clicked
        if (!hasClicked && Input.GetMouseButtonDown(0))
        {
            hasClicked = true;
            StartCoroutine(MoveAndFillWithDelay());
        }
    }

    private IEnumerator MoveAndFillWithDelay()
    {
        // Wait for the specified delay after the click
        yield return new WaitForSeconds(clickDelay);

        // Step 1: Move from startX to endX
        float elapsedTime = 0f;
        Vector2 startPos = new Vector2(startX, rectTransform.anchoredPosition.y);
        Vector2 endPos = new Vector2(endX, rectTransform.anchoredPosition.y);

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure the final position is exact
        rectTransform.anchoredPosition = endPos;

        // Enable the TMP text after movement completes
        if (targetText != null)
        {
            targetText.gameObject.SetActive(true);
        }

        // Step 2: Fill from right to left (fillAmount from 1 to 0)
        elapsedTime = 0f;
        float startFill = 1f;
        float endFill = 0f;

        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fillDuration;
            targetImage.fillAmount = Mathf.Lerp(startFill, endFill, t);
            yield return null;
        }

        // Ensure the final fill amount is exact
        targetImage.fillAmount = endFill;
    }
}