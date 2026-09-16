using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class UniversalRenderPipelineMaterialTests
    {
        private const string UrpShaderPrefix = "Universal Render Pipeline/";
        private const string PoolTableScenePath = "Assets/Scenes/PoolTable.unity";
        private const string CueBallMaterialPath = "Assets/Materials/CueBall.mat";
        private const string TableMaterialPath = "Assets/3D/source/Materials/pool table low_POOL TABLE_BaseColor.mat";
        private const string BallMaterialPath = "Assets/Billiard Balls/Models/Materials/Ball_01.mat";
        private static readonly string[] BallMaterialRoots =
        {
            "Assets/3D/textures/Ball_texture/Materials",
            "Assets/Billiard Balls/Materials",
            "Assets/Billiard Balls/Models/Materials"
        };

        [Test]
        public void ProjectMaterials_UseUniversalRenderPipelineShaders()
        {
            var materialPaths = AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(materialPaths, Is.Not.Empty);

            foreach (var materialPath in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                Assert.That(material, Is.Not.Null, $"Unable to load material at '{materialPath}'.");
                Assert.That(material.shader, Is.Not.Null, $"Material '{materialPath}' has no shader assigned.");
                Assert.That(
                    material.shader.name,
                    Does.StartWith(UrpShaderPrefix),
                    $"Material '{materialPath}' still uses non-URP shader '{material.shader.name}'.");
            }
        }

        [Test]
        public void RepresentativeConvertedMaterials_PreserveBaseTextures()
        {
            AssertConvertedMaterial(TableMaterialPath, "Universal Render Pipeline/Lit");
            AssertConvertedMaterial(BallMaterialPath, "Universal Render Pipeline/Simple Lit");
        }

        [Test]
        public void ConvertedSimpleLitBallMaterials_PreserveScalarSmoothness()
        {
            var materialPaths = BallMaterialRoots
                .SelectMany(root => AssetDatabase.FindAssets("t:Material", new[] { root }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path)
                .ToArray();

            Assert.That(materialPaths, Is.Not.Empty);

            foreach (var materialPath in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null || material.shader == null || material.shader.name != "Universal Render Pipeline/Simple Lit")
                {
                    continue;
                }

                Assert.That(
                    material.GetFloat("_SmoothnessSource"),
                    Is.EqualTo(0f),
                    $"Material '{materialPath}' must use scalar/specular smoothness so the preserved legacy value is not replaced by base-map alpha.");
                Assert.That(
                    material.GetColor("_SpecColor").a,
                    Is.EqualTo(material.GetFloat("_Smoothness")).Within(0.0001f),
                    $"Material '{materialPath}' must store its scalar smoothness in the Simple Lit specular alpha channel.");
            }
        }

        [Test]
        public void CueBallMaterial_IsNonEmissive()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(CueBallMaterialPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.False);
            Assert.That(material.GetColor("_EmissionColor").maxColorComponent, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void PoolTableScene_MeshRenderersUseUniversalRenderPipelineMaterials()
        {
            var scene = EditorSceneManager.OpenPreviewScene(PoolTableScenePath);

            try
            {
                Assert.That(scene.IsValid(), Is.True, $"Failed to open preview scene for {PoolTableScenePath}.");

                var invalidMaterials = new List<string>();

                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    foreach (var renderer in rootObject.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null)
                            {
                                invalidMaterials.Add($"{GetHierarchyPath(renderer.transform)}: missing material");
                                continue;
                            }

                            if (material.shader == null || !material.shader.name.StartsWith(UrpShaderPrefix))
                            {
                                invalidMaterials.Add(
                                    $"{GetHierarchyPath(renderer.transform)}: {material.name} ({material.shader?.name ?? "missing shader"})");
                            }
                        }
                    }
                }

                Assert.That(
                    invalidMaterials,
                    Is.Empty,
                    "PoolTable scene contains MeshRenderer materials that are not URP-compatible:\n" +
                    string.Join("\n", invalidMaterials));

                var cueBallRenderer = scene.GetRootGameObjects()
                    .SelectMany(rootObject => rootObject.GetComponentsInChildren<MeshRenderer>(true))
                    .FirstOrDefault(renderer => renderer.CompareTag("white"));

                Assert.That(cueBallRenderer, Is.Not.Null, "Cue ball MeshRenderer was not found in PoolTable scene.");
                Assert.That(
                    AssetDatabase.GetAssetPath(cueBallRenderer.sharedMaterial),
                    Is.EqualTo(CueBallMaterialPath),
                    "Cue ball must use the dedicated URP material so its original plain-white appearance is preserved.");
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        private static void AssertConvertedMaterial(string materialPath, string expectedShaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Assert.That(material, Is.Not.Null, $"Unable to load material at '{materialPath}'.");
            Assert.That(material.shader, Is.Not.Null, $"Material '{materialPath}' has no shader assigned.");
            Assert.That(material.shader.name, Is.EqualTo(expectedShaderName));
            Assert.That(
                material.GetTexture("_BaseMap"),
                Is.Not.Null,
                $"Material '{materialPath}' lost its base texture during URP conversion.");
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
