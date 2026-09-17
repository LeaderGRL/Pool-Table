using System;
using PoolTable.Gameplay.Shots;
using UnityEngine;
using UnityEngine.Rendering;

namespace PoolTable.Presentation.Vfx
{
    public sealed class CueImpactVfxPresenter : IDisposable
    {
        private const float GoldenAngleRadians = 2.39996323f;
        private static readonly Color ChalkColor = new(0.55f, 0.78f, 1f, 0.46f);
        private static readonly Color AccentColor = new(1f, 0.82f, 0.52f, 0.68f);

        private readonly Transform root;
        private readonly ParticleSystem chalkDustParticles;
        private readonly ParticleSystem contactAccentParticles;
        private readonly Material particleMaterial;
        private bool disposed;

        public CueImpactVfxPresenter(Transform parent, Shader particleShader)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (particleShader == null)
            {
                throw new ArgumentNullException(nameof(particleShader));
            }

            var rootObject = new GameObject("Cue Impact VFX");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;

            particleMaterial = CreateParticleMaterial(particleShader);
            chalkDustParticles = CreateParticleSystem(
                "Chalk Dust",
                root,
                particleMaterial,
                64,
                0.08f);
            contactAccentParticles = CreateParticleSystem(
                "Cue Contact Accent",
                root,
                particleMaterial,
                32,
                0f);
        }

        internal Transform Root => root;

        internal ParticleSystem ChalkDustParticles => chalkDustParticles;

        internal ParticleSystem ContactAccentParticles => contactAccentParticles;

        internal void PlayCueStrike(CueStrikeObservation observation)
        {
            if (disposed)
            {
                return;
            }

            var cue = CueImpactVfxModel.Evaluate(observation.NormalizedPower);
            if (!cue.ShouldPlay)
            {
                return;
            }

            var strikeDirection = observation.StrikeDirection.sqrMagnitude > 0.000001f
                ? observation.StrikeDirection.normalized
                : Vector3.forward;
            BuildPerpendicularBasis(strikeDirection, out var tangent, out var bitangent);

            EmitChalkDust(
                observation.ContactPointWorldPosition,
                strikeDirection,
                tangent,
                bitangent,
                cue);
            EmitContactAccent(
                observation.ContactPointWorldPosition,
                strikeDirection,
                tangent,
                bitangent,
                cue);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (root != null)
            {
                DestroyRuntimeObject(root.gameObject);
            }

            if (particleMaterial != null)
            {
                DestroyRuntimeObject(particleMaterial);
            }
        }

        private void EmitChalkDust(
            Vector3 contactPoint,
            Vector3 strikeDirection,
            Vector3 tangent,
            Vector3 bitangent,
            CueImpactVfxCue cue)
        {
            var backward = -strikeDirection;
            for (var index = 0; index < cue.ChalkParticleCount; index++)
            {
                var normalizedIndex = (index + 0.5f) / cue.ChalkParticleCount;
                var angle = index * GoldenAngleRadians;
                var radial = ((Mathf.Cos(angle) * tangent) + (Mathf.Sin(angle) * bitangent)).normalized;
                var velocityDirection = (backward * 0.55f) + (radial * 0.65f) + (Vector3.up * 0.32f);
                if (velocityDirection.y < 0.04f)
                {
                    velocityDirection.y = 0.04f;
                }

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = contactPoint + (radial * cue.ChalkSizeMeters * 0.2f),
                    velocity = velocityDirection.normalized
                        * cue.ChalkSpeedMetersPerSecond
                        * Mathf.Lerp(0.72f, 1.18f, normalizedIndex),
                    startLifetime = cue.ChalkLifetimeSeconds * Mathf.Lerp(0.82f, 1.18f, normalizedIndex),
                    startSize = cue.ChalkSizeMeters * Mathf.Lerp(0.72f, 1.2f, 1f - normalizedIndex),
                    startColor = ChalkColor,
                };

                chalkDustParticles.Emit(emitParams, 1);
            }
        }

        private void EmitContactAccent(
            Vector3 contactPoint,
            Vector3 strikeDirection,
            Vector3 tangent,
            Vector3 bitangent,
            CueImpactVfxCue cue)
        {
            for (var index = 0; index < cue.AccentParticleCount; index++)
            {
                var normalizedIndex = (index + 0.5f) / cue.AccentParticleCount;
                var angle = (index * GoldenAngleRadians) + 0.7f;
                var radial = ((Mathf.Cos(angle) * tangent) + (Mathf.Sin(angle) * bitangent)).normalized;
                var velocityDirection = (radial * 0.85f) + (strikeDirection * 0.15f) + (Vector3.up * 0.28f);
                if (velocityDirection.y < 0.06f)
                {
                    velocityDirection.y = 0.06f;
                }

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = contactPoint,
                    velocity = velocityDirection.normalized
                        * cue.AccentSpeedMetersPerSecond
                        * Mathf.Lerp(0.78f, 1.16f, normalizedIndex),
                    startLifetime = cue.AccentLifetimeSeconds * Mathf.Lerp(0.82f, 1.12f, normalizedIndex),
                    startSize = cue.AccentSizeMeters * Mathf.Lerp(0.75f, 1.15f, 1f - normalizedIndex),
                    startColor = AccentColor,
                };

                contactAccentParticles.Emit(emitParams, 1);
            }
        }

        private static ParticleSystem CreateParticleSystem(
            string name,
            Transform parent,
            Material material,
            int maxParticles,
            float gravityModifier)
        {
            var systemObject = new GameObject(name);
            systemObject.transform.SetParent(parent, false);
            var particleSystem = systemObject.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startSpeed = 0f;
            main.startLifetime = 0.4f;
            main.startSize = 0.01f;
            main.gravityModifier = gravityModifier;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = fadeGradient;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var renderer = systemObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = -0.1f;

            return particleSystem;
        }

        private static Material CreateParticleMaterial(Shader shader)
        {
            var material = new Material(shader)
            {
                name = "Cue Impact Runtime Particle Material",
                hideFlags = HideFlags.DontSave,
            };

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static void BuildPerpendicularBasis(
            Vector3 direction,
            out Vector3 tangent,
            out Vector3 bitangent)
        {
            tangent = Vector3.Cross(direction, Vector3.up);
            if (tangent.sqrMagnitude <= 0.000001f)
            {
                tangent = Vector3.Cross(direction, Vector3.right);
            }

            tangent.Normalize();
            bitangent = Vector3.Cross(direction, tangent).normalized;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
