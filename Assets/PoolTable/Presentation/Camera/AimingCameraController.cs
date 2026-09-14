using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    [DisallowMultipleComponent]
    public sealed class AimingCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.2f, -4.5f);
        [SerializeField] private float followSpeed = 8f;

        public Transform Target => target;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-followSpeed * Time.deltaTime));

            transform.LookAt(target);
        }
    }
}
