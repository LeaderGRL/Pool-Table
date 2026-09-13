using PoolTable.Core.Shots;
using PoolTable.Gameplay.Balls;
using UnityEngine;

namespace PoolTable.Gameplay.Pockets
{
    [RequireComponent(typeof(BallIdentity), typeof(Rigidbody))]
    public sealed class BallPocketCapture : MonoBehaviour
    {
        private const string LegacyPocketCaptureMessage = "OnModernPocketCaptured";

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

            // Temporary compatibility bridge while the active match flow still lives in Assembly-CSharp.
            gameObject.SendMessage(LegacyPocketCaptureMessage, SendMessageOptions.DontRequireReceiver);
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

        public void RestoreFromLegacyScratch(Vector3 worldPosition)
        {
            Restore(worldPosition);
        }
    }
}
