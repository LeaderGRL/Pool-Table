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
                ["PoolTable.Input"] = new[] { "PoolTable.Core" },
                ["PoolTable.Gameplay"] = new[] { "PoolTable.Core", "PoolTable.Physics", "PoolTable.Input" },
                ["PoolTable.Networking"] = new[] { "PoolTable.Core", "PoolTable.Gameplay" },
                ["PoolTable.Presentation"] = new[] { "PoolTable.Core", "PoolTable.Gameplay" },
            };

        private static readonly string[] RuntimeAssemblyNames = ExpectedRuntimeReferences.Keys.ToArray();

        [Test]
        public void RuntimeAssemblies_KeepExpectedDependencyGraph()
        {
            foreach (var assemblyName in RuntimeAssemblyNames)
            {
                var definition = LoadAssemblyDefinition(GetRuntimeAssemblyPath(assemblyName));

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
            var definition = LoadAssemblyDefinition(GetRuntimeAssemblyPath("PoolTable.Core"));

            Assert.That(definition.noEngineReferences, Is.True);
            Assert.That(definition.references, Is.Empty);
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

            Assert.That(definition.references, Is.EquivalentTo(RuntimeAssemblyNames));
        }

        private static string GetRuntimeAssemblyPath(string assemblyName)
        {
            var moduleName = assemblyName.Substring("PoolTable.".Length);
            return Path.Combine(Application.dataPath, "PoolTable", moduleName, $"{assemblyName}.asmdef");
        }

        private static AssemblyDefinitionData LoadAssemblyDefinition(string path)
        {
            Assert.That(File.Exists(path), Is.True, $"Assembly definition not found at {path}.");

            var definition = JsonUtility.FromJson<AssemblyDefinitionData>(File.ReadAllText(path));
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
