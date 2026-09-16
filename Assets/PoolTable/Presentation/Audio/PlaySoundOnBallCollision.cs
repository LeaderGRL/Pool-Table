using System;
using PoolTable.Gameplay.Balls;
using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    [RequireComponent(typeof(BallIdentity), typeof(Rigidbody))]
    public sealed class PlaySoundOnBallCollision : MonoBehaviour
    {
        [SerializeField] private AudioClip[] SFX_BallCollision;

        private BallIdentity ballIdentity;
        private Rigidbody ballRigidbody;

        public SoundManager SoundManager { get; private set; }

        private void Awake()
        {
            ballIdentity = GetComponent<BallIdentity>();
            ballRigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(SoundManager soundManager)
        {
            SoundManager = soundManager != null
                ? soundManager
                : throw new ArgumentNullException(nameof(soundManager));
        }

        private void OnCollisionEnter(Collision collision)
        {
            var otherBallIdentity = collision.gameObject.GetComponent<BallIdentity>();
            if (otherBallIdentity == null
                || !BallImpactAudioModel.IsPrimaryEmitter(ballIdentity.Id, otherBallIdentity.Id))
            {
                return;
            }

            if (SoundManager == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PlaySoundOnBallCollision)} on {name} was not initialized by the scene composition root.");
            }

            if (SFX_BallCollision == null || SFX_BallCollision.Length == 0 || collision.rigidbody == null)
            {
                return;
            }

            var normalClosingSpeedMetersPerSecond = 0f;
            for (var contactIndex = 0; contactIndex < collision.contactCount; contactIndex++)
            {
                normalClosingSpeedMetersPerSecond = Mathf.Max(
                    normalClosingSpeedMetersPerSecond,
                    BallImpactAudioModel.CalculateNormalClosingSpeed(
                        collision.relativeVelocity,
                        collision.GetContact(contactIndex).normal));
            }

            var cue = BallImpactAudioModel.Evaluate(
                normalClosingSpeedMetersPerSecond,
                ballRigidbody.mass,
                collision.rigidbody.mass,
                SFX_BallCollision.Length);

            if (!cue.ShouldPlay)
            {
                return;
            }

            var clip = SFX_BallCollision[cue.ClipIndex];
            if (clip == null)
            {
                return;
            }

            SoundManager.SetPitch(UnityEngine.Random.Range(0.9f, 1.1f));
            SoundManager.PlaySoundEffect(clip, cue.Volume);
        }
    }
}
