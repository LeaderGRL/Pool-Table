using System.Collections.Generic;
using PoolTable.Physics.Configuration;
using UnityEngine;

namespace PoolTable.Physics.Rails
{
    internal readonly struct RailCollisionResponse
    {
        internal RailCollisionResponse(Vector3 linearVelocity, Vector3 angularVelocity, bool wasApplied)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            WasApplied = wasApplied;
        }

        internal Vector3 LinearVelocity { get; }

        internal Vector3 AngularVelocity { get; }

        internal bool WasApplied { get; }
    }

    internal static class RailCollisionResponseModel
    {
        private const float SolidSphereInertiaFactor = 0.4f;
        private const float MinimumPlanarNormalSquaredMagnitude = 0.000001f;
        private const float MinimumApproachSpeedMetersPerSecond = 0.000001f;
        private const int MaximumManifoldSolverPasses = 8;

        internal static RailCollisionResponse CalculateResponse(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            Vector3 railNormal,
            float ballRadiusMeters)
        {
            var unchanged = new RailCollisionResponse(linearVelocity, angularVelocity, false);
            if (!IsFinite(linearVelocity)
                || !IsFinite(angularVelocity)
                || !IsFinite(railNormal)
                || !IsFinite(ballRadiusMeters)
                || ballRadiusMeters <= 0f)
            {
                return unchanged;
            }

            var planarNormal = new Vector3(railNormal.x, 0f, railNormal.z);
            if (planarNormal.sqrMagnitude < MinimumPlanarNormalSquaredMagnitude)
            {
                return unchanged;
            }

            planarNormal.Normalize();
            var planarLinearVelocity = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
            var incomingNormalSpeed = Vector3.Dot(planarLinearVelocity, planarNormal);
            if (incomingNormalSpeed >= -MinimumApproachSpeedMetersPerSecond)
            {
                return unchanged;
            }

            var normalImpulseSpeed =
                -(1f + BilliardsSimulationConfiguration.RailNormalRestitution) * incomingNormalSpeed;
            var nextPlanarVelocity = planarLinearVelocity + (planarNormal * normalImpulseSpeed);

            var contactOffset = -planarNormal * ballRadiusMeters;
            var railTangent = Vector3.Cross(planarNormal, Vector3.up);
            var sideSpinContactVelocity = Vector3.Cross(
                Vector3.up * angularVelocity.y,
                contactOffset);
            var tangentialContactSpeed = Vector3.Dot(
                planarLinearVelocity + sideSpinContactVelocity,
                railTangent);

            var effectiveTangentialInverseMassFactor = 1f + (1f / SolidSphereInertiaFactor);
            var unconstrainedTangentialImpulseSpeed =
                -tangentialContactSpeed / effectiveTangentialInverseMassFactor;
            var maximumTangentialImpulseSpeed =
                BilliardsSimulationConfiguration.RailTangentialFrictionCoefficient * normalImpulseSpeed;
            var tangentialImpulseSpeed = Mathf.Clamp(
                unconstrainedTangentialImpulseSpeed,
                -maximumTangentialImpulseSpeed,
                maximumTangentialImpulseSpeed);
            var tangentialVelocityChange = railTangent * tangentialImpulseSpeed;
            nextPlanarVelocity += tangentialVelocityChange;

            var inverseInertiaPerUnitMass =
                1f / (SolidSphereInertiaFactor * ballRadiusMeters * ballRadiusMeters);
            var angularVelocityChange =
                Vector3.Cross(contactOffset, tangentialVelocityChange) * inverseInertiaPerUnitMass;
            var nextAngularVelocity = angularVelocity + angularVelocityChange;
            var nextLinearVelocity = new Vector3(
                nextPlanarVelocity.x,
                linearVelocity.y,
                nextPlanarVelocity.z);

            return new RailCollisionResponse(nextLinearVelocity, nextAngularVelocity, true);
        }

        internal static RailCollisionResponse CalculateManifoldResponse(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            IReadOnlyList<Vector3> railNormals,
            float ballRadiusMeters)
        {
            var unchanged = new RailCollisionResponse(linearVelocity, angularVelocity, false);
            if (railNormals == null || railNormals.Count == 0)
            {
                return unchanged;
            }

            var distinctNormals = new List<Vector3>(railNormals.Count);
            for (var index = 0; index < railNormals.Count; index++)
            {
                var planarNormal = new Vector3(railNormals[index].x, 0f, railNormals[index].z);
                if (!IsFinite(planarNormal)
                    || planarNormal.sqrMagnitude < MinimumPlanarNormalSquaredMagnitude)
                {
                    continue;
                }

                planarNormal.Normalize();
                if (!ContainsEquivalentNormal(distinctNormals, planarNormal))
                {
                    distinctNormals.Add(planarNormal);
                }
            }

            SortByOpposition(distinctNormals, linearVelocity);

            var currentLinearVelocity = linearVelocity;
            var currentAngularVelocity = angularVelocity;
            var wasApplied = false;
            for (var index = 0; index < distinctNormals.Count; index++)
            {
                var response = CalculateResponse(
                    currentLinearVelocity,
                    currentAngularVelocity,
                    distinctNormals[index],
                    ballRadiusMeters);
                if (!response.WasApplied)
                {
                    continue;
                }

                currentLinearVelocity = response.LinearVelocity;
                currentAngularVelocity = response.AngularVelocity;
                wasApplied = true;
            }

            for (var pass = 0; pass < MaximumManifoldSolverPasses; pass++)
            {
                var adjustedDuringPass = false;
                for (var index = 0; index < distinctNormals.Count; index++)
                {
                    var normal = distinctNormals[index];
                    var normalSpeed = Vector3.Dot(currentLinearVelocity, normal);
                    if (normalSpeed >= -MinimumApproachSpeedMetersPerSecond)
                    {
                        continue;
                    }

                    // Stabilization only removes renewed penetration. Restitution and tangential
                    // friction were already applied once for this contact manifold above.
                    currentLinearVelocity -= normal * normalSpeed;
                    adjustedDuringPass = true;
                }

                if (!adjustedDuringPass)
                {
                    break;
                }
            }

            if (HasApproachingNormal(currentLinearVelocity, distinctNormals))
            {
                currentLinearVelocity = ProjectOntoSeparatingCone(currentLinearVelocity, distinctNormals);
            }

            return new RailCollisionResponse(currentLinearVelocity, currentAngularVelocity, wasApplied);
        }

        private static bool HasApproachingNormal(Vector3 linearVelocity, List<Vector3> normals)
        {
            for (var index = 0; index < normals.Count; index++)
            {
                if (Vector3.Dot(linearVelocity, normals[index]) < -MinimumApproachSpeedMetersPerSecond)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 ProjectOntoSeparatingCone(Vector3 linearVelocity, List<Vector3> normals)
        {
            var planarVelocity = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
            var bestPlanarVelocity = Vector3.zero;
            var bestDistanceSquared = planarVelocity.sqrMagnitude;

            for (var index = 0; index < normals.Count; index++)
            {
                var normal = normals[index];
                var candidate = planarVelocity - (normal * Vector3.Dot(planarVelocity, normal));
                if (HasApproachingNormal(candidate, normals))
                {
                    continue;
                }

                var distanceSquared = (candidate - planarVelocity).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestPlanarVelocity = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return new Vector3(bestPlanarVelocity.x, linearVelocity.y, bestPlanarVelocity.z);
        }

        private static bool ContainsEquivalentNormal(List<Vector3> normals, Vector3 candidate)
        {
            const float equivalentNormalDotThreshold = 0.9999f;
            for (var index = 0; index < normals.Count; index++)
            {
                if (Vector3.Dot(normals[index], candidate) >= equivalentNormalDotThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SortByOpposition(List<Vector3> normals, Vector3 incomingLinearVelocity)
        {
            for (var index = 1; index < normals.Count; index++)
            {
                var candidate = normals[index];
                var insertionIndex = index;
                while (insertionIndex > 0
                       && CompareNormals(candidate, normals[insertionIndex - 1], incomingLinearVelocity) < 0)
                {
                    normals[insertionIndex] = normals[insertionIndex - 1];
                    insertionIndex--;
                }

                normals[insertionIndex] = candidate;
            }
        }

        private static int CompareNormals(Vector3 left, Vector3 right, Vector3 incomingLinearVelocity)
        {
            var leftDot = Vector3.Dot(incomingLinearVelocity, left);
            var rightDot = Vector3.Dot(incomingLinearVelocity, right);
            var dotComparison = leftDot.CompareTo(rightDot);
            if (dotComparison != 0)
            {
                return dotComparison;
            }

            var xComparison = left.x.CompareTo(right.x);
            return xComparison != 0 ? xComparison : left.z.CompareTo(right.z);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
