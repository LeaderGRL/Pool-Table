using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PoolTable.Tests.EditMode
{
    public sealed class PoolTableLightingSetupTests
    {
        private const string ScenePath = "Assets/Scenes/PoolTable.unity";
        private const string TableMaterialPath =
            "Assets/3D/source/Materials/pool table low_POOL TABLE_BaseColor.mat";
        private const string UrpAssetPath =
            "Assets/Settings/Rendering/PoolTableUniversalRenderPipeline.asset";
        private const string ReflectionCubemapPath =
            "Assets/Settings/Rendering/PoolTableStudioReflection.asset";

        [Test]
        public void PoolTableScene_UsesIntentionalLightingAndReflectionRig()
        {
            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);
                var keyLight = FindRootComponent<Light>(scene, "Pool Table Key Light");
                var overheadA = FindRootComponent<Light>(scene, "Pool Table Overhead Light A");
                var overheadB = FindRootComponent<Light>(scene, "Pool Table Overhead Light B");
                var reflectionProbe = FindRootComponent<ReflectionProbe>(scene, "Pool Table Reflection Probe");

                Assert.That(keyLight.type, Is.EqualTo(LightType.Directional));
                Assert.That(keyLight.shadows, Is.EqualTo(LightShadows.Soft));
                Assert.That(keyLight.intensity, Is.EqualTo(0.62f).Within(0.001f));
                Assert.That(keyLight.shadowStrength, Is.EqualTo(0.68f).Within(0.001f));
                Assert.That(overheadA.type, Is.EqualTo(LightType.Spot));
                Assert.That(overheadB.type, Is.EqualTo(LightType.Spot));
                Assert.That(overheadA.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(overheadB.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(overheadA.intensity, Is.EqualTo(1.45f).Within(0.001f));
                Assert.That(overheadB.intensity, Is.EqualTo(1.45f).Within(0.001f));
                Assert.That(overheadA.transform.position.y, Is.GreaterThan(overheadA.transform.forward.y));
                Assert.That(overheadB.transform.position.y, Is.GreaterThan(overheadB.transform.forward.y));

                Assert.That(reflectionProbe.mode, Is.EqualTo(ReflectionProbeMode.Custom));
                Assert.That(reflectionProbe.boxProjection, Is.True);
                Assert.That(
                    AssetDatabase.GetAssetPath(reflectionProbe.customBakedTexture),
                    Is.EqualTo(ReflectionCubemapPath));

                var tableBounds = CalculateTableBounds(scene);
                var probeBounds = new Bounds(reflectionProbe.transform.position, reflectionProbe.size);
                Assert.That(probeBounds.min.x, Is.LessThanOrEqualTo(tableBounds.min.x));
                Assert.That(probeBounds.max.x, Is.GreaterThanOrEqualTo(tableBounds.max.x));
                Assert.That(probeBounds.min.z, Is.LessThanOrEqualTo(tableBounds.min.z));
                Assert.That(probeBounds.max.z, Is.GreaterThanOrEqualTo(tableBounds.max.z));

                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Trilight));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(0.72f).Within(0.001f));
                Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(0.72f).Within(0.001f));
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PoolTableUrpAsset_EnablesLocalReflectionProbeFeatures()
        {
            var urpAsset = AssetDatabase.LoadMainAssetAtPath(UrpAssetPath);
            Assert.That(urpAsset, Is.Not.Null);

            var serializedAsset = new SerializedObject(urpAsset);
            AssertEnabled(serializedAsset.FindProperty("m_ReflectionProbeBlending"));
            AssertEnabled(serializedAsset.FindProperty("m_ReflectionProbeBoxProjection"));
        }

        [Test]
        public void PoolTableStudioReflection_IsHdrCubemapWithDirectionalContrast()
        {
            var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionCubemapPath);
            Assert.That(cubemap, Is.Not.Null);
            Assert.That(cubemap.width, Is.EqualTo(128));
            Assert.That(cubemap.mipmapCount, Is.GreaterThan(1));

            var top = cubemap.GetPixel(CubemapFace.PositiveY, cubemap.width / 2, cubemap.height / 2);
            var bottom = cubemap.GetPixel(CubemapFace.NegativeY, cubemap.width / 2, cubemap.height / 2);
            Assert.That(top.maxColorComponent, Is.GreaterThan(bottom.maxColorComponent * 4f));
        }

        private static Bounds CalculateTableBounds(Scene scene)
        {
            var tableMaterial = AssetDatabase.LoadAssetAtPath<Material>(TableMaterialPath);
            Assert.That(tableMaterial, Is.Not.Null);

            var hasBounds = false;
            var bounds = default(Bounds);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var usesTableMaterial = false;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == tableMaterial)
                        {
                            usesTableMaterial = true;
                            break;
                        }
                    }

                    if (!usesTableMaterial)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            Assert.That(hasBounds, Is.True);
            return bounds;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindRootComponent<T>(Scene scene, string objectName) where T : Component
        {
            var gameObject = FindRoot(scene, objectName);
            Assert.That(gameObject, Is.Not.Null, $"Missing root object '{objectName}'.");
            var component = gameObject.GetComponent<T>();
            Assert.That(component, Is.Not.Null, $"Missing {typeof(T).Name} on '{objectName}'.");
            return component;
        }

        private static void AssertEnabled(SerializedProperty property)
        {
            Assert.That(property, Is.Not.Null);
            var enabled = property.propertyType == SerializedPropertyType.Boolean
                ? property.boolValue
                : property.intValue != 0;
            Assert.That(enabled, Is.True);
        }
    }
}
