using System.Collections;
using System.IO;
using NUnit.Framework;
using PoolTable.Gameplay.Balls;
using PoolTable.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    [Category("Gameplay")]
    [Category("Physics")]
    public sealed class GameplayScenarioRunnerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScenarioRunner_ExportsDeterministicCheckpointsAndShotInstrumentationAsJson()
        {
            yield return LoadPoolTableScene();

            var compositionRoot = Object.FindAnyObjectByType<PoolTableSceneCompositionRoot>();
            Assert.That(compositionRoot, Is.Not.Null);

            var identities = compositionRoot.BallsRoot.GetComponentsInChildren<BallIdentity>(true);
            Assert.That(identities, Has.Length.EqualTo(16));

            var runner = new GameplayScenarioRunner("scene-instrumentation-smoke", 8801);
            runner.CaptureCheckpoint("initial", identities);

            var instrumentation = compositionRoot.ShotSimulationInstrumentation;
            Assert.That(instrumentation, Is.Not.Null);
            instrumentation.BeginShot();
            yield return new WaitForFixedUpdate();
            runner.AttachShotReport(instrumentation.CompleteShot());
            runner.CaptureCheckpoint("after-fixed-step", identities);

            var reportPath = runner.WriteJsonReport();
            Assert.That(File.Exists(reportPath), Is.True);

            var parsed = JsonUtility.FromJson<GameplayScenarioReport>(File.ReadAllText(reportPath));
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.ScenarioName, Is.EqualTo("scene-instrumentation-smoke"));
            Assert.That(parsed.Seed, Is.EqualTo(8801));
            Assert.That(parsed.Checkpoints, Has.Count.EqualTo(2));
            Assert.That(parsed.Checkpoints[0].Balls, Has.Count.EqualTo(16));
            Assert.That(parsed.Checkpoints[0].Balls[0].BallNumber, Is.EqualTo(0));
            Assert.That(parsed.Shot, Is.Not.Null);
            Assert.That(parsed.Shot.TrackCount, Is.EqualTo(16));
        }

        private static IEnumerator LoadPoolTableScene()
        {
            Assert.That(SceneManager.sceneCountInBuildSettings, Is.GreaterThan(0));

            var loadOperation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }
    }
}
