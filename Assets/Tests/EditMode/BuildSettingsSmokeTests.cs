using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace PoolTable.Tests.EditMode
{
    public sealed class BuildSettingsSmokeTests
    {
        private const string PoolTableScenePath = "Assets/Scenes/PoolTable.unity";
        private const string DemoScenePath = "Assets/Billiard Balls/Demo.unity";

        [Test]
        public void EnabledBuildScenes_ContainOnlyPoolTableScene()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Assert.That(enabledScenes, Is.EqualTo(new[] { PoolTableScenePath }));
            Assert.That(enabledScenes, Does.Not.Contain(DemoScenePath));
        }
    }
}
