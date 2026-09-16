#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Aiming;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Pockets;
using PoolTable.Gameplay.Shots;
using PoolTable.Presentation.Camera;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoolTable.Presentation.Diagnostics
{
    internal sealed class WindowsPlayerSmokeRunner : MonoBehaviour
    {
        private const string SmokeArgument = "-pooltable-player-smoke";
        private const string ReportPathEnvironmentVariable = "POOLTABLE_PLAYER_SMOKE_REPORT";
        private const string InputProbeTypeName = "PoolTable.Input.WindowsPlayerInputSmokeProbe";
        private const float MinimumShotSpeedMetersPerSecond = 0.01f;
        private const float InputObservationSeconds = 0.05f;
        private static readonly object RuntimeLogGate = new object();
        private static readonly List<string> RuntimeErrors = new List<string>();
        private static string runtimeErrorJournalPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartWhenRequested()
        {
            if (!Environment.GetCommandLineArgs().Any(argument =>
                    string.Equals(argument, SmokeArgument, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            lock (RuntimeLogGate)
            {
                RuntimeErrors.Clear();
                runtimeErrorJournalPath = ResolveReportPath() + ".runtime-errors.log";
                var runtimeErrorDirectory = Path.GetDirectoryName(runtimeErrorJournalPath);
                if (string.IsNullOrWhiteSpace(runtimeErrorDirectory))
                {
                    throw new InvalidOperationException(
                        $"Unable to resolve the runtime-error journal directory for {runtimeErrorJournalPath}.");
                }

                Directory.CreateDirectory(runtimeErrorDirectory);
                File.WriteAllText(runtimeErrorJournalPath, string.Empty);
            }

            Application.logMessageReceivedThreaded += CaptureRuntimeError;

            var host = new GameObject(nameof(WindowsPlayerSmokeRunner));
            DontDestroyOnLoad(host);
            host.AddComponent<WindowsPlayerSmokeRunner>();
        }

        private IEnumerator Start()
        {
            var report = new WindowsPlayerSmokeReport();
            var reportPath = ResolveReportPath();
            Exception failure = null;

            yield return null;
            yield return null;

            try
            {
                ThrowIfRuntimeErrors();
                ValidateStartup(report);
            }
            catch (Exception exception)
            {
                failure = UnwrapReflectionException(exception);
            }

            if (failure == null)
            {
                yield return RunValidation(ValidateLocalControls(report), exception => failure = exception);
            }

            if (failure == null)
            {
                yield return RunValidation(ValidateShotFlow(report), exception => failure = exception);
            }

            if (failure == null)
            {
                yield return RunValidation(ValidateBallInHand(report), exception => failure = exception);
            }

            if (failure == null)
            {
                try
                {
                    ThrowIfRuntimeErrors();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }

            RemoveSmokeGamepad();

            report.Success = failure == null;
            if (failure != null)
            {
                report.Error = failure.ToString();
                Debug.LogException(failure);
            }

            report.CleanShutdownRequested = true;
            WriteReport(reportPath, report);
            Debug.Log($"Windows player smoke report: {reportPath}");

            yield return null;
            Application.Quit(report.Success ? 0 : 1);
        }

        private static IEnumerator RunValidation(IEnumerator validation, Action<Exception> onFailure)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!validation.MoveNext())
                    {
                        yield break;
                    }

                    current = validation.Current;
                }
                catch (Exception exception)
                {
                    onFailure(UnwrapReflectionException(exception));
                    yield break;
                }

                yield return current;
            }
        }

        private static void ValidateStartup(WindowsPlayerSmokeReport report)
        {
            var activeScene = SceneManager.GetActiveScene();
            Require(activeScene.IsValid() && activeScene.isLoaded, "The active scene must be loaded.");
            Require(activeScene.name == "PoolTable", $"Expected PoolTable scene, found {activeScene.name}.");

            var identities = FindObjectsByType<BallIdentity>(FindObjectsInactive.Include);
            var activeIdentities = identities
                .Where(identity => identity != null && identity.gameObject.activeInHierarchy)
                .ToArray();
            Require(identities.Length == 16, $"Expected 16 billiard ball identities, found {identities.Length}.");
            Require(activeIdentities.Length == 16, $"Expected 16 active billiard balls, found {activeIdentities.Length}.");
            Require(activeIdentities.Count(identity => identity.IsCueBall) == 1, "Exactly one active cue ball is required.");

            var gameManager = FindActiveLegacyComponent("GameManager");
            var currentTurn = InvokeLegacyMethod(gameManager, "getCurrentPlayerTurn");
            Require(currentTurn?.ToString() == "PlayerOneTurn", "Player One must own the initial turn.");

            report.SceneName = activeScene.name;
            report.BallCount = activeIdentities.Length;
            report.Startup = true;
            report.Checkpoints.Add("startup");
        }

        private static IEnumerator ValidateLocalControls(WindowsPlayerSmokeReport report)
        {
            var player = FindActiveLegacyComponent("PlayersStateManagement");
            var aiming = RequireLegacyBehaviour<CueAimingController>(player, "aimingController");
            var spin = RequireLegacyBehaviour<CueBallSpinController>(player, "spinController");
            var shotPower = RequireLegacyBehaviour<ShotPowerController>(player, "shotPowerController");
            var placement = RequireLegacyBehaviour(player, "ballInHandPlacementController");

            Require(aiming.enabled, "Aiming must be enabled while the player is in the play state.");
            Require(spin.enabled, "Spin control must be enabled while the player is in the play state.");
            Require(!shotPower.enabled, "Shot power must be disabled before a shot starts.");
            Require(!placement.enabled, "Ball-in-hand placement must be inactive at startup.");
            Require(HasInitializedInputReader(aiming), "The aiming controller must initialize its local input reader.");
            Require(HasInitializedInputReader(spin), "The spin controller must initialize its local input reader.");
            Require(HasInitializedInputReader(shotPower), "The shot-power controller must initialize its local input reader.");

            var outputCamera = UnityEngine.Camera.main;
            Require(outputCamera != null, "The player camera must exist during local-control validation.");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                outputCamera.aspect = 16f / 10f;
                outputCamera.ResetProjectionMatrix();
            }

            var aimingCamera = outputCamera.GetComponent<AimingCameraController>();
            Require(aimingCamera != null, "The output camera must provide AimingCameraController during local-control smoke validation.");
            Require(aimingCamera.ApplyRuntimeCameraPose(0f, true), "The aiming camera must resolve a valid runtime pose before visibility validation.");
            RequireCueReadable(outputCamera, aiming);

            Require(aiming.AimStage == CueAimStage.Yaw, "Mouse aiming must start with yaw.");
            aiming.AdvanceAimStage();
            Require(aiming.AimStage == CueAimStage.Elevation, "The first mouse confirmation must advance from yaw to elevation.");
            aiming.AdvanceAimStage();
            Require(aiming.AimStage == CueAimStage.Locked, "The second mouse confirmation must lock aiming before shot power.");
            aiming.enabled = false;
            aiming.enabled = true;
            Require(aiming.AimStage == CueAimStage.Yaw, "Re-entering aiming must reset the mouse flow to yaw first.");

            var directionBefore = new Vector2(aiming.Direction.X, aiming.Direction.Y);
            SetSmokeGamepadState(0.75f, 0f, 0f, 0f, 0f, 0f);
            yield return new WaitForSecondsRealtime(InputObservationSeconds);
            ThrowIfRuntimeErrors();

            var directionAfter = new Vector2(aiming.Direction.X, aiming.Direction.Y);
            Require(
                (directionAfter - directionBefore).sqrMagnitude > 0.000001f,
                "The aiming controller must consume the injected Input System aim axis during Update.");

            Require(aimingCamera.ApplyRuntimeCameraPose(0f, true), "The aiming camera must resolve a valid runtime pose before elevation validation.");
            var elevationBefore = aiming.ElevationDegrees;
            var cameraPositionBeforeElevation = aimingCamera.transform.position;
            var cameraForwardBeforeElevation = aimingCamera.transform.forward;

            SetSmokeGamepadState(0f, 0.75f, 0f, 0f, 0f, 0f);
            yield return new WaitForSecondsRealtime(InputObservationSeconds);
            ThrowIfRuntimeErrors();
            Require(aiming.ElevationDegrees > elevationBefore, "The aiming controller must consume vertical aim input as cue elevation.");
            Require(aimingCamera.ApplyRuntimeCameraPose(0f, true), "The aiming camera must resolve a valid elevated runtime pose.");
            Require(
                aimingCamera.transform.position.y > cameraPositionBeforeElevation.y + 0.001f,
                "The aiming camera must rise with cue elevation instead of remaining on a planar yaw orbit.");
            Require(
                Vector3.Angle(cameraForwardBeforeElevation, aimingCamera.transform.forward) > 0.05f,
                "The aiming camera orientation must follow cue elevation.");
            RequireCueReadable(outputCamera, aiming);

            Require(spin.Spin.IsCentered, "Cue-ball spin must start centered.");
            SetSmokeGamepadState(0f, 0f, 0.5f, 0.5f, 1f, 0f);
            yield return null;
            ThrowIfRuntimeErrors();
            Require(!spin.Spin.IsCentered, "The spin controller must consume the injected Input System action axis during Update.");

            spin.ResetToCenter();
            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 0f);
            yield return null;
            ThrowIfRuntimeErrors();

            report.InputSystem = true;
            report.LocalControls = true;
            report.Checkpoints.Add("local-controls");
        }

        private static void RequireCueReadable(UnityEngine.Camera camera, CueAimingController aiming)
        {
            var projection = camera.projectionMatrix;
            var horizontalSlopeLimit = 1f / Mathf.Max(0.0001f, Mathf.Abs(projection.m00));
            var verticalSlopeLimit = 1f / Mathf.Max(0.0001f, Mathf.Abs(projection.m11));
            var cueBallPosition = aiming.CueBall.transform.position;
            var cueButtPosition = aiming.transform.position;
            var visibleFractions = new[] { 0.2f, 0.35f, 0.5f, 0.55f };

            foreach (var fraction in visibleFractions)
            {
                var cueSample = Vector3.Lerp(cueBallPosition, cueButtPosition, fraction);
                var cameraSpacePoint = camera.transform.InverseTransformPoint(cueSample);
                Require(
                    cameraSpacePoint.z > camera.nearClipPlane,
                    $"Cue sample {fraction:P0} must remain in front of the player camera near plane.");

                var horizontalSlope = Mathf.Abs(cameraSpacePoint.x / cameraSpacePoint.z);
                var verticalSlope = Mathf.Abs(cameraSpacePoint.y / cameraSpacePoint.z);

                Require(
                    horizontalSlope <= horizontalSlopeLimit * 0.94f,
                    $"Cue sample {fraction:P0} must remain horizontally inside the player-camera frustum.");
                Require(
                    verticalSlope <= verticalSlopeLimit * 0.94f,
                    $"Cue sample {fraction:P0} must remain vertically inside the player-camera frustum.");
            }
        }

        private static IEnumerator ValidateShotFlow(WindowsPlayerSmokeReport report)
        {
            var player = FindActiveLegacyComponent("PlayersStateManagement");
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "The player must start in the play state.");

            var shotPower = RequireLegacyBehaviour<ShotPowerController>(player, "shotPowerController");
            var cueBall = shotPower.CueBall;
            Require(cueBall != null, "Shot power must reference the cue-ball Rigidbody.");
            StopAllBalls();

            SetSmokeGamepadState(0f, 0f, 0f, 1f, 0f, 1f);
            yield return new WaitForSecondsRealtime(InputObservationSeconds);

            ThrowIfRuntimeErrors();
            Require(GetLegacyCurrentStateName(player) == "PlayersShootState", "The player must enter the shoot state.");
            Require((bool)InvokeLegacyMethod(player, "IsShotPowerEnabled"), "Shot power must be enabled in the shoot state.");
            Require(shotPower.PullbackMeters > 0f, "Holding primary action with controller pullback must build shot power.");
            report.ShotStateEntered = true;

            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 0f);
            yield return null;

            for (var fixedStep = 0; fixedStep < 5 && !shotPower.ShotCommitted; fixedStep++)
            {
                yield return new WaitForFixedUpdate();
            }

            ThrowIfRuntimeErrors();
            Require(shotPower.ShotCommitted, "Releasing primary action must commit the queued shot during FixedUpdate.");
            Require(!shotPower.enabled, "A committed shot must disable shot-power input.");
            var planarVelocity = Vector3.ProjectOnPlane(cueBall.linearVelocity, Vector3.up).magnitude;
            Require(
                planarVelocity > MinimumShotSpeedMetersPerSecond,
                $"The committed shot must move the cue ball horizontally, measured {planarVelocity:F4} m/s.");

            for (var frame = 0; frame < 10 && GetLegacyCurrentStateName(player) != "PlayersSpectateState"; frame++)
            {
                yield return null;
            }

            Require(GetLegacyCurrentStateName(player) == "PlayersSpectateState", "The player must enter the spectate state after the shot completes.");
            report.SpectateStateEntered = true;

            var spectateVelocity = Vector3.ProjectOnPlane(cueBall.linearVelocity, Vector3.up).magnitude;
            Require(
                spectateVelocity > MinimumShotSpeedMetersPerSecond,
                $"The cue ball must still be moving when spectate begins, measured {spectateVelocity:F4} m/s.");
            yield return null;
            ThrowIfRuntimeErrors();
            var spectateVelocityAfterFrame = Vector3.ProjectOnPlane(cueBall.linearVelocity, Vector3.up).magnitude;
            Require(
                spectateVelocityAfterFrame > MinimumShotSpeedMetersPerSecond,
                $"The cue ball must remain moving during the spectate observation frame, measured {spectateVelocityAfterFrame:F4} m/s.");
            Require(
                GetLegacyCurrentStateName(player) == "PlayersSpectateState",
                "The player must remain in spectate while an active ball is still moving.");

            StopAllBalls();
            for (var frame = 0; frame < 10 && GetLegacyCurrentStateName(player) != "PlayersPlayState"; frame++)
            {
                yield return null;
            }

            ThrowIfRuntimeErrors();
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "The player must return to play after every active ball stops.");

            report.ReturnedToPlay = true;
            report.ShotFlow = true;
            report.Checkpoints.Add("shot-flow");
        }

        private static IEnumerator ValidateBallInHand(WindowsPlayerSmokeReport report)
        {
            var player = FindActiveLegacyComponent("PlayersStateManagement");
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "Ball-in-hand validation must start in the play state.");

            var gameManager = FindActiveLegacyComponent("GameManager");
            var turnBeforeScratch = InvokeLegacyMethod(gameManager, "getCurrentPlayerTurn")?.ToString();
            Require(
                turnBeforeScratch == "PlayerOneTurn" || turnBeforeScratch == "PlayerTwoTurn",
                $"Ball-in-hand validation requires an active player turn, found {turnBeforeScratch ?? "null"}.");

            var controller = FindObjectsByType<BallInHandPlacementController>(
                    FindObjectsInactive.Include)
                .Single();
            var cueBallIdentity = FindObjectsByType<BallIdentity>(FindObjectsInactive.Include)
                .Single(identity => identity.IsCueBall);
            var cueBall = cueBallIdentity.gameObject;
            var capture = cueBall.GetComponent<BallPocketCapture>();
            Require(capture != null, "The cue ball must provide BallPocketCapture for scratch validation.");

            Require(!controller.IsPlacing, "Ball-in-hand placement must be inactive before the smoke scenario starts.");
            Require(capture.TryCapture(new PocketId(1), out _), "The cue ball must be capturable by the real pocket bridge.");
            Require(capture.IsCaptured, "The cue ball must remain marked captured after a scratch.");
            Require(!cueBall.activeSelf, "A captured cue ball must be inactive until ball-in-hand placement begins.");

            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 0f);
            var spectateState = GetRequiredPublicFieldValue(player, "spectateState");
            InvokeLegacyMethod(player, "SwitchState", spectateState);
            StopAllBalls();

            for (var frame = 0; frame < 10 && !controller.IsPlacing; frame++)
            {
                yield return null;
            }

            ThrowIfRuntimeErrors();
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "A captured scratch must return through the play-state resolution path.");
            Require(controller.IsPlacing, "The legacy scratch bridge must start ball-in-hand placement.");
            Require(controller.enabled, "Ball-in-hand placement must be enabled while resolving the scratch.");
            Require(cueBall.activeSelf, "Ball-in-hand placement must reactivate the captured cue ball.");
            Require(capture.IsCaptured, "The cue ball must stay captured until placement is confirmed.");
            Require(controller.IsCurrentPositionLegal, "The initial ball-in-hand candidate must be legal.");
            var expectedTurnAfterScratch = turnBeforeScratch == "PlayerOneTurn" ? "PlayerTwoTurn" : "PlayerOneTurn";
            var turnAfterScratch = InvokeLegacyMethod(gameManager, "getCurrentPlayerTurn")?.ToString();
            Require(
                turnAfterScratch == expectedTurnAfterScratch,
                $"A scratch must transfer the turn to {expectedTurnAfterScratch}, found {turnAfterScratch ?? "null"}.");

            var placementStart = new Vector2(cueBall.transform.position.x, cueBall.transform.position.z);
            SetSmokeGamepadState(0f, 0f, 0.75f, 0f, 0f, 0f);
            yield return new WaitForSecondsRealtime(InputObservationSeconds);
            ThrowIfRuntimeErrors();
            var placementAfterMovement = new Vector2(cueBall.transform.position.x, cueBall.transform.position.z);
            Require(
                (placementAfterMovement - placementStart).sqrMagnitude > 0.000001f,
                "Ball-in-hand must consume the injected controller action axis and move the cue-ball candidate.");
            Require(controller.IsCurrentPositionLegal, "The moved ball-in-hand candidate must remain legal before confirmation.");

            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 0f);
            yield return null;
            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 1f);
            yield return null;

            ThrowIfRuntimeErrors();
            Require(!controller.IsPlacing, "Ball-in-hand placement must end after confirmation.");
            Require(!controller.enabled, "Ball-in-hand placement must disable itself after confirmation.");
            Require(cueBall.activeSelf, "The cue ball must be active after placement is confirmed.");
            Require(!capture.IsCaptured, "Ball-in-hand confirmation must restore the captured cue ball.");
            Require(
                GetLegacyCurrentStateName(player) == "PlayersPlayState",
                "The held placement trigger must not immediately re-enter the shoot state.");
            Require(
                !(bool)InvokeLegacyMethod(player, "IsShotPowerEnabled"),
                "Shot power must remain disabled until the placement trigger is released.");

            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 0f);
            yield return null;
            ThrowIfRuntimeErrors();
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "Releasing placement input must leave the player ready in play state.");
            Require(!LegacyPocketedBallsContainCueBall(), "The legacy pocket registry must clear the restored cue ball.");

            SetSmokeGamepadState(0f, 0f, 0f, 0f, 0f, 1f);
            yield return null;
            ThrowIfRuntimeErrors();
            Require(
                GetLegacyCurrentStateName(player) == "PlayersShootState",
                "A new primary press after placement release must allow the player to enter the shoot state.");
            Require(
                (bool)InvokeLegacyMethod(player, "IsShotPowerEnabled"),
                "Shot power must be enabled when shooting resumes after ball-in-hand placement.");

            report.BallInHand = true;
            report.Checkpoints.Add("ball-in-hand");
        }

        private static MonoBehaviour FindActiveLegacyComponent(string typeName)
        {
            var component = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate != null
                    && candidate.gameObject.activeInHierarchy
                    && candidate.GetType().Name == typeName);
            if (component == null)
            {
                throw new InvalidOperationException($"Required scene component {typeName} was not found.");
            }

            return component;
        }

        private static Behaviour RequireLegacyBehaviour(object target, string fieldName)
        {
            var value = GetRequiredPublicFieldValue(target, fieldName) as Behaviour;
            if (value == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name}.{fieldName} must reference a Behaviour.");
            }

            return value;
        }

        private static T RequireLegacyBehaviour<T>(object target, string fieldName)
            where T : Behaviour
        {
            var value = GetRequiredPublicFieldValue(target, fieldName) as T;
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{fieldName} must reference {typeof(T).Name}.");
            }

            return value;
        }

        private static object GetRequiredPublicFieldValue(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            return field.GetValue(target);
        }

        private static object InvokeLegacyMethod(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            return method.Invoke(target, arguments);
        }

        private static string GetLegacyCurrentStateName(MonoBehaviour player)
        {
            var field = player.GetType().GetField("currentPlayerState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(player.GetType().FullName, "currentPlayerState");
            }

            return field.GetValue(player)?.GetType().Name;
        }

        private static bool HasInitializedInputReader(Behaviour controller)
        {
            var field = controller.GetType().GetField("localPlayerInputReader", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(controller) != null;
        }

        private static Type GetInputProbeType()
        {
            var probeType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(InputProbeTypeName, false))
                .FirstOrDefault(type => type != null);
            Require(probeType != null, $"Development input probe {InputProbeTypeName} was not found.");
            return probeType;
        }

        private static void SetSmokeGamepadState(
            float aimX,
            float aimY,
            float actionX,
            float actionY,
            float secondaryAction,
            float primaryAction)
        {
            var probeType = GetInputProbeType();
            var setStateMethod = probeType.GetMethod("SetGamepadState", BindingFlags.Public | BindingFlags.Static);
            Require(setStateMethod != null, $"{InputProbeTypeName}.SetGamepadState was not found.");
            setStateMethod.Invoke(
                null,
                new object[] { aimX, aimY, actionX, actionY, secondaryAction, primaryAction });
        }

        private static void RemoveSmokeGamepad()
        {
            Type probeType;
            try
            {
                probeType = GetInputProbeType();
            }
            catch
            {
                return;
            }

            var removeMethod = probeType.GetMethod("RemoveGamepad", BindingFlags.Public | BindingFlags.Static);
            if (removeMethod == null)
            {
                return;
            }

            removeMethod.Invoke(null, null);
        }

        private static bool LegacyPocketedBallsContainCueBall()
        {
            var legacyBallManager = FindActiveLegacyComponent("BallStateManager");
            var instanceField = legacyBallManager.GetType().GetField(
                "instance",
                BindingFlags.Public | BindingFlags.Static);
            if (instanceField == null)
            {
                throw new MissingFieldException(legacyBallManager.GetType().FullName, "instance");
            }

            var authority = instanceField.GetValue(null);
            Require(authority != null, "The legacy BallStateManager authority must be initialized.");
            return (bool)InvokeLegacyMethod(authority, "isPocketedBallContainWhiteBall");
        }

        private static void CaptureRuntimeError(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            lock (RuntimeLogGate)
            {
                var entry = $"{type}: {condition}\n{stackTrace}";
                RuntimeErrors.Add(entry);
                if (!string.IsNullOrWhiteSpace(runtimeErrorJournalPath))
                {
                    File.AppendAllText(runtimeErrorJournalPath, entry + Environment.NewLine + "---" + Environment.NewLine);
                }
            }
        }

        private static void ThrowIfRuntimeErrors()
        {
            string[] errors;
            lock (RuntimeLogGate)
            {
                errors = RuntimeErrors.ToArray();
            }

            if (errors.Length > 0)
            {
                throw new InvalidOperationException(
                    $"The player emitted {errors.Length} runtime error(s):\n{string.Join("\n---\n", errors)}");
            }
        }

        private static Exception UnwrapReflectionException(Exception exception)
        {
            while (exception is TargetInvocationException invocationException
                   && invocationException.InnerException != null)
            {
                exception = invocationException.InnerException;
            }

            return exception;
        }

        private static void StopAllBalls()
        {
            foreach (var identity in FindObjectsByType<BallIdentity>(FindObjectsInactive.Include))
            {
                if (!identity.gameObject.activeInHierarchy || !identity.TryGetComponent<Rigidbody>(out var body))
                {
                    continue;
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }

        private static string ResolveReportPath()
        {
            var configuredPath = Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new InvalidOperationException(
                    $"{ReportPathEnvironmentVariable} must point to the player smoke JSON report.");
            }

            return Path.GetFullPath(configuredPath);
        }

        private static void WriteReport(string path, WindowsPlayerSmokeReport report)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Unable to resolve the report directory for {path}.");
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    [Serializable]
    internal sealed class WindowsPlayerSmokeReport
    {
        public bool Success;
        public string SceneName;
        public int BallCount;
        public bool Startup;
        public bool InputSystem;
        public bool LocalControls;
        public bool ShotStateEntered;
        public bool SpectateStateEntered;
        public bool ReturnedToPlay;
        public bool ShotFlow;
        public bool BallInHand;
        public bool CleanShutdownRequested;
        public string Error;
        public List<string> Checkpoints = new List<string>();
    }
}
#endif
