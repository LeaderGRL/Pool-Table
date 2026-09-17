using System;
using PoolTable.Gameplay.Feedback;
using UnityEngine;
using UnityEngine.Rendering;

namespace PoolTable.Presentation.Vfx
{
    public sealed class PocketCaptureVfxPresenter : IDisposable
    {
        private const float GoldenAngleRadians = 2.39996323f;
        private const float SurfaceLiftMeters = 0.024f;
        private const float RimSpreadMeters = 0.043f;

        private static readonly Color DustColor = new(0.72f, 0.26f, 0.20f, 0.62f);
        private static readonly Color AccentColor = new(1f, 0.86f, 0.58f, 0.82f);

        private readonly Transform root;
        private readonly ParticleSystem dustParticles;
        private readonly ParticleSystem accentParticles;
        private readonly Material particleMaterial;
        private bool disposed;

        public PocketCaptureVfxPresenter(Transform parent, Shader particleShader)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (particleShader == null)
            {
                throw new ArgumentNullException(nameof(particleShader));
            }

            var rootObject = new GameObject("Pocket Capture VFX");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;

            particleMaterial = CreateParticleMaterial(particleShader);
            dustParticles = CreateParticleSystem(
                "Pocket Cloth Dust",
                root,
                particleMaterial,
                96,
                0.10f);
            accentParticles = CreateParticleSystem(
                "Pocket Rim Accent",
                root,
                particleMaterial,
                48,
                0.04f);
        }

        internal Transform Root => root;

        internal ParticleSystem DustParticles => dustParticles;

        internal ParticleSystem AccentParticles => accentParticles;

        internal void PlayPocketCapture(PocketCaptureFeedbackObservation observation)
        {
            if (disposed)
            {
                return;
            }

            var cue = PocketCaptureVfxModel.Evaluate();
            if (!cue.ShouldPlay)
            {
                return;
            }

            var origin = observation.MouthWorldPosition + (Vector3.up * SurfaceLiftMeters);
            EmitDust(origin, cue);
            EmitAccent(origin, cue);
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

        private void EmitDust(Vector3 origin, PocketCaptureVfxCue cue)
        {
            for (var index = 0; index < cue.DustParticleCount; index++)
            {
                var normalizedIndex = (index + 0.5f) / cue.DustParticleCount;
                var angle = index * GoldenAngleRadians;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var velocityDirection = ((radial * 0.64f) + (Vector3.up * 0.77f)).normalized;

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = origin + (radial * (RimSpreadMeters + (cue.DustSizeMeters * 0.25f))),
                    velocity = velocityDirection
                        * cue.DustSpeedMetersPerSecond
                        * Mathf.Lerp(0.72f, 1.12f, normalizedIndex),
                    startLifetime = cue.DustLifetimeSeconds * Mathf.Lerp(0.82f, 1.12f, normalizedIndex),
                    startSize = cue.DustSizeMeters * Mathf.Lerp(0.74f, 1.18f, 1f - normalizedIndex),
                    startColor = DustColor,
                };

                dustParticles.Emit(emitParams, 1);
            }
        }

        private void EmitAccent(Vector3 origin, PocketCaptureVfxCue cue)
        {
            for (var index = 0; index < cue.AccentParticleCount; index++)
            {
                var normalizedIndex = (index + 0.5f) / cue.AccentParticleCount;
                var angle = (index * GoldenAngleRadians) + 0.8f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var velocityDirection = ((radial * 0.42f) + (Vector3.up * 0.91f)).normalized;

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = origin + (radial * (RimSpreadMeters * 0.7f)),
                    velocity = velocityDirection
                        * cue.AccentSpeedMetersPerSecond
                        * Mathf.Lerp(0.82f, 1.14f, normalizedIndex),
                    startLifetime = cue.AccentLifetimeSeconds * Mathf.Lerp(0.86f, 1.10f, normalizedIndex),
                    startSize = cue.AccentSizeMeters * Mathf.Lerp(0.76f, 1.16f, 1f - normalizedIndex),
                    startColor = AccentColor,
                };

                accentParticles.Emit(emitParams, 1);
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
                    new GradientAlphaKey(0.55f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = fadeGradient;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));

            var renderer = systemObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = -0.08f;

            return particleSystem;
        }

        private static Material CreateParticleMaterial(Shader shader)
        {
            var material = new Material(shader)
            {
                name = "Pocket Capture Runtime Particle Material",
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
