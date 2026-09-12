using System;
using PoolTable.Presentation.Audio;
using UnityEngine;

namespace PoolTable.Presentation
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PoolTableSceneCompositionRoot : MonoBehaviour
    {
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private Transform ballsRoot;

        public SoundManager SoundManager => soundManager;
        public Transform BallsRoot => ballsRoot;

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

            var collisionAudioBehaviours = ballsRoot.GetComponentsInChildren<PlaySoundOnBallCollision>(true);

            if (collisionAudioBehaviours.Length == 0)
            {
                throw new InvalidOperationException("No collision-audio behaviours were found under the Balls root.");
            }

            foreach (var collisionAudio in collisionAudioBehaviours)
            {
                collisionAudio.Initialize(soundManager);
            }
        }
    }
}
