using UnityEngine;
using TMPro;

public class FPSChanger : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown fpsDropdown;

    private void Start()
    {
        if (fpsDropdown == null)
        {
            fpsDropdown = GetComponent<TMP_Dropdown>();
        }

        SetupDropdown();

        // Initialize dropdown to match current target frame rate
        int currentFPS = Application.targetFrameRate;
        int[] fpsValues = { 15, 30, 45, 60, 120 };
        int dropdownIndex = System.Array.IndexOf(fpsValues, currentFPS);

        // If currentFPS is not in fpsValues, default to 60 FPS (index 3)
        if (dropdownIndex == -1)
        {
            dropdownIndex = 3; // Default to 60 FPS
            Application.targetFrameRate = fpsValues[dropdownIndex];
        }

        fpsDropdown.value = dropdownIndex;
        ChangeFPS(dropdownIndex);

        fpsDropdown.onValueChanged.AddListener(delegate { ChangeFPS(fpsDropdown.value); });
    }

    private void SetupDropdown()
    {
        fpsDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("15 FPS"),
            new TMP_Dropdown.OptionData("30 FPS"),
            new TMP_Dropdown.OptionData("45 FPS"),
            new TMP_Dropdown.OptionData("60 FPS"),
            new TMP_Dropdown.OptionData("120 FPS")
        };

        fpsDropdown.AddOptions(options);
    }

    private void ChangeFPS(int dropdownIndex)
    {
        int[] fpsValues = { 15, 30, 45, 60, 120 };

        int targetFPS = fpsValues[dropdownIndex];
        Application.targetFrameRate = targetFPS;

        Debug.Log($"Target FPS set to: {targetFPS}");
    }

    private void OnDestroy()
    {
        fpsDropdown.onValueChanged.RemoveAllListeners();
    }
}