using UnityEngine;
using TMPro;
using System.Collections;

public class TextSizeAnimation : MonoBehaviour
{
    [SerializeField] private TMP_Text textMeshPro; // Reference to TMP_Text component
    [SerializeField] private float sizeAnimationDuration = 1f; // Duration of font size animation
    [SerializeField] private float moveAnimationDuration = 1f; // Duration of movement animation
    [SerializeField] private float startFontSize = 300f;
    [SerializeField] private float endFontSize = 200f;
    [SerializeField] private float initialDelay = 1f; // Delay before animations can start
    [SerializeField] private float MoveupH = 250f;
    private float sizeElapsedTime = 0f;
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private float moveElapsedTime = 0f;
    private bool isMoving = false;
    private bool canAnimate = false;
    private bool hasClicked = false; // Track if click has occurred
    private bool sizeAnimationCompleted = false; // Track font size animation

    void OnEnable()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TMP_Text>();
        }
        textMeshPro.fontSize = startFontSize; // Set initial font size

        rectTransform = textMeshPro.GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition; // Store initial position (y=0)
        endPosition = new Vector2(startPosition.x, startPosition.y + MoveupH);

        // Start delay coroutine
        StartCoroutine(StartAnimationsAfterDelay());
    }

    private IEnumerator StartAnimationsAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        canAnimate = true; // Allow animations to proceed
    }

    void Update()
    {
        if (!canAnimate) return; // Skip updates until delay is over

        // Handle font size animation (runs once)
        if (!sizeAnimationCompleted && sizeElapsedTime < sizeAnimationDuration)
        {
            sizeElapsedTime += Time.deltaTime;
            float t = sizeElapsedTime / sizeAnimationDuration;
            textMeshPro.fontSize = Mathf.Lerp(startFontSize, endFontSize, t);

            if (sizeElapsedTime >= sizeAnimationDuration)
            {
                sizeAnimationCompleted = true; // Mark font size animation as complete
                textMeshPro.fontSize = endFontSize; // Ensure final size is set
            }
        }

        // Check for screen click to trigger movement (only first click)
        if (!hasClicked && Input.GetMouseButtonDown(0))
        {
            isMoving = true;
            hasClicked = true; // Prevent further clicks from triggering
            moveElapsedTime = 0f; // Reset movement timer
        }

        // Handle movement animation (runs once)
        if (isMoving && moveElapsedTime < moveAnimationDuration)
        {
            moveElapsedTime += Time.deltaTime;
            float t = moveElapsedTime / moveAnimationDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            if (moveElapsedTime >= moveAnimationDuration)
            {
                isMoving = false; // Stop movement
                rectTransform.anchoredPosition = endPosition; // Ensure final position
            }
        }
    }
}