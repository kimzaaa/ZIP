using UnityEngine;

public class UIMusicBeatScaler : MonoBehaviour
{
    [Tooltip("Beats per minute of the music")]
    [SerializeField] private float bpm = 120f;

    [Tooltip("Maximum scale multiplier on beat (e.g., 1.2 = 120% of original size)")]
    [SerializeField] private float maxScale = 1.2f;

    [Tooltip("How quickly the UI returns to original size after beat")]
    [SerializeField] private float smoothTime = 0.1f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float beatInterval;
    private float timer;
    private Vector3 targetScale;
    private Vector3 scaleVelocity;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        beatInterval = 60f / bpm; // Seconds per beat
        targetScale = originalScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Trigger beat when timer exceeds beat interval
        if (timer >= beatInterval)
        {
            timer -= beatInterval; // Reset timer, accounting for any overshoot
            targetScale = originalScale * maxScale; // Scale up on beat
        }

        // Smoothly interpolate scale back to original or towards target
        rectTransform.localScale = Vector3.SmoothDamp(
            rectTransform.localScale,
            targetScale,
            ref scaleVelocity,
            smoothTime
        );

        // Gradually reduce target scale back to original between beats
        targetScale = Vector3.Lerp(targetScale, originalScale, Time.deltaTime / smoothTime);
    }
}