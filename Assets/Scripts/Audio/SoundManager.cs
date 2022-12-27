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

    public void setMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void setSFXVolume(float volume)
    {
        SFX_SoundEffect.volume = volume;
    }

    public void setMusicVolume(float volume)
    {
        
    }

}
