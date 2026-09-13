using UnityEngine;

namespace PoolTable.Physics.Cloth
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallClothResistance : MonoBehaviour
    {
        private const float MinimumSupportingNormalDot = 0.7f;

        private Rigidbody _rigidbody;
        private bool _hasSupportingContact;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_hasSupportingContact)
            {
                _rigidbody.linearVelocity = ClothResistanceModel.CalculateVelocityAfterStep(
                    _rigidbody.linearVelocity,
                    Time.fixedDeltaTime);
            }

            _hasSupportingContact = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            RecordSupportingContact(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            RecordSupportingContact(collision);
        }

        private void RecordSupportingContact(Collision collision)
        {
            if (!collision.collider.TryGetComponent<ClothSurface>(out _))
            {
                return;
            }

            for (var index = 0; index < collision.contactCount; index++)
            {
                if (Vector3.Dot(collision.GetContact(index).normal, Vector3.up) >= MinimumSupportingNormalDot)
                {
                    _hasSupportingContact = true;
                    return;
                }
            }
        }
    }
}
