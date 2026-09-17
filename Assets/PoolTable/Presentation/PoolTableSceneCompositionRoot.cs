using System;
using PoolTable.Gameplay.Feedback;
using PoolTable.Gameplay.Instrumentation;
using PoolTable.Gameplay.Shots;
using PoolTable.Presentation.Audio;
using PoolTable.Presentation.Camera;
using PoolTable.Presentation.Vfx;
using UnityEngine;

namespace PoolTable.Presentation
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PoolTableSceneCompositionRoot : MonoBehaviour
    {
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private Transform ballsRoot;
        [SerializeField] private Transform pocketCaptureVolumesRoot;
        [SerializeField] private ShotPowerController shotPowerController;
        [SerializeField] private ShotSimulationInstrumentation shotSimulationInstrumentation;
        [SerializeField] private AudioClip railImpactClip;
        [SerializeField] private AudioClip pocketCaptureClip;
        [SerializeField] private AudioClip cueImpactClip;
        [SerializeField] private Shader cueImpactParticleShader;

        private PoolTableImpactEventSource impactEventSource;
        private PoolTableImpactAudioPresenter impactAudioPresenter;
        private CueImpactVfxPresenter cueImpactVfxPresenter;
        private PocketCaptureVfxPresenter pocketCaptureVfxPresenter;
        private CameraImpactImpulseController cameraImpactImpulseController;

        public SoundManager SoundManager => soundManager;
        public Transform BallsRoot => ballsRoot;
        public Transform PocketCaptureVolumesRoot => pocketCaptureVolumesRoot;
        public ShotPowerController ShotPowerController => shotPowerController;
        public ShotSimulationInstrumentation ShotSimulationInstrumentation => shotSimulationInstrumentation;
        public AudioClip RailImpactClip => railImpactClip;
        public AudioClip PocketCaptureClip => pocketCaptureClip;
        public AudioClip CueImpactClip => cueImpactClip;
        public Shader CueImpactParticleShader => cueImpactParticleShader;
        public CueImpactVfxPresenter CueImpactVfxPresenter => cueImpactVfxPresenter;
        public PocketCaptureVfxPresenter PocketCaptureVfxPresenter => pocketCaptureVfxPresenter;
        public CameraImpactImpulseController CameraImpactImpulseController => cameraImpactImpulseController;

        private void Awake()
        {
            if (soundManager == null)
            {
                throw new InvalidOperationException("The scene composition root requires a SoundManager reference.");
            }

            if (ballsRoot == null)
            {
                throw new InvalidOperationException("The scene composition root requires the Balls root reference.");
            }

            if (pocketCaptureVolumesRoot == null)
            {
                throw new InvalidOperationException("The scene composition root requires the pocket capture-volumes root reference.");
            }

            if (shotPowerController == null)
            {
                throw new InvalidOperationException("The scene composition root requires the active ShotPowerController reference.");
            }

            if (shotSimulationInstrumentation == null)
            {
                throw new InvalidOperationException("The scene composition root requires shot simulation instrumentation.");
            }

            if (railImpactClip == null || pocketCaptureClip == null || cueImpactClip == null)
            {
                throw new InvalidOperationException("The scene composition root requires rail, pocket, and cue impact clips.");
            }

            if (cueImpactParticleShader == null)
            {
                throw new InvalidOperationException("The scene composition root requires the cue-impact particle shader.");
            }

            var collisionAudioBehaviours = ballsRoot.GetComponentsInChildren<PlaySoundOnBallCollision>(true);

            if (collisionAudioBehaviours.Length == 0)
            {
                throw new InvalidOperationException("No collision-audio behaviours were found under the Balls root.");
            }

            foreach (var collisionAudio in collisionAudioBehaviours)
            {
                collisionAudio.Initialize(soundManager);
            }

            impactEventSource = new PoolTableImpactEventSource(
                ballsRoot,
                pocketCaptureVolumesRoot,
                shotPowerController);
            impactAudioPresenter = new PoolTableImpactAudioPresenter(
                soundManager,
                railImpactClip,
                pocketCaptureClip,
                cueImpactClip);
            var outputCamera = UnityEngine.Camera.main;
            if (outputCamera == null)
            {
                throw new InvalidOperationException("The scene composition root requires a tagged Main Camera.");
            }

            cameraImpactImpulseController = outputCamera.GetComponent<CameraImpactImpulseController>();
            if (cameraImpactImpulseController == null)
            {
                cameraImpactImpulseController = outputCamera.gameObject.AddComponent<CameraImpactImpulseController>();
            }

            cueImpactVfxPresenter = new CueImpactVfxPresenter(transform, cueImpactParticleShader);
            pocketCaptureVfxPresenter = new PocketCaptureVfxPresenter(transform, cueImpactParticleShader);
            impactEventSource.RailImpactObserved += impactAudioPresenter.PlayRailImpact;
            impactEventSource.RailImpactObserved += cameraImpactImpulseController.PlayRailImpact;
            impactEventSource.BallPocketed += impactAudioPresenter.PlayPocketCapture;
            impactEventSource.CueStrikeApplied += impactAudioPresenter.PlayCueStrike;
            impactEventSource.CueStrikeApplied += cameraImpactImpulseController.PlayCueStrike;
            impactEventSource.CueStrikeApplied += cueImpactVfxPresenter.PlayCueStrike;
            impactEventSource.PocketCaptureObserved += pocketCaptureVfxPresenter.PlayPocketCapture;
        }

        private void OnDestroy()
        {
            if (impactEventSource != null && impactAudioPresenter != null)
            {
                impactEventSource.RailImpactObserved -= impactAudioPresenter.PlayRailImpact;
                impactEventSource.BallPocketed -= impactAudioPresenter.PlayPocketCapture;
                impactEventSource.CueStrikeApplied -= impactAudioPresenter.PlayCueStrike;
            }

            if (impactEventSource != null && cueImpactVfxPresenter != null)
            {
                impactEventSource.CueStrikeApplied -= cueImpactVfxPresenter.PlayCueStrike;
            }

            if (impactEventSource != null && cameraImpactImpulseController != null)
            {
                impactEventSource.RailImpactObserved -= cameraImpactImpulseController.PlayRailImpact;
                impactEventSource.CueStrikeApplied -= cameraImpactImpulseController.PlayCueStrike;
            }

            if (impactEventSource != null && pocketCaptureVfxPresenter != null)
            {
                impactEventSource.PocketCaptureObserved -= pocketCaptureVfxPresenter.PlayPocketCapture;
            }

            impactEventSource?.Dispose();
            cueImpactVfxPresenter?.Dispose();
            pocketCaptureVfxPresenter?.Dispose();
        }
    }
}
