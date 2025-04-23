using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIMoveAndFill : MonoBehaviour
{
    public Image movingImage; // The UI Image that moves left to right
    public Image fillImage;   // The UI Image that fills right to left
    public float moveDuration = 2f; // Duration for movement
    public float fillDuration = 2f; // Duration for filling
    public float screenWidth = 1920f; // Canvas width (adjust based on your Canvas size)

    private RectTransform movingImageRect;

    void OnEnable()
    {
        movingImageRect = movingImage.GetComponent<RectTransform>();
        fillImage.fillAmount = 0; // Initialize fill to 0
        StartCoroutine(AnimateUI());
    }

    IEnumerator AnimateUI()
    {
        // Step 1: Move image from left to right
        Vector2 startPos = new Vector2(-screenWidth / 2, movingImageRect.anchoredPosition.y);
        Vector2 endPos = new Vector2(screenWidth / 2, movingImageRect.anchoredPosition.y);
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            movingImageRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        movingImageRect.anchoredPosition = endPos; // Ensure final position

        // Step 2: Fill image from right to left
        elapsedTime = 0f;
        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fillDuration;
            fillImage.fillAmount = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        fillImage.fillAmount = 1; // Ensure full fill
    }
}