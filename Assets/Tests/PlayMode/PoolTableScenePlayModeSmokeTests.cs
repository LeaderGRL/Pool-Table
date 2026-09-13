using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Match;
using PoolTable.Presentation;
using PoolTable.Presentation.Audio;
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
        public IEnumerator PoolTableScene_TypedBallIdentitiesFeedGameplayShotResolution()
        {
            yield return LoadPoolTableScene();

            var identities = Object.FindObjectsByType<BallIdentity>(FindObjectsSortMode.None);
            var objectBalls = identities
                .Select(identity => identity.Id)
                .Where(id => !id.IsCueBall)
                .OrderBy(id => id.Number)
                .ToArray();

            Assert.That(identities, Has.Length.EqualTo(16));
            Assert.That(objectBalls, Has.Length.EqualTo(15));

            var snapshot = new ObjectBallTableSnapshot(objectBalls);
            var state = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial());
            var calledBall = objectBalls.First(id => id.Group == BallGroup.Solids);
            var calledPocket = new PocketId(1);
            var intent = new ShotIntent(
                MatchPlayerId.PlayerOne,
                new ShotDirection(1f, 0f),
                0.5f,
                new CalledShot(calledBall, calledPocket));
            var facts = new ShotFacts(
                calledBall,
                new[] { new PocketedBall(calledBall, calledPocket) },
                System.Array.Empty<BallId>());

            var resolution = new MatchShotResolver().Resolve(state, intent, facts, snapshot);

            Assert.That(resolution.FoulResolution.IsClean, Is.True);
            Assert.That(resolution.GroupAssigned, Is.True);
            Assert.That(resolution.State.Phase, Is.EqualTo(MatchPhase.GroupsAssigned));
            Assert.That(resolution.State.PlayerOne.Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(resolution.State.PlayerTwo.Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(resolution.ShooterContinues, Is.True);
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
            Assert.That(playerCollider.isTrigger, Is.False, "The active player controller collider must remain solid.");

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
            Assert.That(
                GetPublicPropertyValue(cinemachineBrain, "ActiveVirtualCamera"),
                Is.SameAs(cueVirtualCamera),
                "The output CinemachineBrain must render Camera_Cue after startup.");

            var aimingRaycast = FindMonoBehaviourByTypeName(playerController.gameObject, "Stick_Raycast");
            Assert.That(aimingRaycast, Is.Not.Null, "The active player controller must keep Stick_Raycast.");
            Assert.That(aimingRaycast.enabled, Is.True, "Stick_Raycast must be enabled on the active player controller.");
            Assert.That(
                GetPublicGameObjectField(aimingRaycast, "pt"),
                Is.Not.Null,
                "Stick_Raycast.pt must remain wired for aiming hits.");

            var compositionRoot = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>();
            Assert.That(compositionRoot, Is.Not.Null, "The gameplay scene must keep an active composition root.");
            Assert.That(compositionRoot.gameObject.scene, Is.EqualTo(activeScene));

            var soundManager = compositionRoot.SoundManager;
            Assert.That(soundManager, Is.Not.Null, "The composition root must keep its SoundManager reference.");
            Assert.That(soundManager.isActiveAndEnabled, Is.True, "The composed SoundManager must be active and enabled.");
            Assert.That(
                typeof(SoundManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                Is.Null,
                "SoundManager must not expose a static singleton instance after composition-root migration.");

            var soundEffectSource = soundManager.SoundEffectSource;
            Assert.That(soundEffectSource, Is.Not.Null, "SoundManager must keep its SFX AudioSource reference.");
            Assert.That(soundEffectSource.gameObject.activeInHierarchy, Is.True, "The SFX AudioSource object must be active.");
            Assert.That(soundEffectSource.enabled, Is.True, "The SFX AudioSource must be enabled.");

            var billiardBalls = EnumerateSceneObjects(activeScene)
                .Where(gameObject => BilliardBallTags.Contains(gameObject.tag))
                .ToArray();

            var ballsContainer = EnumerateSceneObjects(activeScene)
                .FirstOrDefault(gameObject => gameObject.name == "Balls" && gameObject.activeInHierarchy);

            Assert.That(billiardBalls, Has.Length.EqualTo(16));
            Assert.That(ballsContainer, Is.Not.Null, "The active runtime Balls container must remain available.");
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("white")), Is.EqualTo(1));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("black")), Is.EqualTo(1));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("filled")), Is.EqualTo(7));
            Assert.That(billiardBalls.Count(ball => ball.CompareTag("striped")), Is.EqualTo(7));

            var ballIds = billiardBalls
                .Select(ball => ball.GetComponent<BallIdentity>())
                .Where(identity => identity != null)
                .Select(identity => identity.Id.Number)
                .OrderBy(number => number)
                .ToArray();

            Assert.That(
                ballIds,
                Is.EqualTo(Enumerable.Range(BallId.MinimumNumber, BallId.MaximumNumber - BallId.MinimumNumber + 1)),
                "Typed ball identities must cover IDs 0 through 15 exactly once.");

            foreach (var ball in billiardBalls)
            {
                var rigidbody = ball.GetComponent<Rigidbody>();
                var collider = ball.GetComponent<SphereCollider>();
                var ballStateManager = FindMonoBehaviourByTypeName(ball, "BallStateManager");
                var identities = ball.GetComponents<BallIdentity>();
                var collisionAudio = ball.GetComponent<PlaySoundOnBallCollision>();
                var renderer = ball.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(candidate => candidate.enabled && candidate.gameObject.activeInHierarchy);

                Assert.That(ball.activeInHierarchy, Is.True, $"{ball.name} must be active in the scene hierarchy.");
                Assert.That(ball.transform.IsChildOf(ballsContainer.transform), Is.True, $"{ball.name} must remain under the runtime Balls container.");
                Assert.That(rigidbody, Is.Not.Null, $"{ball.name} must keep its Rigidbody.");
                Assert.That(rigidbody.isKinematic, Is.False, $"{ball.name} Rigidbody must remain dynamic.");
                Assert.That(collider, Is.Not.Null, $"{ball.name} must keep its SphereCollider.");
                Assert.That(collider.enabled, Is.True, $"{ball.name} SphereCollider must be enabled.");
                Assert.That(collider.isTrigger, Is.False, $"{ball.name} SphereCollider must remain a solid collider.");
                Assert.That(ballStateManager, Is.Not.Null, $"{ball.name} must keep BallStateManager.");
                Assert.That(ballStateManager.enabled, Is.True, $"{ball.name} BallStateManager must be enabled.");
                Assert.That(identities, Has.Length.EqualTo(1), $"{ball.name} must have exactly one BallIdentity.");
                Assert.That(identities[0].isActiveAndEnabled, Is.True, $"{ball.name} BallIdentity must be active and enabled.");
                AssertLegacyBallIdentityMatchesTag(ball, identities[0]);
                Assert.That(collisionAudio, Is.Not.Null, $"{ball.name} must keep PlaySoundOnBallCollision.");
                Assert.That(collisionAudio.enabled, Is.True, $"{ball.name} PlaySoundOnBallCollision must be enabled.");
                Assert.That(
                    collisionAudio.SoundManager,
                    Is.SameAs(soundManager),
                    $"{ball.name} collision audio must receive the scene SoundManager from the composition root.");
                Assert.That(renderer, Is.Not.Null, $"{ball.name} must keep an active enabled Renderer in its hierarchy.");

                var collisionClips = GetPrivateFieldValue<AudioClip[]>(collisionAudio, "SFX_BallCollision");
                Assert.That(collisionClips, Is.Not.Null.And.Not.Empty, $"{ball.name} must keep collision audio clips.");
                Assert.That(collisionClips.All(clip => clip != null), Is.True, $"{ball.name} collision audio clips must all be assigned.");
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

            var ground = FindActiveMonoBehaviourWithCollider(activeScene, "Ground", requireTrigger: true);
            Assert.That(ground, Is.Not.Null, "The gameplay scene must keep an active Ground recovery trigger.");

            var groundCollider = ground.GetComponent<Collider>();
            Assert.That(groundCollider, Is.Not.Null, "Ground must keep its recovery collider.");
            Assert.That(groundCollider.enabled, Is.True, "Ground recovery collider must be enabled.");
            Assert.That(groundCollider.isTrigger, Is.True, "Ground recovery collider must remain configured as a trigger.");

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
                .FirstOrDefault(collider =>
                    collider != null
                    && collider.enabled
                    && !collider.isTrigger
                    && collider.sharedMesh != null);
        }

        private static MonoBehaviour FindActiveMonoBehaviourWithCollider(Scene scene, string typeName, bool requireTrigger)
        {
            return EnumerateSceneObjects(scene)
                .Where(gameObject => gameObject.activeInHierarchy)
                .Select(gameObject => new
                {
                    Behaviour = FindMonoBehaviourByTypeName(gameObject, typeName),
                    Collider = gameObject.GetComponent<Collider>(),
                })
                .Where(entry =>
                    entry.Behaviour != null
                    && entry.Behaviour.enabled
                    && entry.Collider != null
                    && entry.Collider.enabled
                    && entry.Collider.isTrigger == requireTrigger)
                .Select(entry => entry.Behaviour)
                .FirstOrDefault();
        }

        private static GameObject GetPublicGameObjectField(MonoBehaviour component, string fieldName)
        {
            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"{component.GetType().Name}.{fieldName} must remain a public serialized field during migration.");
            Assert.That(field.FieldType, Is.EqualTo(typeof(GameObject)));

            return field.GetValue(component) as GameObject;
        }

        private static T GetPrivateFieldValue<T>(MonoBehaviour component, string fieldName)
        {
            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{component.GetType().Name}.{fieldName} must remain a private serialized field during migration.");
            Assert.That(field.FieldType, Is.EqualTo(typeof(T)));

            return (T)field.GetValue(component);
        }

        private static object GetPublicPropertyValue(MonoBehaviour component, string propertyName)
        {
            var property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} must remain a public property during migration.");

            return property.GetValue(component);
        }

        private static Transform GetPublicTransformProperty(MonoBehaviour component, string propertyName)
        {
            var property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} must remain a public property during migration.");
            Assert.That(property.PropertyType, Is.EqualTo(typeof(Transform)));

            return property.GetValue(component) as Transform;
        }

        private static void AssertLegacyBallIdentityMatchesTag(GameObject ball, BallIdentity identity)
        {
            if (ball.CompareTag("white"))
            {
                Assert.That(identity.Id.Number, Is.EqualTo(BallId.CueBallNumber));
                Assert.That(identity.IsCueBall, Is.True);
                Assert.That(identity.Group, Is.EqualTo(BallGroup.None));
                return;
            }

            if (ball.CompareTag("black"))
            {
                Assert.That(identity.Id.Number, Is.EqualTo(BallId.EightBallNumber));
                Assert.That(identity.IsEightBall, Is.True);
                Assert.That(identity.Group, Is.EqualTo(BallGroup.None));
                return;
            }

            if (ball.CompareTag("filled"))
            {
                Assert.That(identity.Id.Number, Is.InRange(1, 7));
                Assert.That(identity.Group, Is.EqualTo(BallGroup.Solids));
                return;
            }

            Assert.That(ball.CompareTag("striped"), Is.True, $"{ball.name} must keep a supported legacy billiard-ball tag during migration.");
            Assert.That(identity.Id.Number, Is.InRange(9, 15));
            Assert.That(identity.Group, Is.EqualTo(BallGroup.Stripes));
        }
    }
}
