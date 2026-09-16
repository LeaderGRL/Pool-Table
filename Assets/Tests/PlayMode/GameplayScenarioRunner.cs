using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoolTable.Gameplay.Balls;
using PoolTable.Physics.Instrumentation;
using UnityEngine;

namespace PoolTable.Tests.PlayMode
{
    internal sealed class GameplayScenarioRunner
    {
        private readonly GameplayScenarioReport report;

        public GameplayScenarioRunner(string scenarioName, int seed)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new ArgumentException("A scenario name is required.", nameof(scenarioName));
            }

            report = new GameplayScenarioReport
            {
                ScenarioName = scenarioName,
                Seed = seed,
            };
            Random = new System.Random(seed);
        }

        public System.Random Random { get; }

        public GameplayScenarioReport Report => report;

        public void CaptureCheckpoint(string name, IEnumerable<BallIdentity> balls)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A checkpoint name is required.", nameof(name));
            }

            if (balls == null)
            {
                throw new ArgumentNullException(nameof(balls));
            }

            var checkpoint = new GameplayScenarioCheckpoint
            {
                Name = name,
                Balls = balls
                    .OrderBy(identity => identity.Id.Number)
                    .Select(CaptureBall)
                    .ToList(),
            };

            report.Checkpoints.Add(checkpoint);
        }

        public void AttachShotReport(ShotSimulationReport shotReport)
        {
            if (shotReport == null)
            {
                throw new ArgumentNullException(nameof(shotReport));
            }

            report.Shot = new GameplayScenarioShotSummary
            {
                DurationSeconds = shotReport.DurationSeconds,
                TrackCount = shotReport.Tracks.Count,
                CollisionCount = shotReport.CollisionCount,
                TotalDistanceTraveledMeters = shotReport.TotalDistanceTraveledMeters,
                InitialTotalKineticEnergyJoules = shotReport.InitialTotalKineticEnergyJoules,
                FinalTotalKineticEnergyJoules = shotReport.FinalTotalKineticEnergyJoules,
                PeakTotalKineticEnergyJoules = shotReport.PeakTotalKineticEnergyJoules,
            };
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(report, prettyPrint);
        }

        public string WriteJsonReport(string outputDirectory = null)
        {
            var directory = string.IsNullOrWhiteSpace(outputDirectory)
                ? ResolveDefaultOutputDirectory()
                : Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"{SanitizeFileName(report.ScenarioName)}.json");
            File.WriteAllText(path, ToJson());
            return path;
        }

        private static GameplayScenarioBallState CaptureBall(BallIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentException("Scenario checkpoints cannot contain a null ball identity.", nameof(identity));
            }

            if (!identity.TryGetComponent<Rigidbody>(out var body))
            {
                throw new InvalidOperationException($"Ball {identity.Id} requires a Rigidbody for scenario checkpoints.");
            }

            return new GameplayScenarioBallState
            {
                BallNumber = identity.Id.Number,
                Active = identity.gameObject.activeInHierarchy,
                PositionMeters = body.position,
                LinearVelocityMetersPerSecond = body.linearVelocity,
                AngularVelocityRadiansPerSecond = body.angularVelocity,
            };
        }

        private static string ResolveDefaultOutputDirectory()
        {
            var configured = Environment.GetEnvironmentVariable("POOLTABLE_SCENARIO_REPORT_DIR");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Logs", "TestResults", "Scenarios");
        }

        private static string SanitizeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            return new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        }
    }

    [Serializable]
    internal sealed class GameplayScenarioReport
    {
        public string ScenarioName;
        public int Seed;
        public List<GameplayScenarioCheckpoint> Checkpoints = new();
        public GameplayScenarioShotSummary Shot;
    }

    [Serializable]
    internal sealed class GameplayScenarioCheckpoint
    {
        public string Name;
        public List<GameplayScenarioBallState> Balls = new();
    }

    [Serializable]
    internal sealed class GameplayScenarioBallState
    {
        public int BallNumber;
        public bool Active;
        public Vector3 PositionMeters;
        public Vector3 LinearVelocityMetersPerSecond;
        public Vector3 AngularVelocityRadiansPerSecond;
    }

    [Serializable]
    internal sealed class GameplayScenarioShotSummary
    {
        public double DurationSeconds;
        public int TrackCount;
        public int CollisionCount;
        public float TotalDistanceTraveledMeters;
        public float InitialTotalKineticEnergyJoules;
        public float FinalTotalKineticEnergyJoules;
        public float PeakTotalKineticEnergyJoules;
    }
}
