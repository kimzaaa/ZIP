using UnityEngine;
using TMPro;
using System.Collections;

public class UIEnableOnClick : MonoBehaviour
{
    [SerializeField] private TMP_Text textMeshPro; // Reference to TMP_Text component
    [SerializeField] private float clickDelay = 0.5f; // Delay after click before fade-in starts
    [SerializeField] private float fadeDuration = 1f; // Duration of fade-in animation
    private bool hasClicked = false;

    void OnEnable()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TMP_Text>();
        }
        // Initialize with fully transparent and disabled
        textMeshPro.enabled = false;
        Color textColor = textMeshPro.color;
        textMeshPro.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
    }

    void Update()
    {
        // Trigger fade-in on screen click
        if (Input.GetMouseButtonDown(0) && !hasClicked)
        {
            hasClicked = true; // Prevent multiple triggers
            StartCoroutine(FadeInAfterDelay());
        }
    }

    private IEnumerator FadeInAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(clickDelay);

        // Enable the text and start fading in
        textMeshPro.enabled = true;
        float elapsedTime = 0f;
        Color startColor = textMeshPro.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            textMeshPro.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Ensure final color is fully opaque
        textMeshPro.color = targetColor;
    }
}