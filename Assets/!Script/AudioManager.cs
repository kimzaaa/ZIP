using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    public Sound[] musicSounds, sfxSounds;
    public Sound[] FootstepSounds, packageSentSound;
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
        PlayMusic("FreeBird");
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

    public void PlayRandomFootstep()
    {
        if (FootstepSounds.Length == 0)
        {
            Debug.Log("No footstep sounds available");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, FootstepSounds.Length);
        Sound s = FootstepSounds[randomIndex];

        if (s == null || s.clip == null)
        {
            Debug.Log("Invalid footstep sound at index: " + randomIndex);
        }
        else
        {
            sfxSource.PlayOneShot(s.clip,0.35f);
        }
    }
    
    public void PlayRandomPackageSentSound()
    {
        if (packageSentSound.Length == 0)
        {
            Debug.Log("No sent sounds available");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, packageSentSound.Length);
        Sound s = packageSentSound[randomIndex];

        if (s == null || s.clip == null)
        {
            Debug.Log("Invalid packageSent sound at index: " + randomIndex);
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
    
    public float GetMusicVolume()
    {
        return musicSource.volume;
    }

    public float GetSFXVolume()
    {
        return sfxSource.volume;
    }
}