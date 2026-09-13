using System;
using UnityEngine;

namespace PoolTable.Physics.Instrumentation
{
    public readonly struct KineticEnergyMeasurement
    {
        public KineticEnergyMeasurement(float linearJoules, float angularJoules)
        {
            LinearJoules = linearJoules;
            AngularJoules = angularJoules;
        }

        public float LinearJoules { get; }

        public float AngularJoules { get; }

        public float TotalJoules => LinearJoules + AngularJoules;
    }

    public static class KineticEnergy
    {
        private const float SolidSphereInertiaFactor = 2f / 5f;

        public static KineticEnergyMeasurement Calculate(
            float massKilograms,
            Vector3 linearVelocityMetersPerSecond,
            Vector3 angularVelocityRadiansPerSecond,
            float radiusMeters)
        {
            if (!IsFinitePositive(massKilograms))
            {
                throw new ArgumentOutOfRangeException(nameof(massKilograms), massKilograms, "Mass must be finite and positive.");
            }

            if (!IsFinitePositive(radiusMeters))
            {
                throw new ArgumentOutOfRangeException(nameof(radiusMeters), radiusMeters, "Radius must be finite and positive.");
            }

            var linearEnergy = 0.5f * massKilograms * linearVelocityMetersPerSecond.sqrMagnitude;
            var inertia = SolidSphereInertiaFactor * massKilograms * radiusMeters * radiusMeters;
            var angularEnergy = 0.5f * inertia * angularVelocityRadiansPerSecond.sqrMagnitude;
            return new KineticEnergyMeasurement(linearEnergy, angularEnergy);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
