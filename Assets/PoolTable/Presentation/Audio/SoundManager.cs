using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public sealed class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource SFX_SoundEffect;

        public AudioSource SoundEffectSource => SFX_SoundEffect;

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
}
