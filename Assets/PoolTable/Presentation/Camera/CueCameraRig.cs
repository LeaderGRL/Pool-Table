using UnityEngine;

namespace PoolTable.Presentation.Camera
{
    internal static class CueCameraRig
    {
        internal static bool TryGetBasis(
            Vector3 strikeDirection,
            out Vector3 forward,
            out Vector3 up)
        {
            forward = strikeDirection.normalized;
            up = default;

            if (forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            right.Normalize();
            up = Vector3.Cross(forward, right).normalized;
            return up.sqrMagnitude > 0.000001f;
        }

        internal static Vector3 GetCameraPosition(
            Vector3 cueBallPosition,
            Vector3 cueForward,
            Vector3 cueUp,
            float distanceBehindCueBall,
            float heightAboveCueAxis)
        {
            return cueBallPosition
                - (cueForward * distanceBehindCueBall)
                + (cueUp * heightAboveCueAxis);
        }

        internal static Vector3 GetFocusPoint(
            Vector3 cueBallPosition,
            Vector3 cueForward,
            Vector3 cueUp,
            float lookAheadDistance,
            float targetHeightOffset)
        {
            return cueBallPosition
                + (cueForward * lookAheadDistance)
                + (cueUp * targetHeightOffset);
        }
    }
}
