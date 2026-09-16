using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class PlatformQualityProfilesTests
    {
        private const string WindowsQualityName = "Pool Table Windows";
        private const string WebQualityName = "Pool Table Web";
        private const string WindowsPipelinePath = "Assets/Settings/Rendering/PoolTableWindowsRenderPipeline.asset";
        private const string WebPipelinePath = "Assets/Settings/Rendering/PoolTableWebRenderPipeline.asset";
        private const string RendererPath = "Assets/Settings/Rendering/PoolTableUniversalRenderer.asset";

        [Test]
        public void QualitySettings_DefinesPlatformSpecificProfilesAndDefaults()
        {
            var serialized = new SerializedObject(QualitySettings.GetQualitySettings());
            var levels = serialized.FindProperty("m_QualitySettings");

            Assert.That(levels, Is.Not.Null);
            Assert.That(levels.isArray, Is.True);

            var windowsIndex = FindQualityLevelIndex(levels, WindowsQualityName);
            var webIndex = FindQualityLevelIndex(levels, WebQualityName);

            Assert.That(windowsIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(webIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(GetPlatformDefaultIndex(serialized, "Standalone"), Is.EqualTo(windowsIndex));
            Assert.That(GetPlatformDefaultIndex(serialized, "WebGL"), Is.EqualTo(webIndex));

            AssertQualityPipeline(levels.GetArrayElementAtIndex(windowsIndex), WindowsPipelinePath);
            AssertQualityPipeline(levels.GetArrayElementAtIndex(webIndex), WebPipelinePath);
        }

        [Test]
        public void PlatformPipelines_ShareTheProjectRenderer()
        {
            AssertPipelineRenderer(WindowsPipelinePath);
            AssertPipelineRenderer(WebPipelinePath);
        }

        [Test]
        public void WindowsPipeline_PreservesDesktopVisualBaseline()
        {
            var serialized = LoadPipeline(WindowsPipelinePath);

            AssertBool(serialized, "m_SupportsHDR", true);
            AssertFloat(serialized, "m_RenderScale", 1f);
            AssertInt(serialized, "m_MainLightShadowmapResolution", 2048);
            AssertInt(serialized, "m_AdditionalLightsPerObjectLimit", 4);
            AssertBool(serialized, "m_ReflectionProbeBlending", true);
            AssertBool(serialized, "m_ReflectionProbeBoxProjection", true);
            AssertFloat(serialized, "m_ShadowDistance", 50f);
            AssertInt(serialized, "m_SoftShadowQuality", 2);
            AssertInt(serialized, "m_ColorGradingLutSize", 32);
            AssertBool(serialized, "m_UseFastSRGBLinearConversion", false);
        }

        [Test]
        public void WebPipeline_ReducesRenderCostWhileKeepingCoreVisualFeatures()
        {
            var serialized = LoadPipeline(WebPipelinePath);

            AssertBool(serialized, "m_SupportsHDR", true);
            AssertFloat(serialized, "m_RenderScale", 0.85f);
            AssertInt(serialized, "m_MainLightShadowmapResolution", 1024);
            AssertInt(serialized, "m_AdditionalLightsPerObjectLimit", 2);
            AssertBool(serialized, "m_ReflectionProbeBlending", true);
            AssertBool(serialized, "m_ReflectionProbeBoxProjection", true);
            AssertFloat(serialized, "m_ShadowDistance", 35f);
            AssertInt(serialized, "m_SoftShadowQuality", 1);
            AssertInt(serialized, "m_ColorGradingLutSize", 16);
            AssertBool(serialized, "m_UseFastSRGBLinearConversion", false);
        }

        [Test]
        public void WebQuality_DisablesRealtimeProbeUpdatesWithoutDisablingReflections()
        {
            var serialized = new SerializedObject(QualitySettings.GetQualitySettings());
            var levels = serialized.FindProperty("m_QualitySettings");
            var webIndex = FindQualityLevelIndex(levels, WebQualityName);

            Assert.That(webIndex, Is.GreaterThanOrEqualTo(0));

            var webLevel = levels.GetArrayElementAtIndex(webIndex);
            Assert.That(webLevel.FindPropertyRelative("realtimeReflectionProbes").boolValue, Is.False);
            Assert.That(webLevel.FindPropertyRelative("vSyncCount").intValue, Is.Zero);
            Assert.That(webLevel.FindPropertyRelative("lodBias").floatValue, Is.EqualTo(1f).Within(0.0001f));
        }

        private static SerializedObject LoadPipeline(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            Assert.That(asset, Is.Not.Null, $"Missing render pipeline asset at {assetPath}.");
            Assert.That(
                asset.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"));
            return new SerializedObject(asset);
        }

        private static void AssertPipelineRenderer(string pipelinePath)
        {
            var serialized = LoadPipeline(pipelinePath);
            var rendererDataList = serialized.FindProperty("m_RendererDataList");

            Assert.That(rendererDataList, Is.Not.Null);
            Assert.That(rendererDataList.arraySize, Is.GreaterThan(0));

            var renderer = rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(renderer), Is.EqualTo(RendererPath));
        }

        private static void AssertQualityPipeline(SerializedProperty qualityLevel, string expectedPath)
        {
            var pipeline = qualityLevel.FindPropertyRelative("customRenderPipeline");
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline.objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pipeline.objectReferenceValue), Is.EqualTo(expectedPath));
        }

        private static int FindQualityLevelIndex(SerializedProperty levels, string qualityName)
        {
            for (var index = 0; index < levels.arraySize; index++)
            {
                var name = levels.GetArrayElementAtIndex(index).FindPropertyRelative("name");
                if (name != null && name.stringValue == qualityName)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int GetPlatformDefaultIndex(SerializedObject serialized, string platformName)
        {
            var platformDefaults = serialized.FindProperty("m_PerPlatformDefaultQuality");
            Assert.That(platformDefaults, Is.Not.Null);

            var iterator = platformDefaults.Copy();
            var end = iterator.GetEndProperty();
            string currentKey = null;

            while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
            {
                if (iterator.name == "first" && iterator.propertyType == SerializedPropertyType.String)
                {
                    currentKey = iterator.stringValue;
                    continue;
                }

                if (currentKey == platformName &&
                    iterator.name == "second" &&
                    iterator.propertyType == SerializedPropertyType.Integer)
                {
                    return iterator.intValue;
                }
            }

            Assert.Fail($"Missing platform quality default for {platformName}.");
            return -1;
        }

        private static void AssertBool(SerializedObject serialized, string propertyName, bool expected)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing {propertyName}.");
            Assert.That(property.boolValue, Is.EqualTo(expected), propertyName);
        }

        private static void AssertInt(SerializedObject serialized, string propertyName, int expected)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing {propertyName}.");
            Assert.That(property.intValue, Is.EqualTo(expected), propertyName);
        }

        private static void AssertFloat(SerializedObject serialized, string propertyName, float expected)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing {propertyName}.");
            Assert.That(property.floatValue, Is.EqualTo(expected).Within(0.0001f), propertyName);
        }
    }
}
