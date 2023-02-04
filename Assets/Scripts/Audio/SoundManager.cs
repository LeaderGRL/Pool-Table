using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource SFX_SoundEffect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySoundEffect(AudioClip clip, float volume)
    {
        SFX_SoundEffect.PlayOneShot(clip, volume);
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        SFX_SoundEffect.volume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        
    }

    public void SetPitch(float pitch)
    {
        SFX_SoundEffect.pitch = pitch;
    }

    public AudioSource GetSFXSoundEffect() 
    {
        return SFX_SoundEffect;
    }

}
