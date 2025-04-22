using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    public Sound[] musicSounds, sfxSounds;
    public AudioSource musicSource, sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // PlayMusic();
    }

    public void PlayMusic(string name)
    {
       Sound s = Array.Find(musicSounds, sound => sound.name == name);

       if (s == null)
       {
           Debug.Log("sound doesn't exist");
       }
       else
       {
           musicSource.clip = s.clip;
           musicSource.Play();
       }
    }
    
    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.Log("sound doesn't exist");
        }
        else
        {
            sfxSource.PlayOneShot(s.clip);
        }  
    }

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }
    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
    }

    public void MusicVolume(float volume)
    {
        musicSource.volume = volume;
    }
    public void SFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}
