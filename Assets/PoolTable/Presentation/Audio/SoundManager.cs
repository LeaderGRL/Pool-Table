using System.Collections.Generic;
using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public sealed class SoundManager : MonoBehaviour
    {
        private const float MinimumPlaybackPitchMagnitude = 0.01f;

        private sealed class SoundEffectVoice
        {
            public SoundEffectVoice(AudioSource source)
            {
                Source = source;
            }

            public AudioSource Source { get; }

            public double BusyUntilDspTime { get; set; }
        }

        [SerializeField] private AudioSource SFX_SoundEffect;
        private readonly List<SoundEffectVoice> soundEffectVoices = new();

        public AudioSource SoundEffectSource => SFX_SoundEffect;

        public void PlaySoundEffect(AudioClip clip, float volume)
        {
            PlaySoundEffect(clip, volume, 1f);
        }

        public void PlaySoundEffect(AudioClip clip, float volume, float pitch)
        {
            var voice = AcquireSoundEffectVoice();
            SynchronizeSoundEffectVoice(voice.Source);
            voice.Source.pitch = pitch;
            voice.Source.PlayOneShot(clip, volume);

            var playbackPitchMagnitude = Mathf.Max(
                Mathf.Abs(pitch),
                MinimumPlaybackPitchMagnitude);
            voice.BusyUntilDspTime = AudioSettings.dspTime + (clip.length / playbackPitchMagnitude);
        }

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            SFX_SoundEffect.volume = volume;
            for (var index = 1; index < soundEffectVoices.Count; index++)
            {
                soundEffectVoices[index].Source.volume = volume;
            }
        }

        public void SetMusicVolume(float volume)
        {
        }

        public AudioSource GetSFXSoundEffect()
        {
            return SFX_SoundEffect;
        }

        private SoundEffectVoice AcquireSoundEffectVoice()
        {
            EnsurePrimarySoundEffectVoice();

            var dspTime = AudioSettings.dspTime;
            for (var index = 0; index < soundEffectVoices.Count; index++)
            {
                if (soundEffectVoices[index].BusyUntilDspTime <= dspTime)
                {
                    return soundEffectVoices[index];
                }
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.Stop();

            var voice = new SoundEffectVoice(source);
            soundEffectVoices.Add(voice);
            return voice;
        }

        private void EnsurePrimarySoundEffectVoice()
        {
            if (soundEffectVoices.Count == 0)
            {
                soundEffectVoices.Add(new SoundEffectVoice(SFX_SoundEffect));
                return;
            }

            if (soundEffectVoices[0].Source == SFX_SoundEffect)
            {
                return;
            }

            soundEffectVoices.Clear();
            soundEffectVoices.Add(new SoundEffectVoice(SFX_SoundEffect));
        }

        private void SynchronizeSoundEffectVoice(AudioSource voice)
        {
            if (voice == SFX_SoundEffect)
            {
                return;
            }

            voice.outputAudioMixerGroup = SFX_SoundEffect.outputAudioMixerGroup;
            voice.mute = SFX_SoundEffect.mute;
            voice.bypassEffects = SFX_SoundEffect.bypassEffects;
            voice.bypassListenerEffects = SFX_SoundEffect.bypassListenerEffects;
            voice.bypassReverbZones = SFX_SoundEffect.bypassReverbZones;
            voice.priority = SFX_SoundEffect.priority;
            voice.volume = SFX_SoundEffect.volume;
            voice.panStereo = SFX_SoundEffect.panStereo;
            voice.spatialBlend = SFX_SoundEffect.spatialBlend;
            voice.reverbZoneMix = SFX_SoundEffect.reverbZoneMix;
            voice.dopplerLevel = SFX_SoundEffect.dopplerLevel;
            voice.spread = SFX_SoundEffect.spread;
            voice.minDistance = SFX_SoundEffect.minDistance;
            voice.maxDistance = SFX_SoundEffect.maxDistance;
            voice.rolloffMode = SFX_SoundEffect.rolloffMode;
            voice.spatialize = SFX_SoundEffect.spatialize;
            voice.spatializePostEffects = SFX_SoundEffect.spatializePostEffects;
        }
    }
}
