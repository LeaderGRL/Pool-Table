using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Gameplay.Balls;
using PoolTable.Presentation.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    public sealed class BallImpactAudioPlayModeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator TypedBallImpact_TriggersCollisionAudio()
        {
            var soundManager = CreateSoundManager(out var audioSource);
            var clip = CreateClip();
            var firstBall = CreateBall("Ball 0", 0, new Vector3(-0.051f, 0f, 0f), soundManager, clip);
            CreateBall("Ball 1", 1, new Vector3(0.051f, 0f, 0f), soundManager, clip);

            audioSource.pitch = 0.5f;
            firstBall.GetComponent<Rigidbody>().linearVelocity = Vector3.right;

            yield return WaitForPhysicsSteps(8);

            Assert.That(audioSource.pitch, Is.InRange(0.9f, 1.1f));
        }

        [UnityTest]
        public IEnumerator VeryWeakTypedBallImpact_RemainsSilent()
        {
            var soundManager = CreateSoundManager(out var audioSource);
            var clip = CreateClip();
            var firstBall = CreateBall("Ball 0", 0, new Vector3(-0.0502f, 0f, 0f), soundManager, clip);
            CreateBall("Ball 1", 1, new Vector3(0.0502f, 0f, 0f), soundManager, clip);

            audioSource.pitch = 0.5f;
            firstBall.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 0.05f;

            yield return WaitForPhysicsSteps(8);

            Assert.That(audioSource.pitch, Is.EqualTo(0.5f));
        }

        [UnityTest]
        public IEnumerator NonBallCollision_DoesNotTriggerBallImpactAudio()
        {
            var soundManager = CreateSoundManager(out var audioSource);
            var clip = CreateClip();
            var ball = CreateBall("Ball 0", 0, new Vector3(-0.051f, 0f, 0f), soundManager, clip);
            CreateNonBallCollider(new Vector3(0.051f, 0f, 0f));

            audioSource.pitch = 0.5f;
            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.right;

            yield return WaitForPhysicsSteps(8);

            Assert.That(audioSource.pitch, Is.EqualTo(0.5f));
        }

        private SoundManager CreateSoundManager(out AudioSource audioSource)
        {
            var gameObject = Track(new GameObject("Sound Manager"));
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var soundManager = gameObject.AddComponent<SoundManager>();
            SetPrivateField(soundManager, "SFX_SoundEffect", audioSource);
            return soundManager;
        }

        private AudioClip CreateClip()
        {
            var clip = AudioClip.Create("Test ball impact", 128, 1, 44100, false);
            createdObjects.Add(clip);
            return clip;
        }

        private GameObject CreateBall(
            string name,
            int ballNumber,
            Vector3 position,
            SoundManager soundManager,
            AudioClip clip)
        {
            var gameObject = Track(new GameObject(name));
            gameObject.transform.position = position;

            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.mass = 0.17f;
            rigidbody.useGravity = false;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.05f;

            var identity = gameObject.AddComponent<BallIdentity>();
            SetPrivateField(identity, "ballNumber", ballNumber);

            var collisionAudio = gameObject.AddComponent<PlaySoundOnBallCollision>();
            SetPrivateField(collisionAudio, "SFX_BallCollision", new[] { clip, clip, clip });
            collisionAudio.Initialize(soundManager);

            return gameObject;
        }

        private GameObject CreateNonBallCollider(Vector3 position)
        {
            var gameObject = Track(new GameObject("Non-ball collider"));
            gameObject.transform.position = position;

            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.05f;
            return gameObject;
        }

        private static IEnumerator WaitForPhysicsSteps(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private T Track<T>(T createdObject)
            where T : Object
        {
            createdObjects.Add(createdObject);
            return createdObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
