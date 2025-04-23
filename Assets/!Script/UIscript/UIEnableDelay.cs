using UnityEngine;
using TMPro;
using System.Collections;

public class UIEnableDelay : MonoBehaviour
{
    [SerializeField] private TMP_Text textMeshPro; // Reference to TMP_Text component
    [SerializeField] private float enableDelay = 1f; // Delay before enabling the UI

    void OnEnable()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TMP_Text>();
        }
        // Initialize as disabled
        textMeshPro.enabled = false;

        // Start the delay coroutine
        StartCoroutine(EnableAfterDelay());
    }

    private IEnumerator EnableAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(enableDelay);

        // Enable the text
        textMeshPro.enabled = true;
    }
}