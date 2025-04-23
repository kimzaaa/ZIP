using UnityEngine;
using TMPro;

public class TextCopy : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sourceText; // The source TMP text (e.g., input field)
    [SerializeField] private TextMeshProUGUI targetText; // The target TMP text to copy to

    private void Start()
    {
        // Ensure both text components are assigned
        if (sourceText == null || targetText == null)
        {
            Debug.LogError("SourceText or TargetText is not assigned in the Inspector!");
            return;
        }

        // Copy initial text
        targetText.text = sourceText.text;

        // Subscribe to the source text's onValueChanged event (for TMP_InputField)
        if (sourceText.GetComponent<TMP_InputField>() != null)
        {
            sourceText.GetComponent<TMP_InputField>().onValueChanged.AddListener(UpdateTargetText);
        }
    }

    private void Update()
    {
        // For non-input fields, update every frame if the text changes
        if (sourceText.GetComponent<TMP_InputField>() == null && targetText.text != sourceText.text)
        {
            targetText.text = sourceText.text;
        }
    }

    private void UpdateTargetText(string newText)
    {
        // Update target text when source text changes (for input fields)
        targetText.text = newText;
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        if (sourceText != null && sourceText.GetComponent<TMP_InputField>() != null)
        {
            sourceText.GetComponent<TMP_InputField>().onValueChanged.RemoveListener(UpdateTargetText);
        }
    }
}