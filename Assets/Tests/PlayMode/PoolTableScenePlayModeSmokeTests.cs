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
using PoolTable.Physics.Cloth;
using PoolTable.Physics.Configuration;
using PoolTable.Physics.Rails;
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
                    "Multiple rail colliders in one physics step must share one custom restitution response.");
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
        public IEnumerator PoolTableScene_LegacyShotSpeedIsIndependentOfBallMass()
        {
            yield return LoadPoolTableScene();

            var activeScene = SceneManager.GetActiveScene();
            var playerController = FindActiveMonoBehaviourByTypeName(activeScene, "PlayersStateManagement");
            Assert.That(playerController, Is.Not.Null);

            var controllerType = playerController.GetType();
            var maxShotSpeedMetersPerSecond =
                (float)controllerType.GetField("maxShotSpeedMetersPerSecond").GetValue(playerController);
            var shootState = controllerType.GetField("shootState").GetValue(playerController);
            var applyShotVelocityChange = shootState.GetType().GetMethod(
                "ApplyShotVelocityChange",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(applyShotVelocityChange, Is.Not.Null);

            var regulationProbe = new GameObject("RegulationMassShotProbe");
            var legacyMassProbe = new GameObject("LegacyMassShotProbe");
            var regulationRigidbody = regulationProbe.AddComponent<Rigidbody>();
            var legacyRigidbody = legacyMassProbe.AddComponent<Rigidbody>();

            regulationRigidbody.useGravity = false;
            legacyRigidbody.useGravity = false;
            regulationRigidbody.linearDamping = 0f;
            legacyRigidbody.linearDamping = 0f;
            regulationRigidbody.mass = BilliardsSimulationConfiguration.BallMassKilograms;
            legacyRigidbody.mass = 1f;

            applyShotVelocityChange.Invoke(
                null,
                new object[] { regulationRigidbody, Vector3.right, 1f, maxShotSpeedMetersPerSecond });
            applyShotVelocityChange.Invoke(
                null,
                new object[] { legacyRigidbody, Vector3.right, 1f, maxShotSpeedMetersPerSecond });

            yield return new WaitForFixedUpdate();

            Assert.That(
                regulationRigidbody.linearVelocity.x,
                Is.EqualTo(maxShotSpeedMetersPerSecond).Within(0.0001f));
            Assert.That(
                legacyRigidbody.linearVelocity.x,
                Is.EqualTo(maxShotSpeedMetersPerSecond).Within(0.0001f));
            Assert.That(
                regulationRigidbody.linearVelocity,
                Is.EqualTo(legacyRigidbody.linearVelocity),
                "Cue-ball shot speed must not change when Rigidbody mass changes.");

            Object.Destroy(regulationProbe);
            Object.Destroy(legacyMassProbe);
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
            var cueStrokeDistancePerInput =
                (float)controllerType.GetField("cueStrokeDistancePerInput").GetValue(playerController);
            Assert.That(
                cueStrokeDistancePerInput,
                Is.EqualTo(0.02f).Within(0.0001f),
                "The cue stroke must use the metric controller scale.");
            Assert.That(
                cueStrokeDistancePerInput,
                Is.LessThan(BilliardsPhysicalSpecification.BallRadiusMeters),
                "A single normalized cue stroke input must move less than one regulation ball radius.");
            Assert.That(
                (float)controllerType.GetField("maxShotSpeedMetersPerSecond").GetValue(playerController),
                Is.EqualTo(6.6666667f).Within(0.0001f),
                "The legacy cue controller must express shot strength as a metric target speed.");
            Assert.That(
                (Vector3)controllerType.GetField("CameraOffset").GetValue(playerController),
                Is.EqualTo(new Vector3(0f, 0.06666667f, 0f)),
                "The cue camera offset must be scaled with the metric cue setup.");
            Assert.That(
                playerController.transform.lossyScale.x,
                Is.EqualTo(0.01f).Within(0.0001f),
                "The active cue model must be scaled to approximately 1.5 meters.");

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
