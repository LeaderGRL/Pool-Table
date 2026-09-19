using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        private static readonly IReadOnlyDictionary<string, string[]> ExpectedRuntimeReferences =
            new Dictionary<string, string[]>
            {
                ["PoolTable.Core"] = Array.Empty<string>(),
                ["PoolTable.Physics"] = new[] { "PoolTable.Core" },
                ["PoolTable.Input"] = new[] { "PoolTable.Core", "Unity.InputSystem" },
                ["PoolTable.Gameplay"] = new[] { "PoolTable.Core", "PoolTable.Physics", "PoolTable.Input" },
                ["PoolTable.Networking"] = new[] { "PoolTable.Core", "PoolTable.Gameplay" },
                ["PoolTable.Presentation"] = new[] { "PoolTable.Core", "PoolTable.Gameplay" },
            };

        private static readonly string[] RuntimeAssemblyNames = ExpectedRuntimeReferences.Keys.ToArray();

        private static readonly IReadOnlyDictionary<string, string[]> ExpectedCompatibilityReferences =
            new Dictionary<string, string[]>
            {
                ["PoolTable.Presentation.UI"] = new[] { "PoolTable.Core" },
            };

        private static readonly string[] CompatibilityAssemblyNames = ExpectedCompatibilityReferences.Keys.ToArray();

        private static readonly IReadOnlyDictionary<string, string[]> ExpectedTestOnlyReferences =
            new Dictionary<string, string[]>
            {
                ["EditMode"] = Array.Empty<string>(),
                ["PlayMode"] = new[] { "Unity.InputSystem", "Unity.InputSystem.TestFramework" },
            };

        [Test]
        public void RuntimeAssemblies_KeepExpectedDependencyGraph()
        {
            foreach (var assemblyName in RuntimeAssemblyNames)
            {
                var definition = LoadAssemblyDefinition(GetRuntimeAssemblyPath(assemblyName), "autoReferenced");

                Assert.That(definition.name, Is.EqualTo(assemblyName));
                Assert.That(definition.autoReferenced, Is.False, $"{assemblyName} must require explicit references.");
                Assert.That(
                    definition.references ?? Array.Empty<string>(),
                    Is.EquivalentTo(ExpectedRuntimeReferences[assemblyName]),
                    $"{assemblyName} has an unexpected dependency graph.");
            }
        }

        [Test]
        public void CoreAssembly_RemainsIndependentFromUnityEngine()
        {
            var definition = LoadAssemblyDefinition(GetRuntimeAssemblyPath("PoolTable.Core"), "autoReferenced", "noEngineReferences");

            Assert.That(definition.noEngineReferences, Is.True);
            Assert.That(definition.references, Is.Empty);
        }

        [Test]
        public void LegacyCompatibilityAssemblies_KeepTheirExplicitDependencyBoundary()
        {
            foreach (var assemblyName in CompatibilityAssemblyNames)
            {
                var path = assemblyName switch
                {
                    "PoolTable.Presentation.UI" => Path.Combine(
                        Application.dataPath,
                        "PoolTable",
                        "Presentation",
                        "UI",
                        "PoolTable.Presentation.UI.asmdef"),
                    _ => throw new InvalidOperationException($"Unknown compatibility assembly {assemblyName}."),
                };
                var definition = LoadAssemblyDefinition(path, "autoReferenced");

                Assert.That(definition.name, Is.EqualTo(assemblyName));
                Assert.That(
                    definition.autoReferenced,
                    Is.True,
                    $"{assemblyName} must remain visible to the temporary Assembly-CSharp migration bridge.");
                Assert.That(
                    definition.references ?? Array.Empty<string>(),
                    Is.EquivalentTo(ExpectedCompatibilityReferences[assemblyName]),
                    $"{assemblyName} has an unexpected compatibility dependency graph.");
            }
        }

        [TestCase("EditMode")]
        [TestCase("PlayMode")]
        public void TestAssemblies_ReferenceEveryModernRuntimeAssembly(string testMode)
        {
            var path = Path.Combine(
                Application.dataPath,
                "Tests",
                testMode,
                $"PoolTable.{testMode}.Tests.asmdef");
            var definition = LoadAssemblyDefinition(path);

            var expectedReferences = RuntimeAssemblyNames
                .Concat(CompatibilityAssemblyNames)
                .Concat(ExpectedTestOnlyReferences[testMode])
                .ToArray();

            Assert.That(definition.references, Is.EquivalentTo(expectedReferences));
        }

        private static string GetRuntimeAssemblyPath(string assemblyName)
        {
            var moduleName = assemblyName.Substring("PoolTable.".Length);
            return Path.Combine(Application.dataPath, "PoolTable", moduleName, $"{assemblyName}.asmdef");
        }

        private static AssemblyDefinitionData LoadAssemblyDefinition(string path, params string[] requiredProperties)
        {
            Assert.That(File.Exists(path), Is.True, $"Assembly definition not found at {path}.");

            var json = File.ReadAllText(path);

            foreach (var property in requiredProperties)
            {
                Assert.That(
                    json.Contains($"\"{property}\""),
                    Is.True,
                    $"Assembly definition at {path} must explicitly declare {property}.");
            }

            var definition = JsonUtility.FromJson<AssemblyDefinitionData>(json);
            Assert.That(definition, Is.Not.Null, $"Unable to parse assembly definition at {path}.");

            return definition;
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
            public string[] references;
            public bool autoReferenced;
            public bool noEngineReferences;
        }
    }
}
