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
            var gameManager = FindActiveMonoBehaviourByTypeName(activeScene, "GameManager");
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");

            Assert.That(gameManager, Is.Not.Null, "GameManager must be initialized in the gameplay scene.");
            Assert.That(playerController, Is.Not.Null, "PlayersStateManagement must be initialized in the gameplay scene.");
            Assert.That(gameManager.isActiveAndEnabled, Is.True);
            Assert.That(playerController.isActiveAndEnabled, Is.True);

            var playerRigidbody = playerController.GetComponent<Rigidbody>();
            var playerCollider = playerController.GetComponent<Collider>();

            Assert.That(playerRigidbody, Is.Not.Null, "The active player controller must keep its Rigidbody.");
            Assert.That(playerCollider, Is.Not.Null, "The active player controller must keep its collision shape.");
            Assert.That(playerCollider.enabled, Is.True, "The active player controller collider must be enabled.");

            var cueBall = GetPublicGameObjectField(playerController, "WhiteBall");
            var spectateCamera = GetPublicGameObjectField(playerController, "Cam");
            var cueCamera = GetPublicGameObjectField(playerController, "Cue_Camera");

            Assert.That(cueBall, Is.Not.Null);
            Assert.That(cueBall.CompareTag("white"), Is.True);
            Assert.That(cueBall.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That(cueBall.GetComponent<SphereCollider>(), Is.Not.Null);

            var cueBallStateManager = FindMonoBehaviourByTypeName(cueBall, "BallStateManager");
            var cueBallCollision = FindMonoBehaviourByTypeName(cueBall, "WhiteBallCollision");

            Assert.That(cueBallStateManager, Is.Not.Null);
            Assert.That(cueBallStateManager.enabled, Is.True);
            Assert.That(cueBallCollision, Is.Not.Null, "The cue ball must keep WhiteBallCollision.");
            Assert.That(cueBallCollision.enabled, Is.True, "WhiteBallCollision must be enabled on the cue ball.");

            Assert.That(spectateCamera, Is.Not.Null, "PlayersStateManagement.Cam must remain wired.");
            Assert.That(spectateCamera.activeInHierarchy, Is.True, "The spectate camera must be active in the scene hierarchy.");

            var outputCamera = spectateCamera.GetComponent<Camera>();
            var cinemachineBrain = FindMonoBehaviourByTypeName(spectateCamera, "CinemachineBrain");

            Assert.That(outputCamera, Is.Not.Null, "PlayersStateManagement.Cam must reference a camera object.");
            Assert.That(outputCamera.enabled, Is.True, "The output Camera component must be enabled.");
            Assert.That(cinemachineBrain, Is.Not.Null, "The output camera must keep its CinemachineBrain.");
            Assert.That(cinemachineBrain.enabled, Is.True, "The output camera CinemachineBrain must be enabled.");

            Assert.That(cueCamera, Is.Not.Null);
            Assert.That(cueCamera.name, Is.EqualTo("Camera_Cue"));
            Assert.That(cueCamera.activeInHierarchy, Is.True, "Camera_Cue must be active in the scene hierarchy.");

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
                Assert.That(collider.isTrigger, Is.False, $"{ball.name} SphereCollider must remain a solid collider.");
                Assert.That(ballStateManager, Is.Not.Null, $"{ball.name} must keep BallStateManager.");
                Assert.That(ballStateManager.enabled, Is.True, $"{ball.name} BallStateManager must be enabled.");
            }

            Assert.That(
                FindActiveSolidMeshColliderByName(activeScene, "tabletop"),
                Is.Not.Null,
                "The pool table must keep active solid tabletop collision geometry.");
            Assert.That(
                FindActiveSolidMeshColliderByName(activeScene, "rubber"),
                Is.Not.Null,
                "The pool table must keep active solid cushion collision geometry.");
            Assert.That(
                FindActiveSolidMeshColliderByName(activeScene, "sides"),
                Is.Not.Null,
                "The pool table must keep active solid side-rail collision geometry.");

            var pocket = FindActiveMonoBehaviourByTypeName(activeScene, "Pocket");
            Assert.That(pocket, Is.Not.Null, "The gameplay scene must keep an active Pocket capture behavior.");

            var pocketCollider = pocket.GetComponent<Collider>();
            Assert.That(pocketCollider, Is.Not.Null, "Pocket must keep its trigger collider.");
            Assert.That(pocketCollider.enabled, Is.True, "Pocket trigger collider must be enabled.");
            Assert.That(pocketCollider.isTrigger, Is.True, "Pocket collider must remain configured as a trigger.");

            var playerOneTurn = GetPublicGameObjectField(gameManager, "UI_Player1Turn");
            var playerTwoTurn = GetPublicGameObjectField(gameManager, "UI_Player2Turn");
            var playerOneBallType = GetPublicGameObjectField(gameManager, "UI_Player1BallType");
            var playerTwoBallType = GetPublicGameObjectField(gameManager, "UI_Player2BallType");

            Assert.That(playerOneTurn, Is.Not.Null);
            Assert.That(playerTwoTurn, Is.Not.Null);
            Assert.That(playerOneBallType, Is.Not.Null);
            Assert.That(playerTwoBallType, Is.Not.Null);
            Assert.That(FindMonoBehaviourByTypeName(playerOneBallType, "Text"), Is.Not.Null, "Player 1 ball-type UI must keep its Text component.");
            Assert.That(FindMonoBehaviourByTypeName(playerTwoBallType, "Text"), Is.Not.Null, "Player 2 ball-type UI must keep its Text component.");
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

        private static MonoBehaviour FindActiveMonoBehaviourByTypeName(Scene scene, string typeName)
        {
            return EnumerateSceneObjects(scene)
                .SelectMany(gameObject => gameObject.GetComponents<MonoBehaviour>())
                .FirstOrDefault(component =>
                    component != null
                    && component.GetType().Name == typeName
                    && component.isActiveAndEnabled);
        }

        private static MonoBehaviour FindMonoBehaviourByTypeName(GameObject gameObject, string typeName)
        {
            return gameObject.GetComponents<MonoBehaviour>()
                .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        private static MeshCollider FindActiveSolidMeshColliderByName(Scene scene, string nameFragment)
        {
            return EnumerateSceneObjects(scene)
                .Where(gameObject => gameObject.activeInHierarchy && gameObject.name.ToLowerInvariant().Contains(nameFragment))
                .Select(gameObject => gameObject.GetComponent<MeshCollider>())
                .FirstOrDefault(collider => collider != null && collider.enabled && !collider.isTrigger);
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
