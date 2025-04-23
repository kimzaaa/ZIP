using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResolutionChanger : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    private List<ResolutionOption> resolutionOptions;
    private const string RESOLUTION_PREF_KEY = "ResolutionSetting";

    private struct ResolutionOption
    {
        public int width;
        public int height;
        public bool isFullscreen;
        public string displayName;

        public ResolutionOption(int w, int h, bool fs, string name)
        {
            width = w;
            height = h;
            isFullscreen = fs;
            displayName = name;
        }
    }

    void Start()
    {
        InitializeResolutionOptions();
        PopulateDropdown();
        LoadSavedResolution();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void InitializeResolutionOptions()
    {
        resolutionOptions = new List<ResolutionOption>
        {
            new ResolutionOption(960, 540, false, "qHD (960x540) Window"),
            new ResolutionOption(960, 540, true, "qHD (960x540) Fullscreen"),
            new ResolutionOption(1280, 720, false, "HD (1280x720) Window"),
            new ResolutionOption(1280, 720, true, "HD (1280x720) Fullscreen"),
            new ResolutionOption(1600, 900, false, "HD+ (1600x900) Window"),
            new ResolutionOption(1600, 900, true, "HD+ (1600x900) Fullscreen"),
            new ResolutionOption(1920, 1080, false, "FHD (1920x1080) Window"),
            new ResolutionOption(1920, 1080, true, "FHD (1920x1080) Fullscreen"),
            new ResolutionOption(2560, 1440, false, "QHD (2560x1440) Window"),
            new ResolutionOption(2560, 1440, true, "QHD (2560x1440) Fullscreen"),
            new ResolutionOption(3840, 2160, false, "UHD (3840x2160) Window"),
            new ResolutionOption(3840, 2160, true, "UHD (3840x2160) Fullscreen")
        };
    }

    private void PopulateDropdown()
    {
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var option in resolutionOptions)
        {
            options.Add(option.displayName);
        }
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSavedResolution()
    {
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, 0);
        resolutionDropdown.value = savedIndex;
        ApplyResolution(savedIndex);
    }

    private void OnResolutionChanged(int index)
    {
        ApplyResolution(index);
        PlayerPrefs.SetInt(RESOLUTION_PREF_KEY, index);
        PlayerPrefs.Save();
    }

    private void ApplyResolution(int index)
    {
        ResolutionOption option = resolutionOptions[index];
        Screen.SetResolution(option.width, option.height, option.isFullscreen);
    }
}