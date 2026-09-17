using System.Collections;
using NUnit.Framework;
using PoolTable.Gameplay.Shots;
using PoolTable.Presentation.Vfx;
using UnityEngine;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    public sealed class CueImpactVfxPresenterPlayModeTests
    {
        private GameObject parentObject;
        private CueImpactVfxPresenter presenter;

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
        public IEnumerator Presenter_EmitsAtContactPointAndReusesBoundedParticleSystems()
        {
            parentObject = new GameObject("Cue impact VFX test parent");
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Assert.That(particleShader, Is.Not.Null);
            presenter = new CueImpactVfxPresenter(parentObject.transform, particleShader);
            var contactPoint = new Vector3(0.25f, 0.82f, -0.4f);
            var strikeDirection = new Vector3(0.8f, -0.1f, 0.4f).normalized;
            var observation = new CueStrikeObservation(
                1f,
                6.6666667f,
                contactPoint,
                strikeDirection);

            presenter.PlayCueStrike(observation);

            var expectedCue = CueImpactVfxModel.Evaluate(1f);
            Assert.That(presenter.Root.childCount, Is.EqualTo(2));
            Assert.That(presenter.ChalkDustParticles.particleCount, Is.EqualTo(expectedCue.ChalkParticleCount));
            Assert.That(presenter.ContactAccentParticles.particleCount, Is.EqualTo(expectedCue.AccentParticleCount));

            var chalkParticles = new ParticleSystem.Particle[expectedCue.ChalkParticleCount];
            var chalkParticleCount = presenter.ChalkDustParticles.GetParticles(chalkParticles);
            Assert.That(chalkParticleCount, Is.EqualTo(expectedCue.ChalkParticleCount));
            Assert.That(
                Vector3.Distance(chalkParticles[0].position, contactPoint),
                Is.LessThan(0.01f),
                "The chalk burst must originate from the actual cue-ball contact point.");

            presenter.PlayCueStrike(observation);
            presenter.PlayCueStrike(observation);

            Assert.That(
                presenter.Root.childCount,
                Is.EqualTo(2),
                "Repeated cue strikes must reuse the two runtime particle systems.");
            Assert.That(
                presenter.ChalkDustParticles.particleCount,
                Is.LessThanOrEqualTo(presenter.ChalkDustParticles.main.maxParticles));
            Assert.That(
                presenter.ContactAccentParticles.particleCount,
                Is.LessThanOrEqualTo(presenter.ContactAccentParticles.main.maxParticles));

            presenter.Dispose();
            presenter = null;
            yield return null;

            Assert.That(parentObject.transform.Find("Cue Impact VFX"), Is.Null);
        }
    }
}
