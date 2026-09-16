using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoolTable.Tests.EditMode
{
    public sealed class PoolTablePostProcessingSetupTests
    {
        private const string ScenePath = "Assets/Scenes/PoolTable.unity";
        private const string ProfilePath =
            "Assets/Settings/Rendering/PoolTablePostProcessingProfile.asset";
        private const string VolumeName = "Pool Table Post Processing Volume";
        private const string AdditionalCameraDataType =
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData";
        private const string VolumeType = "UnityEngine.Rendering.Volume";

        [Test]
        public void PoolTableScene_UsesDedicatedGlobalPostProcessingVolume()
        {
            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);

                var cameraObject = FindRoot(scene, "Main Camera");
                Assert.That(cameraObject, Is.Not.Null);

                var cameraData = FindComponentByTypeName(cameraObject, AdditionalCameraDataType);
                Assert.That(cameraData, Is.Not.Null);
                var serializedCameraData = new SerializedObject(cameraData);
                Assert.That(serializedCameraData.FindProperty("m_RenderPostProcessing").boolValue, Is.True);
                Assert.That(
                    serializedCameraData.FindProperty("m_VolumeLayerMask.m_Bits").intValue & 1,
                    Is.EqualTo(1));

                var volumeObject = FindRoot(scene, VolumeName);
                Assert.That(volumeObject, Is.Not.Null);
                Assert.That(volumeObject.layer, Is.EqualTo(0));

                var volume = FindComponentByTypeName(volumeObject, VolumeType);
                Assert.That(volume, Is.Not.Null);
                var serializedVolume = new SerializedObject(volume);
                Assert.That(serializedVolume.FindProperty("m_IsGlobal").boolValue, Is.True);
                Assert.That(serializedVolume.FindProperty("priority").floatValue, Is.EqualTo(10f).Within(0.001f));
                Assert.That(serializedVolume.FindProperty("weight").floatValue, Is.EqualTo(1f).Within(0.001f));
                Assert.That(
                    AssetDatabase.GetAssetPath(serializedVolume.FindProperty("sharedProfile").objectReferenceValue),
                    Is.EqualTo(ProfilePath));
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
        public void PoolTablePostProcessingProfile_UsesRestrainedExplicitOverrides()
        {
            var profile = AssetDatabase.LoadMainAssetAtPath(ProfilePath);
            Assert.That(profile, Is.Not.Null);

            var serializedProfile = new SerializedObject(profile);
            var components = serializedProfile.FindProperty("components");
            Assert.That(components, Is.Not.Null);
            Assert.That(components.arraySize, Is.EqualTo(4));
            for (var i = 0; i < components.arraySize; i++)
            {
                Assert.That(components.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(ProfilePath);
            var tonemapping = FindSubAsset(allAssets, "UnityEngine.Rendering.Universal.Tonemapping");
            var colorAdjustments = FindSubAsset(allAssets, "UnityEngine.Rendering.Universal.ColorAdjustments");
            var bloom = FindSubAsset(allAssets, "UnityEngine.Rendering.Universal.Bloom");
            var vignette = FindSubAsset(allAssets, "UnityEngine.Rendering.Universal.Vignette");

            AssertIntOverride(tonemapping, "mode", 2);

            AssertFloatOverride(colorAdjustments, "postExposure", 0.1f);
            AssertFloatOverride(colorAdjustments, "contrast", 5f);
            AssertFloatOverride(colorAdjustments, "saturation", -4f);

            AssertFloatOverride(bloom, "threshold", 1f);
            AssertFloatOverride(bloom, "intensity", 0.08f);
            AssertFloatOverride(bloom, "scatter", 0.55f);

            AssertFloatOverride(vignette, "intensity", 0.12f);
            AssertFloatOverride(vignette, "smoothness", 0.32f);
            AssertBoolOverride(vignette, "rounded", false);
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == objectName);
        }

        private static Component FindComponentByTypeName(GameObject gameObject, string typeName)
        {
            return gameObject.GetComponents<Component>()
                .FirstOrDefault(component => component != null && component.GetType().FullName == typeName);
        }

        private static Object FindSubAsset(Object[] assets, string typeName)
        {
            var asset = assets.FirstOrDefault(candidate => candidate != null && candidate.GetType().FullName == typeName);
            Assert.That(asset, Is.Not.Null, $"Missing profile component '{typeName}'.");
            return asset;
        }

        private static void AssertFloatOverride(Object component, string propertyName, float expected)
        {
            var property = GetOverrideProperty(component, propertyName);
            Assert.That(property.FindPropertyRelative("m_Value").floatValue, Is.EqualTo(expected).Within(0.001f));
        }

        private static void AssertIntOverride(Object component, string propertyName, int expected)
        {
            var property = GetOverrideProperty(component, propertyName);
            Assert.That(property.FindPropertyRelative("m_Value").intValue, Is.EqualTo(expected));
        }

        private static void AssertBoolOverride(Object component, string propertyName, bool expected)
        {
            var property = GetOverrideProperty(component, propertyName);
            Assert.That(property.FindPropertyRelative("m_Value").boolValue, Is.EqualTo(expected));
        }

        private static SerializedProperty GetOverrideProperty(Object component, string propertyName)
        {
            var property = new SerializedObject(component).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property '{propertyName}'.");
            Assert.That(property.FindPropertyRelative("m_OverrideState").boolValue, Is.True);
            return property;
        }
    }
}
