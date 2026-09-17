using PoolTable.Core.Shots;
using PoolTable.Gameplay.Feedback;
using PoolTable.Gameplay.Shots;
using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public sealed class PoolTableImpactAudioPresenter
    {
        private readonly SoundManager soundManager;
        private readonly AudioClip railImpactClip;
        private readonly AudioClip pocketCaptureClip;
        private readonly AudioClip cueImpactClip;

        public PoolTableImpactAudioPresenter(
            SoundManager soundManager,
            AudioClip railImpactClip,
            AudioClip pocketCaptureClip,
            AudioClip cueImpactClip)
        {
            this.soundManager = soundManager != null
                ? soundManager
                : throw new System.ArgumentNullException(nameof(soundManager));
            this.railImpactClip = railImpactClip != null
                ? railImpactClip
                : throw new System.ArgumentNullException(nameof(railImpactClip));
            this.pocketCaptureClip = pocketCaptureClip != null
                ? pocketCaptureClip
                : throw new System.ArgumentNullException(nameof(pocketCaptureClip));
            this.cueImpactClip = cueImpactClip != null
                ? cueImpactClip
                : throw new System.ArgumentNullException(nameof(cueImpactClip));
        }

        internal void PlayRailImpact(RailImpactObservation observation)
        {
            Play(railImpactClip, ImpactLayerAudioModel.EvaluateRail(observation.ImpactEnergyJoules));
        }

        internal void PlayPocketCapture(PocketedBall pocketedBall)
        {
            Play(pocketCaptureClip, ImpactLayerAudioModel.PocketCapture);
        }

        internal void PlayCueStrike(CueStrikeObservation observation)
        {
            Play(cueImpactClip, ImpactLayerAudioModel.EvaluateCueStrike(observation.NormalizedPower));
        }

        private void Play(AudioClip clip, ImpactLayerAudioCue cue)
        {
            if (!cue.ShouldPlay)
            {
                return;
            }

            soundManager.PlaySoundEffect(clip, cue.Volume, cue.Pitch);
        }
    }
}
