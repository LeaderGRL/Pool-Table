using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class PoolTableSceneSmokeTests
    {
        private const string PoolTableScenePath = "Assets/Scenes/PoolTable.unity";

        [Test]
        public void PoolTableScene_OpensWithoutMissingMonoBehaviourScripts()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PoolTableScenePath);
            Assert.That(sceneAsset, Is.Not.Null, $"Scene asset not found at {PoolTableScenePath}.");

            var scene = EditorSceneManager.OpenPreviewScene(PoolTableScenePath);

            try
            {
                Assert.That(scene.IsValid(), Is.True, $"Failed to open preview scene for {PoolTableScenePath}.");

                var missingScriptEntries = new List<string>();

                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    foreach (var transform in rootObject.GetComponentsInChildren<Transform>(true))
                    {
                        var gameObject = transform.gameObject;
                        var missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

                        if (missingScriptCount > 0)
                        {
                            missingScriptEntries.Add($"{GetHierarchyPath(transform)} ({missingScriptCount})");
                        }
                    }
                }

                Assert.That(
                    missingScriptEntries,
                    Is.Empty,
                    "Missing MonoBehaviour script references found:\n" + string.Join("\n", missingScriptEntries));
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
