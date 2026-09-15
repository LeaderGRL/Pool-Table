using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace PoolTable.Editor
{
    public static class WindowsPlayerBuild
    {
        private const string OutputPathEnvironmentVariable = "POOLTABLE_WINDOWS_BUILD_PATH";

        public static void BuildDevelopmentPlayer()
        {
            var outputPath = Environment.GetEnvironmentVariable(OutputPathEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException(
                    $"{OutputPathEnvironmentVariable} must point to the Windows player executable.");
            }

            outputPath = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException($"Unable to resolve the build directory for {outputPath}.");
            }

            Directory.CreateDirectory(outputDirectory);

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("At least one enabled build scene is required.");
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows player build failed with result {report.summary.result} and "
                    + $"{report.summary.totalErrors} error(s).");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException("Unity reported a successful build but the player executable is missing.", outputPath);
            }
        }
    }
}
