using PoolTable.Core.Shots;
using PoolTable.Gameplay.Balls;
using UnityEngine;

namespace PoolTable.Gameplay.Pockets
{
    [RequireComponent(typeof(BallIdentity), typeof(Rigidbody))]
    public sealed class BallPocketCapture : MonoBehaviour
    {
        public bool IsCaptured { get; private set; }

        public PocketId? CapturedPocket { get; private set; }

        public bool TryCapture(PocketId pocket, out PocketedBall observation)
        {
            if (IsCaptured)
            {
                observation = default;
                return false;
            }

            var identity = GetComponent<BallIdentity>();
            var rigidbody = GetComponent<Rigidbody>();

            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.Sleep();

            observation = new PocketedBall(identity.Id, pocket);
            IsCaptured = true;
            CapturedPocket = pocket;

            gameObject.SetActive(false);
            return true;
        }

        public void Restore(Vector3 worldPosition)
        {
            transform.position = worldPosition;

            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            IsCaptured = false;
            CapturedPocket = null;
            gameObject.SetActive(true);
            rigidbody.WakeUp();
        }
    }
}
