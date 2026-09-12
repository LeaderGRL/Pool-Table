using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    public sealed class PoolTableScenePlayModeSmokeTests
    {
        private const string PoolTableScenePath = "Assets/Scenes/PoolTable.unity";
        private static readonly HashSet<string> BilliardBallTags = new HashSet<string>
        {
            "white",
            "black",
            "filled",
            "striped",
        };

        [UnityTest]
        public IEnumerator PoolTableScene_StartsAndKeepsEssentialSceneObjectsAvailable()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            Assert.That(activeScene.path, Is.EqualTo(PoolTableScenePath));
            Assert.That(activeScene.isLoaded, Is.True);

            Assert.That(GameObject.Find("Main Camera"), Is.Not.Null);
            Assert.That(SceneContainsObject(activeScene, "POOL TABLE"), Is.True);
            Assert.That(SceneContainsObject(activeScene, "Balls"), Is.True);
            Assert.That(SceneContainsObject(activeScene, "PoolCue"), Is.True);
            Assert.That(SceneContainsObject(activeScene, "Player"), Is.True);
        }

        [UnityTest]
        public IEnumerator PoolTableScene_InitializesLegacyGameplayWiring()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var gameManager = FindMonoBehaviourByTypeName(activeScene, "GameManager");
            var playerController = FindMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");

            Assert.That(gameManager, Is.Not.Null, "GameManager must be initialized in the gameplay scene.");
            Assert.That(playerController, Is.Not.Null, "PlayersStateManagement must be initialized in the gameplay scene.");

            var cueBall = GetPublicGameObjectField(playerController, "WhiteBall");
            var cueCamera = GetPublicGameObjectField(playerController, "Cue_Camera");

            Assert.That(cueBall, Is.Not.Null);
            Assert.That(cueBall.CompareTag("white"), Is.True);
            Assert.That(cueBall.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That(cueBall.GetComponent<SphereCollider>(), Is.Not.Null);
            Assert.That(HasMonoBehaviourType(cueBall, "BallStateManager"), Is.True);

            Assert.That(cueCamera, Is.Not.Null);
            Assert.That(cueCamera.name, Is.EqualTo("Camera_Cue"));

            var cueVirtualCamera = FindMonoBehaviourByTypeName(cueCamera, "CinemachineFreeLook");
            Assert.That(cueVirtualCamera, Is.Not.Null, "Camera_Cue must keep its CinemachineFreeLook component.");
            Assert.That(cueVirtualCamera.enabled, Is.True, "Camera_Cue CinemachineFreeLook must be enabled.");
            Assert.That(
                GetPublicTransformProperty(cueVirtualCamera, "LookAt"),
                Is.EqualTo(cueBall.transform),
                "Camera_Cue must keep the cue ball as its LookAt target.");

            var billiardBalls = EnumerateSceneObjects(activeScene)
                .Where(gameObject => BilliardBallTags.Contains(gameObject.tag))
                .ToArray();

            Assert.That(billiardBalls, Has.Length.EqualTo(16));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("white")), Is.EqualTo(1));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("black")), Is.EqualTo(1));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("filled")), Is.EqualTo(7));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("striped")), Is.EqualTo(7));

            foreach (var ball in billiardBalls)
            {
                var collider = ball.GetComponent<SphereCollider>();
                var ballStateManager = FindMonoBehaviourByTypeName(ball, "BallStateManager");

                Assert.That(ball.activeInHierarchy, Is.True, $"{ball.name} must be active in the scene hierarchy.");
                Assert.That(ball.GetComponent<Rigidbody>(), Is.Not.Null, $"{ball.name} must keep its Rigidbody.");
                Assert.That(collider, Is.Not.Null, $"{ball.name} must keep its SphereCollider.");
                Assert.That(collider.enabled, Is.True, $"{ball.name} SphereCollider must be enabled.");
                Assert.That(ballStateManager, Is.Not.Null, $"{ball.name} must keep BallStateManager.");
                Assert.That(ballStateManager.enabled, Is.True, $"{ball.name} BallStateManager must be enabled.");
            }

            var playerOneTurn = GetPublicGameObjectField(gameManager, "UI_Player1Turn");
            var playerTwoTurn = GetPublicGameObjectField(gameManager, "UI_Player2Turn");

            Assert.That(playerOneTurn, Is.Not.Null);
            Assert.That(playerTwoTurn, Is.Not.Null);
            Assert.That(
                playerOneTurn.activeInHierarchy,
                Is.Not.EqualTo(playerTwoTurn.activeInHierarchy),
                "Exactly one turn indicator must be active in the scene hierarchy after startup.");
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
            yield return null;
        }

        private static bool SceneContainsObject(Scene scene, string objectName)
        {
            return EnumerateSceneObjects(scene).Any(gameObject => gameObject.name == objectName);
        }

        private static IEnumerable<GameObject> EnumerateSceneObjects(Scene scene)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var transform in rootObject.GetComponentsInChildren<Transform>(true))
                {
                    yield return transform.gameObject;
                }
            }
        }

        private static MonoBehaviour FindMonoBehaviourByTypeName(Scene scene, string typeName)
        {
            return EnumerateSceneObjects(scene)
                .SelectMany(gameObject => gameObject.GetComponents<MonoBehaviour>())
                .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        private static MonoBehaviour FindMonoBehaviourByTypeName(GameObject gameObject, string typeName)
        {
            return gameObject.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        private static bool HasMonoBehaviourType(GameObject gameObject, string typeName)
        {
            return gameObject.GetComponents<MonoBehaviour>()
                .Any(component => component != null && component.GetType().Name == typeName);
        }

        private static GameObject GetPublicGameObjectField(MonoBehaviour component, string fieldName)
        {
            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"{component.GetType().Name}.{fieldName} must remain a public serialized field during migration.");
            Assert.That(field.FieldType, Is.EqualTo(typeof(GameObject)));

            return field.GetValue(component) as GameObject;
        }

        private static Transform GetPublicTransformProperty(MonoBehaviour component, string propertyName)
        {
            var property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} must remain a public property during migration.");
            Assert.That(property.PropertyType, Is.EqualTo(typeof(Transform)));

            return property.GetValue(component) as Transform;
        }
    }
}
