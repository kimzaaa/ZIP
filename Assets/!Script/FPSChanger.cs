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

        fpsDropdown.value = 3;
        ChangeFPS(fpsDropdown.value);

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