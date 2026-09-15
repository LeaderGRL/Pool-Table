#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PoolTable.Gameplay.BallInHand;
using PoolTable.Gameplay.Balls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoolTable.Presentation.Diagnostics
{
    internal sealed class WindowsPlayerSmokeRunner : MonoBehaviour
    {
        private const string SmokeArgument = "-pooltable-player-smoke";
        private const string ReportPathEnvironmentVariable = "POOLTABLE_PLAYER_SMOKE_REPORT";
        private const string InputProbeTypeName = "PoolTable.Input.WindowsPlayerInputSmokeProbe";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (!Environment.GetCommandLineArgs().Any(argument =>
                    string.Equals(argument, SmokeArgument, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var host = new GameObject(nameof(WindowsPlayerSmokeRunner));
            DontDestroyOnLoad(host);
            host.AddComponent<WindowsPlayerSmokeRunner>();
        }

        private IEnumerator Start()
        {
            var report = new WindowsPlayerSmokeReport();
            var reportPath = ResolveReportPath();

            yield return null;
            yield return null;

            try
            {
                ValidateStartup(report);
                ValidateLocalControls(report);
                ValidateShotFlow(report);
                ValidateBallInHand(report);
                report.Success = true;
            }
            catch (Exception exception)
            {
                report.Success = false;
                report.Error = exception.ToString();
                Debug.LogException(exception);
            }

            report.CleanShutdownRequested = true;
            WriteReport(reportPath, report);
            Debug.Log($"Windows player smoke report: {reportPath}");

            yield return null;
            Application.Quit(report.Success ? 0 : 1);
        }

        private static void ValidateStartup(WindowsPlayerSmokeReport report)
        {
            var activeScene = SceneManager.GetActiveScene();
            Require(activeScene.IsValid() && activeScene.isLoaded, "The active scene must be loaded.");
            Require(activeScene.name == "PoolTable", $"Expected PoolTable scene, found {activeScene.name}.");

            var identities = FindObjectsByType<BallIdentity>(FindObjectsInactive.Include);
            Require(identities.Length == 16, $"Expected 16 billiard balls, found {identities.Length}.");
            Require(identities.Count(identity => identity.IsCueBall) == 1, "Exactly one cue ball is required.");

            var gameManager = FindActiveLegacyComponent("GameManager");
            var currentTurn = InvokeLegacyMethod(gameManager, "getCurrentPlayerTurn");
            Require(currentTurn?.ToString() == "PlayerOneTurn", "Player One must own the initial turn.");

            report.SceneName = activeScene.name;
            report.BallCount = identities.Length;
            report.Startup = true;
            report.Checkpoints.Add("startup");
        }

        private static void ValidateLocalControls(WindowsPlayerSmokeReport report)
        {
            var player = FindActiveLegacyComponent("PlayersStateManagement");
            var aiming = RequireLegacyBehaviour(player, "aimingController");
            var spin = RequireLegacyBehaviour(player, "spinController");
            var shotPower = RequireLegacyBehaviour(player, "shotPowerController");
            var placement = RequireLegacyBehaviour(player, "ballInHandPlacementController");

            Require(aiming.enabled, "Aiming must be enabled while the player is in the play state.");
            Require(spin.enabled, "Spin control must be enabled while the player is in the play state.");
            Require(!shotPower.enabled, "Shot power must be disabled before a shot starts.");
            Require(!placement.enabled, "Ball-in-hand placement must be inactive at startup.");
            Require(HasInitializedInputReader(aiming), "The aiming controller must initialize its local input reader.");
            Require(HasInitializedInputReader(spin), "The spin controller must initialize its local input reader.");
            Require(HasInitializedInputReader(shotPower), "The shot-power controller must initialize its local input reader.");
            ValidateInputSystemPath(aiming);

            report.InputSystem = true;
            report.LocalControls = true;
            report.Checkpoints.Add("local-controls");
        }

        private static void ValidateShotFlow(WindowsPlayerSmokeReport report)
        {
            var player = FindActiveLegacyComponent("PlayersStateManagement");
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "The player must start in the play state.");

            var shootState = GetRequiredPublicFieldValue(player, "shootState");
            InvokeLegacyMethod(player, "SwitchState", shootState);
            Require(GetLegacyCurrentStateName(player) == "PlayersShootState", "The player must enter the shoot state.");
            Require((bool)InvokeLegacyMethod(player, "IsShotPowerEnabled"), "Shot power must be enabled in the shoot state.");
            report.ShotStateEntered = true;

            var shotPower = RequireLegacyBehaviour(player, "shotPowerController");
            shotPower.enabled = false;
            InvokeLegacyMethod(shootState, "UpdateState", player);
            Require(GetLegacyCurrentStateName(player) == "PlayersSpectateState", "The player must enter the spectate state after the shot completes.");
            report.SpectateStateEntered = true;

            StopAllBalls();
            var spectateState = GetRequiredPublicFieldValue(player, "spectateState");
            InvokeLegacyMethod(spectateState, "UpdateState", player);
            Require(GetLegacyCurrentStateName(player) == "PlayersPlayState", "The player must return to play after every active ball stops.");

            report.ReturnedToPlay = true;
            report.ShotFlow = true;
            report.Checkpoints.Add("shot-flow");
        }

        private static void ValidateBallInHand(WindowsPlayerSmokeReport report)
        {
            var controller = FindObjectsByType<BallInHandPlacementController>(
                    FindObjectsInactive.Include)
                .Single();

            Require(!controller.IsPlacing, "Ball-in-hand placement must be inactive before the smoke scenario starts.");
            controller.BeginLegacyScratchPlacement();
            Require(controller.IsPlacing, "Ball-in-hand placement must become active after a scratch.");
            Require(controller.IsCurrentPositionLegal, "The initial ball-in-hand candidate must be legal.");
            Require(controller.TryConfirmPlacement(), "A legal ball-in-hand position must be confirmable.");
            Require(!controller.IsPlacing, "Ball-in-hand placement must end after confirmation.");

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

        private static void ValidateInputSystemPath(Behaviour controller)
        {
            var readerField = controller.GetType().GetField(
                "localPlayerInputReader",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var reader = readerField?.GetValue(controller);
            Require(reader != null, $"{controller.GetType().Name} must expose an initialized local input reader.");

            var probeType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(InputProbeTypeName, false))
                .FirstOrDefault(type => type != null);
            Require(probeType != null, $"Development input probe {InputProbeTypeName} was not found.");

            var injectMethod = probeType.GetMethod("InjectGamepadState", BindingFlags.Public | BindingFlags.Static);
            var removeMethod = probeType.GetMethod("RemoveGamepad", BindingFlags.Public | BindingFlags.Static);
            Require(injectMethod != null, $"{InputProbeTypeName}.InjectGamepadState was not found.");
            Require(removeMethod != null, $"{InputProbeTypeName}.RemoveGamepad was not found.");

            try
            {
                injectMethod.Invoke(null, null);
                var snapshot = InvokeLegacyMethod(reader, "Read");
                var snapshotType = snapshot.GetType();
                var aimAxis = (Vector2)GetRequiredPublicPropertyValue(snapshot, snapshotType, "AimAxis");
                var actionAxis = (Vector2)GetRequiredPublicPropertyValue(snapshot, snapshotType, "ActionAxis");
                var primaryAction = (bool)GetRequiredPublicPropertyValue(snapshot, snapshotType, "PrimaryActionIsPressed");
                var secondaryAction = (bool)GetRequiredPublicPropertyValue(snapshot, snapshotType, "SecondaryActionIsPressed");

                Require(aimAxis.sqrMagnitude > 0f, "The real Input System gamepad aim axis must reach the local input reader.");
                Require(actionAxis.sqrMagnitude > 0f, "The real Input System gamepad action axis must reach the local input reader.");
                Require(primaryAction, "The real Input System primary action must reach the local input reader.");
                Require(secondaryAction, "The real Input System secondary action must reach the local input reader.");
            }
            finally
            {
                removeMethod.Invoke(null, null);
            }
        }

        private static object GetRequiredPublicPropertyValue(object target, Type targetType, string propertyName)
        {
            var property = targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new MissingMemberException(targetType.FullName, propertyName);
            }

            return property.GetValue(target);
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
