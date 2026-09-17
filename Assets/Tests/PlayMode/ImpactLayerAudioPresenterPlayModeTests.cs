using System.Reflection;
using NUnit.Framework;
using PoolTable.Gameplay.Feedback;
using PoolTable.Gameplay.Shots;
using PoolTable.Presentation.Audio;
using UnityEngine;

namespace PoolTable.Tests.PlayMode
{
    public sealed class ImpactLayerAudioPresenterPlayModeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int OverlapTestClipLengthSamples = 441000;
        private GameObject soundManagerObject;
        private AudioClip railClip;
        private AudioClip pocketClip;
        private AudioClip cueClip;

        [TearDown]
        public void TearDown()
        {
            if (soundManagerObject != null)
            {
                Object.DestroyImmediate(soundManagerObject);
            }

            Object.DestroyImmediate(railClip);
            Object.DestroyImmediate(pocketClip);
            Object.DestroyImmediate(cueClip);
        }

        [Test]
        public void Presenter_PreservesDistinctPitchForOverlappingImpactLayers()
        {
            var soundManager = CreateSoundManager(out var audioSource);
            railClip = CreateClip("Rail impact");
            pocketClip = CreateClip("Pocket capture");
            cueClip = CreateClip("Cue strike");
            var presenter = new PoolTableImpactAudioPresenter(
                soundManager,
                railClip,
                pocketClip,
                cueClip);

            presenter.PlayRailImpact(new RailImpactObservation(0.12f, 2f, 0.2f));
            Assert.That(audioSource.pitch, Is.EqualTo(0.92f).Within(0.0001f));

            presenter.PlayPocketCapture(default);
            presenter.PlayCueStrike(new CueStrikeObservation(1f, 6.6666667f));

            var voices = soundManagerObject.GetComponents<AudioSource>();
            Assert.That(voices, Has.Length.EqualTo(3));
            Assert.That(voices[0].pitch, Is.EqualTo(0.92f).Within(0.0001f));
            Assert.That(voices[1].pitch, Is.EqualTo(0.78f).Within(0.0001f));
            Assert.That(voices[2].pitch, Is.EqualTo(0.94f).Within(0.0001f));
        }

        private SoundManager CreateSoundManager(out AudioSource audioSource)
        {
            soundManagerObject = new GameObject("Impact layer sound manager");
            audioSource = soundManagerObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var soundManager = soundManagerObject.AddComponent<SoundManager>();
            var field = typeof(SoundManager).GetField("SFX_SoundEffect", PrivateInstance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(soundManager, audioSource);
            return soundManager;
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, OverlapTestClipLengthSamples, 1, 44100, false);
        }
    }
}
