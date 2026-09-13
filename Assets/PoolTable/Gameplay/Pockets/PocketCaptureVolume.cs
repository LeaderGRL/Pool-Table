using System;
using PoolTable.Core.Shots;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Gameplay.Pockets
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PocketCaptureVolume : MonoBehaviour
    {
        [SerializeField, Range(PocketId.MinimumIndex, PocketId.MaximumIndex)]
        private int pocketIndex = PocketId.MinimumIndex;

        public event Action<PocketedBall> BallCaptured;

        public PocketId Pocket => new PocketId(pocketIndex);

        public SphereCollider TriggerCollider => GetComponent<SphereCollider>();

        public bool TryCapture(BallPocketCapture ballCapture)
        {
            if (ballCapture == null || !ballCapture.TryCapture(Pocket, out var observation))
            {
                return false;
            }

            BallCaptured?.Invoke(observation);
            return true;
        }

        private void Awake()
        {
            ConfigureTrigger();
        }

        private void Reset()
        {
            ConfigureTrigger();
        }

        private void OnValidate()
        {
            ConfigureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            var rigidbody = other.attachedRigidbody;
            if (rigidbody == null)
            {
                return;
            }

            TryCapture(rigidbody.GetComponent<BallPocketCapture>());
        }

        private void ConfigureTrigger()
        {
            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                return;
            }

            trigger.isTrigger = true;
            trigger.radius = BilliardsPhysicalSpecification.PocketCaptureRadiusMeters;
        }
    }
}
