using System;
using UnityEngine;

namespace PoolTable.Presentation.Audio
{
    public sealed class PlaySoundOnBallCollision : MonoBehaviour
    {
        [SerializeField] private AudioClip[] SFX_BallCollision;

        public SoundManager SoundManager { get; private set; }

        public void Initialize(SoundManager soundManager)
        {
            SoundManager = soundManager != null
                ? soundManager
                : throw new ArgumentNullException(nameof(soundManager));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("striped")
                && !collision.gameObject.CompareTag("filled")
                && !collision.gameObject.CompareTag("white")
                && !collision.gameObject.CompareTag("black"))
            {
                return;
            }

            if (SoundManager == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PlaySoundOnBallCollision)} on {name} was not initialized by the scene composition root.");
            }

            var collisionSpeed = collision.relativeVelocity.magnitude;
            var soundIndex = Mathf.Clamp(Mathf.RoundToInt(collisionSpeed / 10), 0, SFX_BallCollision.Length - 1);

            SoundManager.SetPitch(UnityEngine.Random.Range(0.9f, 1.1f));

            var volume = Mathf.Clamp(collisionSpeed / 10, 0.01f, 1f);
            SoundManager.PlaySoundEffect(SFX_BallCollision[soundIndex], volume);
        }
    }
}
