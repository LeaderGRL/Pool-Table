using System.Collections;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Feedback;
using PoolTable.Physics.Configuration;
using PoolTable.Presentation.Vfx;
using UnityEngine;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    public sealed class PocketCaptureVfxPresenterPlayModeTests
    {
        private GameObject parentObject;
        private PocketCaptureVfxPresenter presenter;

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            presenter = null;

            if (parentObject != null)
            {
                Object.DestroyImmediate(parentObject);
                parentObject = null;
            }
        }

        [UnityTest]
        public IEnumerator Presenter_EmitsAtPocketMouthAndReusesBoundedParticleSystems()
        {
            parentObject = new GameObject("Pocket capture VFX test parent");
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Assert.That(particleShader, Is.Not.Null);
            presenter = new PocketCaptureVfxPresenter(parentObject.transform, particleShader);
            var mouthPosition = new Vector3(1.2f, 0.76f, -0.63f);
            var observation = new PocketCaptureFeedbackObservation(
                new PocketedBall(new BallId(4), new PocketId(3)),
                mouthPosition);

            presenter.PlayPocketCapture(observation);

            var expectedCue = PocketCaptureVfxModel.Evaluate();
            Assert.That(presenter.Root.childCount, Is.EqualTo(2));
            Assert.That(presenter.DustParticles.particleCount, Is.EqualTo(expectedCue.DustParticleCount));
            Assert.That(presenter.AccentParticles.particleCount, Is.EqualTo(expectedCue.AccentParticleCount));

            var particles = new ParticleSystem.Particle[expectedCue.DustParticleCount];
            var particleCount = presenter.DustParticles.GetParticles(particles);
            Assert.That(particleCount, Is.EqualTo(expectedCue.DustParticleCount));
            Assert.That(
                Vector3.Distance(particles[0].position, mouthPosition),
                Is.LessThan(BilliardsPhysicalSpecification.PocketCaptureRadiusMeters + 0.01f),
                "Pocket feedback must remain localized within the captured pocket footprint.");

            presenter.PlayPocketCapture(observation);
            presenter.PlayPocketCapture(observation);

            Assert.That(
                presenter.Root.childCount,
                Is.EqualTo(2),
                "Repeated pocket captures must reuse the two runtime particle systems.");
            Assert.That(
                presenter.DustParticles.particleCount,
                Is.LessThanOrEqualTo(presenter.DustParticles.main.maxParticles));
            Assert.That(
                presenter.AccentParticles.particleCount,
                Is.LessThanOrEqualTo(presenter.AccentParticles.main.maxParticles));

            presenter.Dispose();
            presenter = null;
            yield return null;

            Assert.That(parentObject.transform.Find("Pocket Capture VFX"), Is.Null);
        }
    }
}
