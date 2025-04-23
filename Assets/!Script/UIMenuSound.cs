using UnityEngine;
using UnityEngine.UI;

public class UIMenuSound : MonoBehaviour
{
    public Slider musicSlider, sfxSlider;

    private void Start()
    {
        // Set sliders to current volume levels at start
        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }

    public void MusicVolume()
    {
        AudioManager.Instance.MusicVolume(musicSlider.value);
    }

    public void SFXVolume()
    {
        AudioManager.Instance.SFXVolume(sfxSlider.value);
    }
}