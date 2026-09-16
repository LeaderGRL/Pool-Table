using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Match;
using PoolTable.Core.Rules;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Aiming;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Instrumentation;
using PoolTable.Gameplay.Match;
using PoolTable.Gameplay.Pockets;
using PoolTable.Gameplay.Shots;
using PoolTable.Input;
using PoolTable.Physics.Cloth;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Instrumentation;
using PoolTable.Physics.Pockets;
using PoolTable.Physics.Rails;
using PoolTable.Presentation;
using PoolTable.Presentation.Audio;
using PoolTable.Presentation.Camera;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    [Category("SceneSmoke")]
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
        public IEnumerator PoolTableScene_StartsWithPlayerOneBeforeAnyShotIsResolved()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var gameManager = FindActiveMonoBehaviourByTypeName(activeScene, "GameManager");
            Assert.That(gameManager, Is.Not.Null);

            var currentTurn = gameManager.GetType().GetMethod("getCurrentPlayerTurn").Invoke(gameManager, null);
            var playerOneTurn = GetPublicGameObjectField(gameManager, "UI_Player1Turn");
            var playerTwoTurn = GetPublicGameObjectField(gameManager, "UI_Player2Turn");

            Assert.That(
                currentTurn.ToString(),
                Is.EqualTo("PlayerOneTurn"),
                "Loading the scene must initialize the first turn instead of adjudicating a shot that never happened.");
            Assert.That(playerOneTurn.activeInHierarchy, Is.True);
            Assert.That(playerTwoTurn.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator PoolTableScene_SpectateStateWaitsForEveryMovingBallToStop()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");
            var ballsRoot = GameObject.Find("Balls");
            Assert.That(playerController, Is.Not.Null);
            Assert.That(ballsRoot, Is.Not.Null);

            var ballStateManagers = ballsRoot
                .GetComponentsInChildren<Component>()
                .Where(component => component.GetType().Name == "BallStateManager")
                .ToArray();
            Assert.That(ballStateManagers.Length, Is.GreaterThan(1));

            foreach (var ballStateManager in ballStateManagers)
            {
                var body = ballStateManager.GetComponent<Rigidbody>();
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            ballStateManagers[1].GetComponent<Rigidbody>().linearVelocity = Vector3.right;

            var controllerType = playerController.GetType();
            var currentStateField = controllerType.GetField(
                "currentPlayerState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var spectateState = controllerType.GetField("spectateState").GetValue(playerController);
            controllerType.GetMethod("SwitchState").Invoke(playerController, new[] { spectateState });

            spectateState.GetType().GetMethod("UpdateState").Invoke(spectateState, new object[] { playerController });

            Assert.That(
                currentStateField.GetValue(playerController).GetType().Name,
                Is.EqualTo("PlayersSpectateState"),
                "Spectating must continue while any billiard ball is still moving.");
        }

        [UnityTest]
        public IEnumerator PoolTableScene_UsesInputSystemUiModule()
        {
            yield return LoadPoolTableScene();

            var eventSystem = GameObject.Find("EventSystem");
            Assert.That(eventSystem, Is.Not.Null);

            var componentTypeNames = eventSystem
                .GetComponents<Component>()
                .Select(component => component.GetType().FullName)
                .ToArray();

            Assert.That(
                componentTypeNames,
                Does.Contain("UnityEngine.InputSystem.UI.InputSystemUIInputModule"));
            Assert.That(
                componentTypeNames,
                Does.Not.Contain("UnityEngine.EventSystems.StandaloneInputModule"));
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
        public IEnumerator PoolTableScene_UsesMetricBilliardsPhysicalScale()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var identities = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallIdentity>())
                .Where(identity => identity != null)
                .OrderBy(identity => identity.Id.Number)
                .ToArray();

            Assert.That(identities, Has.Length.EqualTo(16));

            var ballsContainer = EnumerateSceneObjects(activeScene)
                .First(gameObject => gameObject.name == "Balls");
            Assert.That(ballsContainer.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(ballsContainer.transform.lossyScale, Is.EqualTo(Vector3.one));

            var tabletop = FindActiveSolidMeshColliderByName(activeScene, "tabletop");
            Assert.That(tabletop, Is.Not.Null);
            Assert.That(
                tabletop.GetComponent<ClothSurface>(),
                Is.Not.Null,
                "The tabletop collider must explicitly identify itself as a cloth surface.");
            Assert.That(tabletop.sharedMaterial, Is.Not.Null);
            Assert.That(
                tabletop.sharedMaterial.dynamicFriction,
                Is.EqualTo(0f),
                "The tabletop must not add native PhysX friction on top of explicit cloth resistance.");
            Assert.That(
                tabletop.sharedMaterial.staticFriction,
                Is.EqualTo(0f),
                "The tabletop must not reintroduce implicit sliding-to-rolling coupling before that model is implemented.");
            Assert.That(
                tabletop.bounds.size.x,
                Is.EqualTo(BilliardsPhysicalSpecification.NineFootPlayingSurfaceLengthMeters).Within(0.001f));
            Assert.That(
                tabletop.bounds.size.z,
                Is.EqualTo(BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters).Within(0.001f));
            Assert.That(
                tabletop.bounds.max.y,
                Is.EqualTo(BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters).Within(0.001f));

            foreach (var identity in identities)
            {
                var sphere = identity.GetComponent<SphereCollider>();
                var railCollisionResponse = identity.GetComponent<BallRailCollisionResponse>();
                Assert.That(sphere, Is.Not.Null, $"Ball {identity.Id.Number} must keep a SphereCollider.");
                Assert.That(
                    railCollisionResponse,
                    Is.Not.Null,
                    $"Ball {identity.Id.Number} must use the explicit rail-collision response adapter.");
                Assert.That(
                    sphere.bounds.size.x,
                    Is.EqualTo(BilliardsPhysicalSpecification.BallDiameterMeters).Within(0.0001f),
                    $"Ball {identity.Id.Number} must use the regulation diameter on X.");
                Assert.That(
                    sphere.bounds.size.y,
                    Is.EqualTo(BilliardsPhysicalSpecification.BallDiameterMeters).Within(0.0001f),
                    $"Ball {identity.Id.Number} must use the regulation diameter on Y.");
                Assert.That(
                    sphere.bounds.size.z,
                    Is.EqualTo(BilliardsPhysicalSpecification.BallDiameterMeters).Within(0.0001f),
                    $"Ball {identity.Id.Number} must use the regulation diameter on Z.");
                Assert.That(
                    identity.transform.position.y,
                    Is.EqualTo(BilliardsPhysicalSpecification.BallCenterHeightMeters).Within(0.002f),
                    $"Ball {identity.Id.Number} must rest one radius above the reference bed height.");
            }

            var cueBall = identities.Single(identity => identity.IsCueBall);
            Assert.That(
                cueBall.transform.position.x,
                Is.EqualTo(BilliardsPhysicalSpecification.HeadStringX).Within(0.001f));
            Assert.That(cueBall.transform.position.z, Is.EqualTo(0f).Within(0.001f));

            var objectBalls = identities.Where(identity => !identity.IsCueBall).ToArray();
            var apexX = objectBalls.Min(identity => identity.transform.position.x);
            Assert.That(apexX, Is.EqualTo(BilliardsPhysicalSpecification.FootSpotX).Within(0.001f));

            var eightBall = objectBalls.Single(identity => identity.Id.Number == 8);
            var thirdRowCenterX = BilliardsPhysicalSpecification.FootSpotX
                + (2f * BilliardsPhysicalSpecification.TriangularRackRowSpacingMeters);
            Assert.That(eightBall.transform.position.x, Is.EqualTo(thirdRowCenterX).Within(0.0002f));
            Assert.That(eightBall.transform.position.z, Is.EqualTo(0f).Within(0.0002f));

            var rearRowX = objectBalls.Max(identity => identity.transform.position.x);
            var rearRow = objectBalls
                .Where(identity => Mathf.Abs(identity.transform.position.x - rearRowX) <= 0.0002f)
                .OrderBy(identity => identity.transform.position.z)
                .ToArray();
            Assert.That(rearRow, Has.Length.EqualTo(5));
            Assert.That(rearRow.First().Id.Group, Is.Not.EqualTo(rearRow.Last().Id.Group));
            Assert.That(
                new[] { rearRow.First().Id.Group, rearRow.Last().Id.Group },
                Is.EquivalentTo(new[] { BallGroup.Solids, BallGroup.Stripes }),
                "The two rear corners of an 8-ball rack must contain opposite groups.");

            for (var firstIndex = 0; firstIndex < objectBalls.Length; firstIndex++)
            {
                var first = objectBalls[firstIndex].transform.position;
                var hasTouchingNeighbor = false;

                for (var secondIndex = 0; secondIndex < objectBalls.Length; secondIndex++)
                {
                    if (firstIndex == secondIndex)
                    {
                        continue;
                    }

                    var distance = Vector3.Distance(first, objectBalls[secondIndex].transform.position);
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(BilliardsPhysicalSpecification.BallDiameterMeters - 0.0002f),
                        "The initial rack must not contain overlapping balls.");

                    if (Mathf.Abs(distance - BilliardsPhysicalSpecification.BallDiameterMeters) <= 0.0002f)
                    {
                        hasTouchingNeighbor = true;
                    }
                }

                Assert.That(
                    hasTouchingNeighbor,
                    Is.True,
                    $"Object ball {objectBalls[firstIndex].Id.Number} must touch the initial triangular rack.");
            }
        }

        [UnityTest]
        public IEnumerator PoolTableScene_UsesExplicitRailCollisionSurfaces()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var cushion = FindActiveSolidMeshColliderByName(activeScene, "rubber");
            var sideRail = FindActiveSolidMeshColliderByName(activeScene, "sides");

            AssertRailSurface(cushion, "The rubber cushion");
            AssertRailSurface(sideRail, "The side-rail fallback geometry");
        }

        [UnityTest]
        public IEnumerator PoolTableScene_UsesSixTypedPocketCaptureVolumes()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var volumes = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<PocketCaptureVolume>())
                .Where(volume => volume != null)
                .OrderBy(volume => volume.Pocket.Index)
                .ToArray();

            Assert.That(volumes, Has.Length.EqualTo(PocketId.MaximumIndex));
            Assert.That(
                volumes.Select(volume => volume.Pocket.Index),
                Is.EqualTo(Enumerable.Range(PocketId.MinimumIndex, PocketId.MaximumIndex)));

            foreach (var volume in volumes)
            {
                Assert.That(volume.TriggerCollider.isTrigger, Is.True);
                Assert.That(
                    volume.TriggerCollider.radius,
                    Is.EqualTo(BilliardsPhysicalSpecification.PocketCaptureRadiusMeters).Within(0.000001f));
                Assert.That(
                    Vector3.Distance(volume.transform.position, PocketCaptureLayout.GetCenter(volume.Pocket)),
                    Is.LessThan(0.0001f),
                    $"Pocket {volume.Pocket.Index} must remain on the authoritative metric capture layout.");
            }

            var ballCaptures = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallPocketCapture>())
                .Where(capture => capture != null)
                .ToArray();
            Assert.That(ballCaptures, Has.Length.EqualTo(16));
            Assert.That(SceneContainsObject(activeScene, "pocket_destroy"), Is.False);
        }

        [UnityTest]
        public IEnumerator PocketCapture_PreservesLegacyTurnAndScratchFlowDuringMigration()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var volume = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<PocketCaptureVolume>())
                .First(candidate => candidate != null && candidate.Pocket == new PocketId(1));
            var identities = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallIdentity>())
                .Where(identity => identity != null)
                .ToArray();
            var objectBallIdentity = identities.First(identity => identity.Id.Number == 1);
            var cueBallIdentity = identities.First(identity => identity.Id.IsCueBall);
            var objectBall = objectBallIdentity.gameObject;
            var cueBall = cueBallIdentity.gameObject;
            var objectCapture = objectBall.GetComponent<BallPocketCapture>();
            var cueCapture = cueBall.GetComponent<BallPocketCapture>();
            var anyLegacyBallManager = FindActiveMonoBehaviourByTypeName(activeScene, "BallStateManager");
            var ballManagerType = anyLegacyBallManager.GetType();
            var legacyAuthority = ballManagerType
                .GetField("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null) as MonoBehaviour;
            var pocketedBalls = legacyAuthority.GetType()
                .GetMethod("getPocketedBalls")
                .Invoke(legacyAuthority, null) as IDictionary;
            var gameManager = FindActiveMonoBehaviourByTypeName(activeScene, "GameManager");
            var turnNumberField = gameManager.GetType().GetField("turnNumber");
            var initialTurnNumber = (int)turnNumberField.GetValue(gameManager);
            var initialPocketedCount = pocketedBalls.Count;

            Assert.That(volume.TryCapture(objectCapture), Is.True);
            Assert.That(objectBall.activeSelf, Is.False);
            Assert.That(pocketedBalls.Contains(objectBall), Is.True);
            Assert.That(pocketedBalls.Count, Is.EqualTo(initialPocketedCount + 1));

            Assert.That(volume.TryCapture(objectCapture), Is.False);
            Assert.That(pocketedBalls.Count, Is.EqualTo(initialPocketedCount + 1));

            turnNumberField.SetValue(gameManager, initialTurnNumber + 1);
            Assert.That(
                (bool)legacyAuthority.GetType().GetMethod("IsBallPocketedLastTurn").Invoke(legacyAuthority, null),
                Is.True,
                "The legacy turn resolver must still observe object balls captured by the typed pocket system.");

            var cueInitialPosition = cueBall.transform.position;
            Assert.That(volume.TryCapture(cueCapture), Is.True);
            Assert.That(cueBall.activeSelf, Is.False);
            Assert.That(cueCapture.IsCaptured, Is.True);
            Assert.That(
                (bool)legacyAuthority.GetType().GetMethod("isPocketedBallContainWhiteBall").Invoke(legacyAuthority, null),
                Is.True,
                "The legacy scratch resolver must still observe the cue ball captured by the typed pocket system.");

            legacyAuthority.GetType().GetMethod("resetWhiteBallFromPocket").Invoke(legacyAuthority, null);

            Assert.That(cueBall.activeSelf, Is.True);
            Assert.That(cueCapture.IsCaptured, Is.False);
            Assert.That(cueCapture.CapturedPocket, Is.Null);
            Assert.That(Vector3.Distance(cueBall.transform.position, cueInitialPosition), Is.LessThan(0.0001f));
            Assert.That(pocketedBalls.Contains(cueBall), Is.False);
        }

        [UnityTest]
        public IEnumerator BallInHandPlacement_RestoresCapturedCueBallOnlyAfterLegalConfirmation()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var controller = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallInHandPlacementController>())
                .Single(candidate => candidate != null);
            var identities = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallIdentity>())
                .Where(identity => identity != null)
                .ToArray();
            var cueBallIdentity = identities.Single(identity => identity.IsCueBall);
            var objectBallIdentity = identities.Single(identity => identity.Id.Number == 1);
            var cueBall = cueBallIdentity.gameObject;
            var cueBody = cueBall.GetComponent<Rigidbody>();
            var cueCapture = cueBall.GetComponent<BallPocketCapture>();
            var legacyPlayerState = EnumerateSceneObjects(activeScene)
                .Select(gameObject => FindMonoBehaviourByTypeName(gameObject, "PlayersStateManagement"))
                .First(component => component != null && component.gameObject.activeInHierarchy);
            var placementField = legacyPlayerState.GetType().GetField("ballInHandPlacementController");

            Assert.That(controller.enabled, Is.False);
            Assert.That(placementField, Is.Not.Null);
            Assert.That(placementField.GetValue(legacyPlayerState), Is.SameAs(controller));

            var anyLegacyBallManager = FindActiveMonoBehaviourByTypeName(activeScene, "BallStateManager");
            var ballManagerType = anyLegacyBallManager.GetType();
            var legacyAuthority = ballManagerType
                .GetField("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null) as MonoBehaviour;
            var pocketedBalls = legacyAuthority.GetType()
                .GetMethod("getPocketedBalls")
                .Invoke(legacyAuthority, null) as IDictionary;

            cueBody.linearVelocity = new Vector3(0.8f, 0f, 0.2f);
            cueBody.angularVelocity = new Vector3(2f, 3f, 4f);
            Assert.That(cueCapture.TryCapture(new PocketId(1), out _), Is.True);
            Assert.That(cueBall.activeSelf, Is.False);
            Assert.That(pocketedBalls.Contains(cueBall), Is.True);

            var openTable = OpenTableRule.EnterAfterBreak(MatchState.CreateInitial(MatchPlayerId.PlayerOne));
            var granted = BallInHandRule.GrantAfterStandardFoul(
                openTable,
                new FoulResolution(ShotFoul.CueBallScratch));

            var cursorState = new TestCursorStateAccessor(CursorLockMode.Locked, false);
            controller.CursorStateAccessor = cursorState;

            controller.BeginPlacement(granted);

            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.IsPlacing, Is.True);
            Assert.That(cueBall.activeSelf, Is.True);
            Assert.That(cueCapture.IsCaptured, Is.True);
            Assert.That(cueBody.isKinematic, Is.True);
            Assert.That(cueBody.detectCollisions, Is.False);
            Assert.That(cursorState.LockState, Is.EqualTo(CursorLockMode.Confined));
            Assert.That(cursorState.Visible, Is.True);

            var placementCamera = Camera.main;
            Assert.That(placementCamera, Is.Not.Null);

            var tableCenter = new Vector3(
                0f,
                BilliardsPhysicalSpecification.ReferenceTableBedHeightMeters,
                0f);
            var directionToTableCenter = (tableCenter - placementCamera.transform.position).normalized;
            Assert.That(Vector3.Dot(placementCamera.transform.forward, directionToTableCenter), Is.GreaterThan(0.999f));
            Assert.That(
                placementCamera.transform.position.z,
                Is.LessThan(-BilliardsPhysicalSpecification.NineFootPlayingSurfaceWidthMeters * 0.5f),
                "Direct ball-in-hand placement must immediately use the side-overview camera.");

            var pointerPosition = placementCamera.pixelRect.center;
            Assert.That(controller.TryProjectPointerToTable(pointerPosition, out var projectedPosition), Is.True);
            var expectedPointerPosition = BallInHandPlacementGeometry.ClampToPlacementArea(
                projectedPosition,
                CueBallPlacementArea.Anywhere);

            controller.MoveCandidate(
                new LocalPlayerInputSnapshot(
                    pointerDelta: Vector2.one,
                    primaryActionIsPressed: false,
                    pointerPosition: pointerPosition,
                    hasPointerPosition: true),
                0f);

            Assert.That(
                Vector2.Distance(controller.CurrentPlanarPosition, expectedPointerPosition),
                Is.LessThan(0.0001f),
                "Mouse ball-in-hand input must place the cue-ball candidate directly under the projected pointer position.");

            var beforeControllerMove = controller.CurrentPlanarPosition;
            var controllerAxis = new Vector2(0.2f, -0.15f);
            const float controllerMoveDuration = 0.25f;
            var expectedControllerPosition = BallInHandPlacementGeometry.ClampToPlacementArea(
                beforeControllerMove + new Vector2(
                    controllerAxis.y * 0.75f * controllerMoveDuration,
                    controllerAxis.x * 0.75f * controllerMoveDuration),
                CueBallPlacementArea.Anywhere);

            controller.MoveCandidate(
                new LocalPlayerInputSnapshot(
                    pointerDelta: Vector2.zero,
                    primaryActionIsPressed: false,
                    actionAxis: controllerAxis),
                controllerMoveDuration);

            Assert.That(
                Vector2.Distance(controller.CurrentPlanarPosition, expectedControllerPosition),
                Is.LessThan(0.0001f),
                "Controller ball-in-hand input must preserve relative movement while mouse placement uses direct projection.");

            var alternateOpenTable = OpenTableRule.EnterAfterBreak(
                MatchState.CreateInitial(MatchPlayerId.PlayerTwo));
            var alternateGranted = BallInHandRule.GrantAfterStandardFoul(
                alternateOpenTable,
                new FoulResolution(ShotFoul.CueBallScratch));

            Assert.Throws<System.InvalidOperationException>(() => controller.BeginPlacement(alternateGranted));
            Assert.Throws<System.InvalidOperationException>(() => controller.BeginLegacyScratchPlacement());

            cueBall.transform.position = new Vector3(
                objectBallIdentity.transform.position.x,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                objectBallIdentity.transform.position.z);

            Assert.That(controller.TryConfirmPlacement(), Is.False);
            Assert.That(controller.IsPlacing, Is.True);
            Assert.That(controller.LastCompletedMatchState, Is.Null);
            Assert.That(granted.HasBallInHand, Is.True);
            Assert.That(cueCapture.IsCaptured, Is.True);

            var acceptedPosition = new Vector3(
                BilliardsPhysicalSpecification.HeadStringX,
                BilliardsPhysicalSpecification.BallCenterHeightMeters,
                0f);
            cueBall.transform.position = acceptedPosition;

            Assert.That(controller.TryConfirmPlacement(), Is.True);
            Assert.That(controller.enabled, Is.False);
            Assert.That(controller.IsPlacing, Is.False);
            Assert.That(controller.LastCompletedMatchState, Is.Not.Null);
            Assert.That(controller.LastCompletedMatchState.HasBallInHand, Is.False);
            Assert.That(controller.LastCompletedMatchState.CurrentPlayer, Is.EqualTo(MatchPlayerId.PlayerTwo));
            Assert.That(cueCapture.IsCaptured, Is.False);
            Assert.That(cueCapture.CapturedPocket, Is.Null);
            Assert.That(cueBody.isKinematic, Is.False);
            Assert.That(cueBody.detectCollisions, Is.True);
            Assert.That(cueBody.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(cueBody.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(Vector3.Distance(cueBall.transform.position, acceptedPosition), Is.LessThan(0.0001f));
            Assert.That(pocketedBalls.Contains(cueBall), Is.False);
            Assert.That(cursorState.LockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(cursorState.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator RailCollisionResponse_ReboundsBallFromMarkedStaticRail()
        {
            var railObject = new GameObject("RailCollisionResponseTestRail");
            var ballObject = new GameObject("RailCollisionResponseTestBall");
            var material = new PhysicsMaterial("RailCollisionResponseTestMaterial")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            try
            {
                var railCollider = railObject.AddComponent<BoxCollider>();
                railCollider.size = new Vector3(0.1f, 0.2f, 0.5f);
                railCollider.sharedMaterial = material;
                railObject.AddComponent<RailSurface>();
                railObject.transform.position = new Vector3(0.2f, 0f, 0f);

                var ballCollider = ballObject.AddComponent<SphereCollider>();
                ballCollider.radius = BilliardsPhysicalSpecification.BallRadiusMeters;
                ballCollider.sharedMaterial = material;
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.mass = BilliardsSimulationConfiguration.BallMassKilograms;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                ballObject.AddComponent<BallRailCollisionResponse>();
                ballObject.transform.position = Vector3.zero;
                rigidbody.linearVelocity = Vector3.right * 2f;

                for (var step = 0; step < 60 && rigidbody.linearVelocity.x >= 0f; step++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(rigidbody.linearVelocity.x, Is.LessThan(-1f));
                Assert.That(
                    Mathf.Abs(rigidbody.linearVelocity.x),
                    Is.LessThan(2f),
                    "The rail response must rebound with controlled energy loss.");
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
                Object.DestroyImmediate(railObject);
                Object.DestroyImmediate(material);
            }
        }

        [UnityTest]
        public IEnumerator RailCollisionResponse_CoalescesOverlappingRailCollidersPerPhysicsStep()
        {
            var firstRailObject = new GameObject("CoalescedRailCollisionResponseTestRailA");
            var secondRailObject = new GameObject("CoalescedRailCollisionResponseTestRailB");
            var ballObject = new GameObject("CoalescedRailCollisionResponseTestBall");
            var material = new PhysicsMaterial("CoalescedRailCollisionResponseTestMaterial")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            try
            {
                foreach (var railObject in new[] { firstRailObject, secondRailObject })
                {
                    var railCollider = railObject.AddComponent<BoxCollider>();
                    railCollider.size = new Vector3(0.1f, 0.2f, 0.5f);
                    railCollider.sharedMaterial = material;
                    railObject.AddComponent<RailSurface>();
                    railObject.transform.position = new Vector3(0.2f, 0f, 0f);
                }

                var ballCollider = ballObject.AddComponent<SphereCollider>();
                ballCollider.radius = BilliardsPhysicalSpecification.BallRadiusMeters;
                ballCollider.sharedMaterial = material;
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.mass = BilliardsSimulationConfiguration.BallMassKilograms;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var railResponse = ballObject.AddComponent<BallRailCollisionResponse>();
                var resolvedRailObservations = new List<RailCollisionObservation>();
                railResponse.RailCollisionResolved += resolvedRailObservations.Add;
                ballObject.transform.position = Vector3.zero;
                rigidbody.linearVelocity = Vector3.right * 2f;

                for (var step = 0; step < 60 && rigidbody.linearVelocity.x >= 0f; step++)
                {
                    yield return new WaitForFixedUpdate();
                }

                yield return new WaitForFixedUpdate();

                Assert.That(rigidbody.linearVelocity.x, Is.LessThan(-1f));
                Assert.That(
                    Mathf.Abs(rigidbody.linearVelocity.x),
                    Is.LessThan(2f),
                    "Multiple rail colliders in one physics step must share one custom restitution response.");
                Assert.That(
                    resolvedRailObservations,
                    Has.Count.EqualTo(1),
                    "Overlapping rail colliders in one physics step must emit one aggregated rail observation.");

                var expectedResponse = RailCollisionResponseModel.CalculateManifoldResponse(
                    Vector3.right * 2f,
                    Vector3.zero,
                    new[] { Vector3.left, Vector3.left },
                    BilliardsPhysicalSpecification.BallRadiusMeters);
                var expectedImpulse = (expectedResponse.LinearVelocity - (Vector3.right * 2f)) * rigidbody.mass;
                Assert.That(
                    Vector3.Distance(resolvedRailObservations[0].AppliedLinearImpulse, expectedImpulse),
                    Is.LessThan(0.000001f));
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
                Object.DestroyImmediate(firstRailObject);
                Object.DestroyImmediate(secondRailObject);
                Object.DestroyImmediate(material);
            }
        }

        [UnityTest]
        public IEnumerator RigidbodySimulationProbe_BeginRecordingDiscardsPendingRailObservation()
        {
            var railObject = new GameObject("PendingRailObservationTestRail");
            var ballObject = new GameObject("PendingRailObservationTestBall");
            var material = new PhysicsMaterial("PendingRailObservationTestMaterial")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            var previousSimulationMode = UnityEngine.Physics.simulationMode;

            try
            {
                UnityEngine.Physics.simulationMode = SimulationMode.Script;

                const float railCenterX = 0.2f;
                const float railHalfWidth = 0.05f;
                var railCollider = railObject.AddComponent<BoxCollider>();
                railCollider.size = new Vector3(railHalfWidth * 2f, 0.2f, 0.5f);
                railCollider.sharedMaterial = material;
                railObject.AddComponent<RailSurface>();
                railObject.transform.position = new Vector3(railCenterX, 0f, 0f);

                var ballCollider = ballObject.AddComponent<SphereCollider>();
                ballCollider.radius = BilliardsPhysicalSpecification.BallRadiusMeters;
                ballCollider.sharedMaterial = material;
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.mass = BilliardsSimulationConfiguration.BallMassKilograms;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var railResponse = ballObject.AddComponent<BallRailCollisionResponse>();
                var probe = ballObject.AddComponent<RigidbodySimulationProbe>();
                var observations = new List<ProbeCollisionObservation>();
                probe.CollisionObserved += (_, observation) => observations.Add(observation);

                rigidbody.position = new Vector3(
                    railCenterX - railHalfWidth - BilliardsPhysicalSpecification.BallRadiusMeters - 0.001f,
                    0f,
                    0f);
                rigidbody.linearVelocity = Vector3.right * 2f;
                UnityEngine.Physics.SyncTransforms();

                UnityEngine.Physics.Simulate(BilliardsSimulationConfiguration.FixedTimestepSeconds);
                Assert.That(rigidbody.linearVelocity.x, Is.LessThan(0f), "The scripted step must create a pending rail observation.");

                rigidbody.position = Vector3.zero;
                rigidbody.linearVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();

                probe.BeginRecording(1d);
                railResponse.FlushPendingObservation();
                probe.StopRecording(1.01d);

                Assert.That(
                    observations,
                    Is.Empty,
                    "A rail observation created before the shot boundary must not leak into the new recording.");
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousSimulationMode;
                Object.DestroyImmediate(ballObject);
                Object.DestroyImmediate(railObject);
                Object.DestroyImmediate(material);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RailCollisionResponse_ReboundsRenewedImpactDuringPersistentContact()
        {
            var railObject = new GameObject("PersistentRailCollisionResponseTestRail");
            var ballObject = new GameObject("PersistentRailCollisionResponseTestBall");
            var material = new PhysicsMaterial("PersistentRailCollisionResponseTestMaterial")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            try
            {
                const float railCenterX = 0.2f;
                const float railHalfWidth = 0.05f;
                var railCollider = railObject.AddComponent<BoxCollider>();
                railCollider.size = new Vector3(railHalfWidth * 2f, 0.2f, 0.5f);
                railCollider.sharedMaterial = material;
                railObject.AddComponent<RailSurface>();
                railObject.transform.position = new Vector3(railCenterX, 0f, 0f);

                var ballCollider = ballObject.AddComponent<SphereCollider>();
                ballCollider.radius = BilliardsPhysicalSpecification.BallRadiusMeters;
                ballCollider.sharedMaterial = material;
                var rigidbody = ballObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.mass = BilliardsSimulationConfiguration.BallMassKilograms;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                ballObject.AddComponent<BallRailCollisionResponse>();
                ballObject.transform.position = new Vector3(
                    railCenterX - railHalfWidth - BilliardsPhysicalSpecification.BallRadiusMeters,
                    0f,
                    0f);

                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                rigidbody.linearVelocity = Vector3.right * 2f;
                yield return new WaitForFixedUpdate();

                Assert.That(
                    rigidbody.linearVelocity.x,
                    Is.LessThan(-1f),
                    "A renewed inward impact while contact persists must use the configured rail rebound.");
                Assert.That(
                    Mathf.Abs(rigidbody.linearVelocity.x),
                    Is.LessThan(2f),
                    "The renewed rail rebound must retain controlled energy loss.");
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
                Object.DestroyImmediate(railObject);
                Object.DestroyImmediate(material);
            }
        }

        [UnityTest]
        public IEnumerator PoolTableScene_UsesBilliardsRigidbodySimulationConfiguration()
        {
            yield return LoadPoolTableScene();

            Assert.That(
                Time.fixedDeltaTime,
                Is.EqualTo(BilliardsSimulationConfiguration.FixedTimestepSeconds).Within(0.000001f));
            Assert.That(
                UnityEngine.Physics.sleepThreshold,
                Is.EqualTo(BilliardsSimulationConfiguration.GlobalSleepThreshold).Within(0.000001f));
            Assert.That(
                UnityEngine.Physics.defaultContactOffset,
                Is.EqualTo(BilliardsSimulationConfiguration.DefaultContactOffsetMeters).Within(0.000001f));
            Assert.That(
                UnityEngine.Physics.defaultSolverIterations,
                Is.EqualTo(BilliardsSimulationConfiguration.DefaultSolverIterations));
            Assert.That(
                UnityEngine.Physics.defaultSolverVelocityIterations,
                Is.EqualTo(BilliardsSimulationConfiguration.DefaultSolverVelocityIterations));

            var identities = Object.FindObjectsByType<BallIdentity>(FindObjectsSortMode.None);
            Assert.That(identities, Has.Length.EqualTo(16));

            foreach (var identity in identities)
            {
                var rigidbody = identity.GetComponent<Rigidbody>();

                Assert.That(rigidbody, Is.Not.Null, $"Ball {identity.Id.Number} must keep a Rigidbody.");
                Assert.That(
                    rigidbody.mass,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallMassKilograms).Within(0.000001f),
                    $"Ball {identity.Id.Number} must use the billiards mass baseline.");
                Assert.That(
                    rigidbody.linearDamping,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallLinearDamping).Within(0.000001f),
                    $"Ball {identity.Id.Number} must not hide cloth resistance in Rigidbody linear damping.");
                Assert.That(
                    rigidbody.angularDamping,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallAngularDamping).Within(0.000001f),
                    $"Ball {identity.Id.Number} must not hide rotational resistance in Rigidbody angular damping.");
                Assert.That(
                    rigidbody.maxAngularVelocity,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallMaxAngularVelocityRadiansPerSecond).Within(0.000001f),
                    $"Ball {identity.Id.Number} must allow regulation-radius rolling angular speeds.");
                Assert.That(
                    identity.GetComponent<BallClothResistance>(),
                    Is.Not.Null,
                    $"Ball {identity.Id.Number} must use the explicit cloth-resistance component.");
                Assert.That(rigidbody.useGravity, Is.True, $"Ball {identity.Id.Number} must remain gravity-enabled.");
                Assert.That(rigidbody.isKinematic, Is.False, $"Ball {identity.Id.Number} must remain dynamic.");
                Assert.That(
                    rigidbody.collisionDetectionMode,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallCollisionDetectionMode),
                    $"Ball {identity.Id.Number} must use continuous dynamic collision detection.");
                Assert.That(
                    rigidbody.interpolation,
                    Is.EqualTo(BilliardsSimulationConfiguration.BallInterpolation),
                    $"Ball {identity.Id.Number} must use the billiards interpolation baseline.");
            }
        }

        [UnityTest]
        public IEnumerator PoolTableScene_VerticalVelocityDampingIsTimestepIndependent()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var ballStateManager = FindActiveMonoBehaviourByTypeName(activeScene, "BallStateManager");
            Assert.That(ballStateManager, Is.Not.Null);

            var getUpwardVelocityRetention = ballStateManager.GetType().GetMethod(
                "GetUpwardVelocityRetention",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(getUpwardVelocityRetention, Is.Not.Null);

            var legacyTickRetention = (float)getUpwardVelocityRetention.Invoke(null, new object[] { 0.02f });
            var currentTickRetention = (float)getUpwardVelocityRetention.Invoke(
                null,
                new object[] { BilliardsSimulationConfiguration.FixedTimestepSeconds });
            var currentRetentionOverLegacyInterval = Mathf.Pow(
                currentTickRetention,
                0.02f / BilliardsSimulationConfiguration.FixedTimestepSeconds);

            Assert.That(legacyTickRetention, Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(
                currentRetentionOverLegacyInterval,
                Is.EqualTo(legacyTickRetention).Within(0.000001f),
                "Vertical damping must preserve the legacy 20 ms behavior when the physics timestep changes.");
        }

        [UnityTest]
        public IEnumerator PoolTableScene_ModernShotPowerIsIndependentOfBallMass()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");
            Assert.That(playerController, Is.Not.Null);

            var shotPowerController = playerController.GetComponent<ShotPowerController>();
            var spinController = playerController.GetComponent<CueBallSpinController>();
            Assert.That(shotPowerController, Is.Not.Null, "The active cue must own the modern shot-power adapter.");
            Assert.That(spinController, Is.Not.Null, "The active cue must own the modern cue-ball spin adapter.");
            Assert.That(shotPowerController.enabled, Is.False, "Shot power must stay disabled while aiming.");
            Assert.That(spinController.Spin.IsCentered, Is.True);

            var cueBall = shotPowerController.CueBall;
            var originalMass = cueBall.mass;
            var originalPosition = cueBall.position;
            var originalRotation = cueBall.rotation;
            var measuredSpeeds = new float[2];
            var masses = new[] { BilliardsSimulationConfiguration.BallMassKilograms, 1f };

            for (var massIndex = 0; massIndex < masses.Length; massIndex++)
            {
                cueBall.mass = masses[massIndex];
                cueBall.position = originalPosition;
                cueBall.rotation = originalRotation;
                cueBall.linearVelocity = Vector3.zero;
                cueBall.angularVelocity = Vector3.zero;

                shotPowerController.enabled = true;
                Assert.That(shotPowerController.NormalizedPower, Is.Zero);

                shotPowerController.ProcessInput(
                    new LocalPlayerInputSnapshot(new Vector2(0f, 10f), true));
                Assert.That(
                    shotPowerController.PullbackMeters,
                    Is.Zero,
                    "The pointer delta from the frame that enables shot power must not become accidental pullback.");

                yield return null;

                for (var stroke = 0; stroke < 18; stroke++)
                {
                    shotPowerController.ProcessInput(
                        new LocalPlayerInputSnapshot(new Vector2(0f, 10f), true));
                }

                Assert.That(shotPowerController.NormalizedPower, Is.EqualTo(1f).Within(0.000001f));
                Assert.That(
                    shotPowerController.PullbackMeters,
                    Is.EqualTo(shotPowerController.MaximumCuePullbackMeters).Within(0.000001f));

                shotPowerController.ProcessInput(new LocalPlayerInputSnapshot(Vector2.zero, false));

                Assert.That(
                    shotPowerController.ShotCommitted,
                    Is.False,
                    "Releasing the cue must queue the strike until the next physics step.");
                Assert.That(
                    shotPowerController.enabled,
                    Is.True,
                    "Shot power must remain active until the queued strike reaches FixedUpdate.");

                yield return new WaitForFixedUpdate();

                Assert.That(shotPowerController.ShotCommitted, Is.True);
                Assert.That(spinController.Spin.IsCentered, Is.True, "A committed shot must leave the next shot centered.");
                Assert.That(
                    shotPowerController.enabled,
                    Is.False,
                    "A physically committed shot must disable further power input.");
                Assert.That(
                    Mathf.Abs(cueBall.angularVelocity.y),
                    Is.LessThan(0.0001f),
                    "Centered strikes must not introduce side spin.");

                measuredSpeeds[massIndex] = Vector3.ProjectOnPlane(cueBall.linearVelocity, Vector3.up).magnitude;
            }

            Assert.That(
                measuredSpeeds[0],
                Is.EqualTo(measuredSpeeds[1]).Within(0.0001f),
                "Modern cue-ball shot speed must not change when Rigidbody mass changes.");
            Assert.That(
                measuredSpeeds[0],
                Is.GreaterThan(shotPowerController.MaximumShotSpeedMetersPerSecond - 0.1f),
                "A full-power shot must remain close to the configured target speed after one physics step.");

            cueBall.mass = originalMass;
            cueBall.position = originalPosition;
            cueBall.rotation = originalRotation;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
        }

        [UnityTest]
        public IEnumerator PoolTableScene_WiresModernAimingAcrossLegacyPlayerStates()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");
            Assert.That(playerController, Is.Not.Null);

            var aimingController = playerController.GetComponent<CueAimingController>();
            var spinController = playerController.GetComponent<CueBallSpinController>();
            var shotPowerController = playerController.GetComponent<ShotPowerController>();
            Assert.That(aimingController, Is.Not.Null, "The active cue must use the modern gameplay aiming adapter.");
            Assert.That(spinController, Is.Not.Null, "The active cue must use the modern gameplay spin adapter.");
            Assert.That(shotPowerController, Is.Not.Null, "The active cue must use the modern gameplay shot-power adapter.");
            Assert.That(aimingController.isActiveAndEnabled, Is.True, "Aiming must be enabled while the player is in play state.");
            Assert.That(spinController.isActiveAndEnabled, Is.True, "Spin selection must be enabled while the player is aiming.");
            Assert.That(shotPowerController.enabled, Is.False, "Shot power must be disabled while the player is aiming.");
            Assert.That(aimingController.CueBall, Is.Not.Null);
            Assert.That(aimingController.CueDistance, Is.EqualTo(1.6666667f).Within(0.0001f));
            Assert.That(aimingController.MinimumElevationDegrees, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(aimingController.MaximumElevationDegrees, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(
                aimingController.YawDegreesPerPointerUnit,
                Is.EqualTo(0.1f).Within(0.0001f),
                "Modern aiming must preserve the legacy 0.1 sensitivity applied to raw Input System pointer delta.");
            Assert.That(
                aimingController.PitchDegreesPerPointerUnit,
                Is.EqualTo(0.1f).Within(0.0001f),
                "Cue elevation must use the same raw-pointer sensitivity baseline as planar aiming.");
            Assert.That(
                spinController.NormalizedUnitsPerPointerUnit,
                Is.EqualTo(0.01f).Within(0.0001f),
                "Spin selection must map pointer movement into normalized cue-tip contact units.");
            Assert.That(shotPowerController.SpinController, Is.SameAs(spinController));

            var direction = aimingController.Direction;
            var initialElevation = aimingController.ElevationDegrees;
            var magnitudeSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
            Assert.That(magnitudeSquared, Is.EqualTo(1f).Within(0.00001f));

            var outputCameraObject = Camera.main.gameObject;
            var aimingCameraController = outputCameraObject.GetComponent<AimingCameraController>();
            var shotCameraController = outputCameraObject.GetComponent<ShotCameraController>();
            var spectateCameraController = outputCameraObject.GetComponent<SpectateCameraController>();
            Assert.That(aimingCameraController, Is.Not.Null, "The output camera must use the modern aiming-camera adapter.");
            Assert.That(shotCameraController, Is.Not.Null, "The output camera must use the modern shot-camera adapter.");
            Assert.That(spectateCameraController, Is.Not.Null, "The output camera must use the modern spectate-camera adapter.");
            Assert.That(aimingCameraController.AimingController, Is.SameAs(aimingController));
            Assert.That(shotCameraController.ShotPowerController, Is.SameAs(shotPowerController));
            Assert.That(shotCameraController.enabled, Is.False, "Shot presentation must be inactive while aiming.");
            Assert.That(spectateCameraController.enabled, Is.False, "Spectate presentation must be inactive while aiming.");
            AssertAimingCameraMatchesDirection(aimingCameraController, aimingController);

            var spinInput = new LocalPlayerInputSnapshot(new Vector2(20f, -10f), false, true);
            aimingController.ProcessInput(spinInput);
            spinController.ProcessInput(spinInput);

            Assert.That(aimingController.Direction.X, Is.EqualTo(direction.X).Within(0.00001f));
            Assert.That(aimingController.Direction.Y, Is.EqualTo(direction.Y).Within(0.00001f));
            Assert.That(aimingController.ElevationDegrees, Is.EqualTo(initialElevation).Within(0.00001f));
            Assert.That(spinController.Spin.Side, Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(spinController.Spin.Vertical, Is.EqualTo(-0.1f).Within(0.00001f));

            var directionToCueBall = (aimingController.CueBall.transform.position - playerController.transform.position).normalized;
            Assert.That(
                Vector3.Dot(playerController.transform.forward, directionToCueBall),
                Is.EqualTo(1f).Within(0.0001f),
                "The physical cue must point through the cue-ball center while gameplay aim stays planar.");

            var planarForward = Vector3.ProjectOnPlane(playerController.transform.forward, Vector3.up).normalized;
            Assert.That(planarForward.x, Is.EqualTo(direction.X).Within(0.0001f));
            Assert.That(planarForward.z, Is.EqualTo(direction.Y).Within(0.0001f));

            aimingController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(15f, 8f), false));
            Assert.That(
                aimingController.Direction.X,
                Is.EqualTo(direction.X).Within(0.00001f),
                "The secondary-button release frame must not leak the final spin-drag delta into cue yaw.");
            Assert.That(aimingController.Direction.Y, Is.EqualTo(direction.Y).Within(0.00001f));
            Assert.That(
                aimingController.ElevationDegrees,
                Is.EqualTo(initialElevation).Within(0.00001f),
                "The secondary-button release frame must not leak the final spin-drag delta into cue elevation.");

            aimingController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(15f, 8f), false));
            Assert.That(
                Mathf.Abs(aimingController.Direction.X - direction.X)
                    + Mathf.Abs(aimingController.Direction.Y - direction.Y),
                Is.GreaterThan(0.00001f),
                "Cue yaw must resume on the frame after the secondary-button release transition.");
            Assert.That(
                aimingController.ElevationDegrees,
                Is.GreaterThan(initialElevation),
                "Vertical pointer input must raise cue elevation once aiming resumes.");
            AssertAimingCameraMatchesDirection(aimingCameraController, aimingController);

            aimingController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(-15f, -8f), false));
            Assert.That(aimingController.Direction.X, Is.EqualTo(direction.X).Within(0.00001f));
            Assert.That(aimingController.Direction.Y, Is.EqualTo(direction.Y).Within(0.00001f));
            Assert.That(aimingController.ElevationDegrees, Is.EqualTo(initialElevation).Within(0.00001f));

            aimingController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(0f, 10000f), false));
            Assert.That(aimingController.ElevationDegrees, Is.EqualTo(20f).Within(0.0001f));
            var elevatedStrikeDirection = aimingController.StrikeDirection;
            Assert.That(elevatedStrikeDirection.y, Is.LessThan(0f));
            Assert.That(elevatedStrikeDirection.magnitude, Is.EqualTo(1f).Within(0.0001f));
            var elevatedDirectionToCueBall = (aimingController.CueBall.transform.position - playerController.transform.position).normalized;
            Assert.That(
                Vector3.Dot(playerController.transform.forward, elevatedDirectionToCueBall),
                Is.EqualTo(1f).Within(0.0001f),
                "The cue transform must stay aligned with the elevated strike direction.");

            aimingController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(0f, -10000f), false));
            Assert.That(aimingController.ElevationDegrees, Is.EqualTo(0f).Within(0.0001f));

            var controllerType = playerController.GetType();
            Assert.That(controllerType.GetMethod("setRotation"), Is.Null, "Legacy player code must no longer own cue rotation.");
            Assert.That(controllerType.GetMethod("turnArround"), Is.Null, "Legacy pointer-driven rotation must be removed.");

            var aimingBehaviour = controllerType.GetField("aimingController").GetValue(playerController);
            var spinBehaviour = controllerType.GetField("spinController").GetValue(playerController);
            var shotPowerBehaviour = controllerType.GetField("shotPowerController").GetValue(playerController);
            Assert.That(aimingBehaviour, Is.SameAs(aimingController));
            Assert.That(spinBehaviour, Is.SameAs(spinController));
            Assert.That(shotPowerBehaviour, Is.SameAs(shotPowerController));
            Assert.That(controllerType.GetField("Cam"), Is.Null, "Legacy gameplay state must no longer own the output camera reference.");
            Assert.That(controllerType.GetField("Cue_Camera"), Is.Null, "Legacy gameplay state must no longer own the legacy cue camera reference.");
            Assert.That(controllerType.GetField("CameraOffset"), Is.Null);
            Assert.That(controllerType.GetField("CameraDistance"), Is.Null);
            Assert.That(controllerType.GetMethod("lockCamera"), Is.Null);
            Assert.That(controllerType.GetField("shotCameraController").GetValue(playerController), Is.SameAs(shotCameraController));
            Assert.That(controllerType.GetField("spectateCameraController").GetValue(playerController), Is.SameAs(spectateCameraController));

            var switchState = controllerType.GetMethod("SwitchState");
            var shootState = controllerType.GetField("shootState").GetValue(playerController);
            switchState.Invoke(playerController, new[] { shootState });
            Assert.That(aimingController.enabled, Is.False, "Aiming must be disabled while the cue is in shoot state.");
            Assert.That(spinController.enabled, Is.False, "Spin selection must stop while shot power is active.");
            Assert.That(shotPowerController.enabled, Is.True, "Shot power must be enabled while the cue is in shoot state.");
            Assert.That(shotCameraController.enabled, Is.True, "Shot presentation must be enabled while shot power is active.");
            Assert.That(spectateCameraController.enabled, Is.False, "Spectate presentation must stay disabled before the strike.");
            Assert.That(aimingCameraController.ApplyCameraPose(0f, true), Is.False);
            AssertShotCameraMatchesDirection(shotCameraController, shotPowerController);
            Assert.That(spinController.Spin.Side, Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(spinController.Spin.Vertical, Is.EqualTo(-0.1f).Within(0.00001f));

            shotPowerController.CueBall.linearVelocity = Vector3.right;
            var spectateState = controllerType.GetField("spectateState").GetValue(playerController);
            switchState.Invoke(playerController, new[] { spectateState });
            Assert.That(aimingController.enabled, Is.False, "Aiming must remain disabled while spectating.");
            Assert.That(spinController.enabled, Is.False, "Spin selection must remain disabled while spectating.");
            Assert.That(shotPowerController.enabled, Is.False, "Shot power must be disabled while spectating.");
            Assert.That(shotCameraController.enabled, Is.False, "Shot presentation must stop after the strike phase.");
            Assert.That(spectateCameraController.enabled, Is.True, "Spectate presentation must own the output camera after the strike.");
            Assert.That(
                aimingCameraController.ApplyCameraPose(0f, true),
                Is.False,
                "Aiming presentation must stop driving the output camera while gameplay aiming is disabled.");
            Assert.That(
                shotCameraController.ApplyCameraPose(0f, true),
                Is.False,
                "Shot presentation must stop driving the output camera after shot power is disabled.");
            Assert.That(spectateCameraController.ApplyCameraPose(0f, true), Is.True);

            playerController.transform.rotation = Quaternion.Euler(0f, 123f, 0f);
            shotPowerController.CueBall.linearVelocity = Vector3.zero;
            spectateCameraController.enabled = false;
            aimingController.enabled = true;

            Assert.That(aimingController.Direction.X, Is.EqualTo(direction.X).Within(0.00001f));
            Assert.That(aimingController.Direction.Y, Is.EqualTo(direction.Y).Within(0.00001f));

            directionToCueBall = (aimingController.CueBall.transform.position - playerController.transform.position).normalized;
            Assert.That(
                Vector3.Dot(playerController.transform.forward, directionToCueBall),
                Is.EqualTo(1f).Within(0.0001f),
                "Re-enabling aiming must restore a cue pose that intersects the cue-ball center.");

            planarForward = Vector3.ProjectOnPlane(playerController.transform.forward, Vector3.up).normalized;
            Assert.That(planarForward.x, Is.EqualTo(direction.X).Within(0.0001f));
            Assert.That(planarForward.z, Is.EqualTo(direction.Y).Within(0.0001f));

            spinController.ResetToCenter();
        }

        [UnityTest]
        public IEnumerator PoolTableScene_SelectedCueBallSpinReachesCommittedStrike()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");
            Assert.That(playerController, Is.Not.Null);

            var spinController = playerController.GetComponent<CueBallSpinController>();
            var shotPowerController = playerController.GetComponent<ShotPowerController>();
            Assert.That(spinController, Is.Not.Null);
            Assert.That(shotPowerController, Is.Not.Null);

            spinController.ProcessInput(new LocalPlayerInputSnapshot(new Vector2(50f, 0f), false, true));
            Assert.That(spinController.Spin.Side, Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(spinController.Spin.Vertical, Is.Zero.Within(0.00001f));

            var controllerType = playerController.GetType();
            var switchState = controllerType.GetMethod("SwitchState");
            var shootState = controllerType.GetField("shootState").GetValue(playerController);
            switchState.Invoke(playerController, new[] { shootState });

            Assert.That(spinController.enabled, Is.False);
            Assert.That(shotPowerController.enabled, Is.True);
            Assert.That(spinController.Spin.Side, Is.EqualTo(0.5f).Within(0.00001f));

            var cueBall = shotPowerController.CueBall;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;

            yield return null;

            for (var stroke = 0; stroke < 18; stroke++)
            {
                shotPowerController.ProcessInput(
                    new LocalPlayerInputSnapshot(new Vector2(0f, 10f), true));
            }

            shotPowerController.ProcessInput(new LocalPlayerInputSnapshot(Vector2.zero, false));

            Assert.That(shotPowerController.ShotCommitted, Is.False);
            Assert.That(
                spinController.Spin.Side,
                Is.EqualTo(0.5f).Within(0.00001f),
                "Queueing the shot must preserve the selected contact point until physics commits it.");
            Assert.That(Mathf.Abs(cueBall.angularVelocity.y), Is.LessThan(0.0001f));

            yield return new WaitForFixedUpdate();

            Assert.That(shotPowerController.ShotCommitted, Is.True);
            Assert.That(spinController.Spin.IsCentered, Is.True, "A committed strike must reset spin for the next shot.");
            Assert.That(
                cueBall.angularVelocity.y,
                Is.GreaterThan(0.1f),
                "Positive side contact must produce positive vertical-axis side spin.");
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

            var controllerType = playerController.GetType();
            Assert.That(
                (float)controllerType.GetField("distance").GetValue(playerController),
                Is.EqualTo(1.6666667f).Within(0.0001f),
                "The legacy cue controller distance must use the metric table scale.");

            var shotPowerController = playerController.GetComponent<ShotPowerController>();
            Assert.That(shotPowerController, Is.Not.Null);
            Assert.That(
                shotPowerController.MaximumCuePullbackMeters,
                Is.EqualTo(0.35f).Within(0.0001f),
                "The modern cue controller must clamp visual pullback to an explicit metric range.");
            Assert.That(
                shotPowerController.PointerDeltaSensitivity,
                Is.EqualTo(0.1f).Within(0.0001f),
                "Modern shot power must preserve the legacy pointer-delta sensitivity while reading raw Input System input.");
            Assert.That(
                shotPowerController.CueStrokeMetersPerPointerUnit,
                Is.EqualTo(0.02f).Within(0.0001f),
                "The cue stroke must use the metric controller scale.");
            Assert.That(
                shotPowerController.CueStrokeMetersPerPointerUnit,
                Is.LessThan(BilliardsPhysicalSpecification.BallRadiusMeters),
                "A single normalized cue stroke input must move less than one regulation ball radius.");
            Assert.That(
                shotPowerController.MaximumShotSpeedMetersPerSecond,
                Is.EqualTo(6.6666667f).Within(0.0001f),
                "The modern cue controller must express shot strength as a metric target speed.");
            Assert.That(
                playerController.transform.lossyScale.x,
                Is.EqualTo(0.01f).Within(0.0001f),
                "The active cue model must be scaled to approximately 1.5 meters.");

            var cueBall = GetPublicGameObjectField(playerController, "WhiteBall");
            var outputCameraObject = Camera.main.gameObject;

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

            var stoppedSpeedThreshold = (float)cueBallStateManager.GetType()
                .GetField("StoppedSpeedThresholdMetersPerSecond")
                .GetRawConstantValue();
            Assert.That(
                stoppedSpeedThreshold,
                Is.EqualTo(BilliardsPhysicalSpecification.BallStoppedSpeedMetersPerSecond).Within(0.000001f),
                "The legacy ball-state threshold must stay aligned with the metric physics specification.");

            var cueBallMovingMethod = cueBallStateManager.GetType().GetMethod("isBallMoving");
            var cueBallRigidbody = cueBall.GetComponent<Rigidbody>();
            cueBallRigidbody.linearVelocity = Vector3.right * 0.009f;
            Assert.That((bool)cueBallMovingMethod.Invoke(cueBallStateManager, null), Is.False);
            cueBallRigidbody.linearVelocity = Vector3.right * 0.011f;
            Assert.That((bool)cueBallMovingMethod.Invoke(cueBallStateManager, null), Is.True);
            cueBallRigidbody.linearVelocity = Vector3.zero;

            var initialCueBallPosition = cueBall.transform.position;
            cueBall.transform.position = new Vector3(10f, 10f, 10f);
            cueBallRigidbody.linearVelocity = Vector3.one;
            cueBallRigidbody.angularVelocity = Vector3.one;
            var ballManagerType = cueBallStateManager.GetType();
            var currentBallStateField = ballManagerType.GetField(
                "currentState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var pocketedState = ballManagerType.GetField("pocketedState").GetValue(cueBallStateManager);
            currentBallStateField.SetValue(cueBallStateManager, pocketedState);
            var legacyBallManager = FindActiveMonoBehaviourByTypeName(activeScene, "BallStateManager");
            legacyBallManager.GetType().GetMethod("addPocketedBall").Invoke(
                legacyBallManager,
                new object[] { cueBall, 1 });
            legacyBallManager.GetType().GetMethod("resetWhiteBallFromPocket").Invoke(
                legacyBallManager,
                null);
            Assert.That(
                Vector3.Distance(cueBall.transform.position, initialCueBallPosition),
                Is.LessThan(0.0001f),
                "The cue ball must return to its metric initial position after a scratch.");
            Assert.That(cueBallRigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(cueBallRigidbody.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(
                currentBallStateField.GetValue(cueBallStateManager).GetType().Name,
                Is.EqualTo("BallIdleState"),
                "The cue ball must leave BallPocketedState after scratch recovery.");

            Assert.That(outputCameraObject, Is.Not.Null);
            Assert.That(outputCameraObject.activeInHierarchy, Is.True, "The output camera must be active in the scene hierarchy.");

            var outputCamera = outputCameraObject.GetComponent<Camera>();
            var cinemachineBrain = FindMonoBehaviourByTypeName(outputCameraObject, "CinemachineBrain");
            var aimingCameraController = outputCameraObject.GetComponent<AimingCameraController>();
            var shotCameraController = outputCameraObject.GetComponent<ShotCameraController>();
            var spectateCameraController = outputCameraObject.GetComponent<SpectateCameraController>();
            var modernAimingController = playerController.GetComponent<CueAimingController>();

            Assert.That(outputCamera, Is.Not.Null);
            Assert.That(outputCamera.enabled, Is.True, "The output Camera component must be enabled.");
            Assert.That(cinemachineBrain, Is.Not.Null, "The output camera must keep its CinemachineBrain.");
            Assert.That(cinemachineBrain.enabled, Is.True, "The output camera CinemachineBrain must be enabled.");
            Assert.That(aimingCameraController, Is.Not.Null, "The output camera must own modern aiming presentation.");
            Assert.That(shotCameraController, Is.Not.Null, "The output camera must own modern shot presentation.");
            Assert.That(spectateCameraController, Is.Not.Null, "The output camera must own modern spectate presentation.");
            Assert.That(modernAimingController, Is.Not.Null);
            Assert.That(aimingCameraController.AimingController, Is.SameAs(modernAimingController));
            Assert.That(aimingCameraController.DistanceBehindCueBall, Is.EqualTo(2.4f).Within(0.0001f));
            Assert.That(aimingCameraController.HeightAboveCueBall, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(aimingCameraController.LookAheadDistance, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(aimingCameraController.TargetHeightOffset, Is.EqualTo(0.08f).Within(0.0001f));
            AssertAimingCameraMatchesDirection(aimingCameraController, modernAimingController);
            Assert.That(shotCameraController.ShotPowerController, Is.SameAs(shotPowerController));
            Assert.That(shotCameraController.enabled, Is.False);
            Assert.That(spectateCameraController.BallsRoot, Is.EqualTo(GameObject.Find("Balls").transform));
            Assert.That(spectateCameraController.CueBall, Is.SameAs(cueBallRigidbody));
            Assert.That(spectateCameraController.MinimumMovingSpeedMetersPerSecond, Is.EqualTo(0.01f).Within(0.0001f));
            Assert.That(spectateCameraController.enabled, Is.False);
            Assert.That(controllerType.GetField("shotCameraController").GetValue(playerController), Is.SameAs(shotCameraController));
            Assert.That(controllerType.GetField("spectateCameraController").GetValue(playerController), Is.SameAs(spectateCameraController));
            Assert.That(controllerType.GetField("Cam"), Is.Null);
            Assert.That(controllerType.GetField("Cue_Camera"), Is.Null);
            Assert.That(controllerType.GetField("CameraOffset"), Is.Null);
            Assert.That(controllerType.GetField("CameraDistance"), Is.Null);
            Assert.That(controllerType.GetMethod("lockCamera"), Is.Null);

            var cueCamera = GameObject.Find("Camera_Cue");
            Assert.That(cueCamera, Is.Not.Null);
            Assert.That(cueCamera.name, Is.EqualTo("Camera_Cue"));
            Assert.That(cueCamera.activeInHierarchy, Is.True, "The inert legacy Camera_Cue object remains only as scene cleanup debt.");

            var cueVirtualCamera = FindMonoBehaviourByTypeName(cueCamera, "CinemachineFreeLook");
            Assert.That(cueVirtualCamera, Is.Not.Null, "Camera_Cue must keep its CinemachineFreeLook component.");
            Assert.That(cueVirtualCamera.enabled, Is.False, "The legacy input-driven FreeLook must stay disabled.");
            Assert.That(
                GetPublicTransformProperty(cueVirtualCamera, "LookAt"),
                Is.EqualTo(cueBall.transform),
                "The inert legacy FreeLook must not regain input-driven camera authority.");
            Assert.That(
                GetPublicPropertyValue(cinemachineBrain, "ActiveVirtualCamera"),
                Is.Null,
                "The disabled legacy FreeLook must no longer drive the output camera during aiming.");

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

            Assert.That(
                FindActiveMonoBehaviourByTypeName(activeScene, "Pocket"),
                Is.Null,
                "The gameplay scene must not retain the legacy global Pocket capture behavior.");

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

        [UnityTest]
        public IEnumerator PoolTableScene_ShotInstrumentationRecordsAllBallsAndBallCollision()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var compositionRoot = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>();
            Assert.That(compositionRoot, Is.Not.Null);

            var instrumentation = compositionRoot.ShotSimulationInstrumentation;
            Assert.That(instrumentation, Is.Not.Null, "The composition root must expose shot simulation instrumentation.");
            Assert.That(instrumentation.isActiveAndEnabled, Is.True);
            Assert.That(instrumentation.RegisteredBallCount, Is.EqualTo(16));

            var probes = compositionRoot.BallsRoot.GetComponentsInChildren<RigidbodySimulationProbe>(true);
            Assert.That(probes, Has.Length.EqualTo(16), "Every billiard ball must expose one simulation probe.");

            var identities = EnumerateSceneObjects(activeScene)
                .Select(gameObject => gameObject.GetComponent<BallIdentity>())
                .Where(identity => identity != null)
                .ToArray();
            var cueBall = identities.Single(identity => identity.Id.Number == 0);
            var oneBall = identities.Single(identity => identity.Id.Number == 1);
            var cueBody = cueBall.GetComponent<Rigidbody>();
            var oneBody = oneBall.GetComponent<Rigidbody>();

            cueBody.useGravity = false;
            oneBody.useGravity = false;
            cueBody.linearVelocity = Vector3.zero;
            oneBody.linearVelocity = Vector3.zero;
            cueBody.angularVelocity = Vector3.zero;
            oneBody.angularVelocity = Vector3.zero;
            cueBody.position = new Vector3(-0.05f, 2f, 0f);
            oneBody.position = new Vector3(0.05f, 2f, 0f);
            cueBody.linearVelocity = Vector3.right;
            oneBody.linearVelocity = Vector3.left;
            UnityEngine.Physics.SyncTransforms();

            instrumentation.BeginShot();
            for (var step = 0; step < 16; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            var report = instrumentation.CompleteShot();

            Assert.That(report, Is.SameAs(instrumentation.LastReport));
            Assert.That(report.Tracks, Has.Count.EqualTo(16));
            Assert.That(report.DurationSeconds, Is.GreaterThanOrEqualTo(BilliardsSimulationConfiguration.FixedTimestepSeconds));
            Assert.That(report.TryGetTrack(cueBall.Id, out var cueTrack), Is.True);
            Assert.That(report.TryGetTrack(oneBall.Id, out var oneTrack), Is.True);
            Assert.That(cueTrack.Samples, Has.Count.GreaterThan(2));
            Assert.That(oneTrack.Samples, Has.Count.GreaterThan(2));
            Assert.That(cueTrack.DistanceTraveledMeters, Is.GreaterThan(0f));
            Assert.That(oneTrack.DistanceTraveledMeters, Is.GreaterThan(0f));
            Assert.That(
                report.Collisions.Any(collision =>
                    collision.Kind == SimulationCollisionKind.Ball
                    && collision.Ball == cueBall.Id
                    && collision.OtherBall == oneBall.Id),
                Is.True,
                "The controlled cue-ball/object-ball impact must be captured once using typed ball IDs.");
        }

        [UnityTest]
        public IEnumerator PoolTableScene_ShotInstrumentationRecordsPersistentRackImpulseTransfer()
        {
            yield return LoadPoolTableScene();

            var compositionRoot = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>();
            Assert.That(compositionRoot, Is.Not.Null);
            var instrumentation = compositionRoot.ShotSimulationInstrumentation;
            Assert.That(instrumentation, Is.Not.Null);

            var identities = compositionRoot.BallsRoot.GetComponentsInChildren<BallIdentity>(true);
            var cueBall = identities.Single(identity => identity.Id.IsCueBall);
            var objectBalls = identities.Where(identity => !identity.Id.IsCueBall).ToArray();
            var apexBall = objectBalls.OrderBy(identity => identity.transform.position.x).First();

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var cueBody = cueBall.GetComponent<Rigidbody>();
            cueBody.useGravity = false;
            cueBody.linearVelocity = Vector3.zero;
            cueBody.angularVelocity = Vector3.zero;
            cueBody.position = apexBall.GetComponent<Rigidbody>().position
                - (Vector3.right * BilliardsPhysicalSpecification.BallDiameterMeters * 1.5f);
            cueBody.linearVelocity = Vector3.right * 2f;
            UnityEngine.Physics.SyncTransforms();

            instrumentation.BeginShot();
            for (var step = 0; step < 40; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            var report = instrumentation.CompleteShot();
            Assert.That(
                report.Collisions.Any(collision =>
                    collision.Kind == SimulationCollisionKind.Ball
                    && collision.Ball != cueBall.Id
                    && collision.OtherBall.HasValue
                    && collision.OtherBall.Value != cueBall.Id
                    && collision.ImpulseNewtonSeconds > 0f),
                Is.True,
                "A break must record impulse transfer through object-ball contacts that already existed in the rack.");
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

        private sealed class TestCursorStateAccessor : ICursorStateAccessor
        {
            public TestCursorStateAccessor(CursorLockMode lockState, bool visible)
            {
                LockState = lockState;
                Visible = visible;
            }

            public CursorLockMode LockState { get; set; }

            public bool Visible { get; set; }
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

        private static void AssertRailSurface(MeshCollider collider, string subject)
        {
            Assert.That(collider, Is.Not.Null, $"{subject} must keep active solid collision geometry.");
            Assert.That(
                collider.GetComponent<RailSurface>(),
                Is.Not.Null,
                $"{subject} must explicitly identify itself as a rail surface.");
            Assert.That(collider.sharedMaterial, Is.Not.Null, $"{subject} must use the dedicated rail PhysicMaterial.");
            Assert.That(
                collider.sharedMaterial.dynamicFriction,
                Is.EqualTo(0f),
                $"{subject} must not add native PhysX friction on top of the explicit rail model.");
            Assert.That(
                collider.sharedMaterial.staticFriction,
                Is.EqualTo(0f),
                $"{subject} must not add native PhysX friction on top of the explicit rail model.");
            Assert.That(
                collider.sharedMaterial.bounciness,
                Is.EqualTo(0f),
                $"{subject} must not add native PhysX bounce on top of the explicit rail model.");
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

        private static void AssertAimingCameraMatchesDirection(
            AimingCameraController cameraController,
            CueAimingController aimingController)
        {
            Assert.That(cameraController.ApplyCameraPose(0f, true), Is.True);

            var strikeDirection = aimingController.StrikeDirection.normalized;
            var planarDirection = Vector3.ProjectOnPlane(strikeDirection, Vector3.up).normalized;
            var cueBallPosition = aimingController.CueBall.transform.position;
            var elevationFollowHeight = Mathf.Max(0f, -strikeDirection.y)
                * aimingController.CueDistance
                * cameraController.ElevationFollowFactor;
            var expectedPosition = cueBallPosition
                - (planarDirection * cameraController.DistanceBehindCueBall)
                + (Vector3.up * (cameraController.HeightAboveCueBall + elevationFollowHeight));
            var expectedFocusPoint = cueBallPosition
                + (planarDirection * cameraController.LookAheadDistance)
                + (Vector3.up * cameraController.TargetHeightOffset);

            Assert.That(
                Vector3.Distance(cameraController.transform.position, expectedPosition),
                Is.LessThan(0.0001f),
                "Aiming camera position must be derived from the canonical gameplay aim direction.");
            Assert.That(
                Vector3.Dot(
                    cameraController.transform.forward,
                    (expectedFocusPoint - expectedPosition).normalized),
                Is.GreaterThan(0.999f),
                "Aiming camera orientation must follow the canonical gameplay aim direction.");
        }

        private static void AssertShotCameraMatchesDirection(
            ShotCameraController cameraController,
            ShotPowerController shotPowerController)
        {
            Assert.That(cameraController.ApplyCameraPose(0f, true), Is.True);

            var strikeDirection = shotPowerController.AimingController.StrikeDirection.normalized;
            var planarDirection = Vector3.ProjectOnPlane(strikeDirection, Vector3.up).normalized;
            var cueBallPosition = shotPowerController.CueBall.position;
            var expectedDistance = cameraController.DistanceBehindCueBall
                + (shotPowerController.NormalizedPower * cameraController.AdditionalDistanceAtFullPower);
            var elevationFollowHeight = Mathf.Max(0f, -strikeDirection.y)
                * shotPowerController.AimingController.CueDistance
                * cameraController.ElevationFollowFactor;
            var expectedPosition = cueBallPosition
                - (planarDirection * expectedDistance)
                + (Vector3.up * (cameraController.HeightAboveCueBall + elevationFollowHeight));
            var expectedFocusPoint = cueBallPosition
                + (planarDirection * cameraController.LookAheadDistance)
                + (Vector3.up * cameraController.TargetHeightOffset);

            Assert.That(
                Vector3.Distance(cameraController.transform.position, expectedPosition),
                Is.LessThan(0.0001f),
                "Shot camera position must remain derived from the canonical gameplay aim direction.");
            Assert.That(
                Vector3.Dot(
                    cameraController.transform.forward,
                    (expectedFocusPoint - expectedPosition).normalized),
                Is.GreaterThan(0.999f),
                "Shot camera orientation must remain aligned with the canonical gameplay aim direction.");
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
