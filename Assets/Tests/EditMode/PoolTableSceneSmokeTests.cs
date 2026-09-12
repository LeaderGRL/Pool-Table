using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var scene = SceneManager.GetSceneByPath(PoolTableScenePath);
            var sceneWasAlreadyLoaded = scene.IsValid() && scene.isLoaded;

            if (!sceneWasAlreadyLoaded)
            {
                scene = EditorSceneManager.OpenScene(PoolTableScenePath, OpenSceneMode.Additive);
            }

            try
            {
                Assert.That(scene.IsValid(), Is.True, $"Failed to open {PoolTableScenePath}.");

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
                if (!sceneWasAlreadyLoaded && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
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
