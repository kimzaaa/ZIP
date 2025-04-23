using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonHoverEffect : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText; // Reference to the TMP text component
    [SerializeField] private Image buttonImage; // Reference to the button's image component
    [SerializeField] private float textSizeIncrease = 10f; // Amount to increase text size on hover
    [SerializeField] private float imageScaleIncrease = 0.1f; // Amount to increase image vertical scale on hover

    private float originalTextSize; // Original font size of the text
    private Vector3 originalImageScale; // Original scale of the image

    void Start()
    {
        // Store original sizes
        if (buttonText != null)
            originalTextSize = buttonText.fontSize;
        if (buttonImage != null)
            originalImageScale = buttonImage.transform.localScale;

        // Add EventTrigger component if not already present
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        // Set up PointerEnter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => { OnHoverEnter(); });
        trigger.triggers.Add(pointerEnter);

        // Set up PointerExit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener((data) => { OnHoverExit(); });
        trigger.triggers.Add(pointerExit);
    }

    void OnHoverEnter()
    {
        // Increase text size
        if (buttonText != null)
            buttonText.fontSize = originalTextSize + textSizeIncrease;
            buttonImage.transform.localScale = new Vector3(
                originalImageScale.x,
                originalImageScale.y + imageScaleIncrease,
                originalImageScale.z
            );

        // Increase image vertical scale (up and down)
        
    }

    void OnHoverExit()
    {
        // Revert text size
        if (buttonText != null)
            buttonText.fontSize = originalTextSize;
            buttonImage.transform.localScale = originalImageScale;
        // Revert image scale
        
            
    }
}