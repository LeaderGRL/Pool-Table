using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

namespace PoolTable.Tests.EditMode
{
    public sealed class UniversalRenderPipelineConfigurationTests
    {
        private const string PipelinePath = "Assets/Settings/Rendering/PoolTableUniversalRenderPipeline.asset";
        private const string RendererPath = "Assets/Settings/Rendering/PoolTableUniversalRenderer.asset";

        [Test]
        public void GraphicsSettings_UsesProjectUniversalRenderPipelineAsset()
        {
            var pipelineAsset = GraphicsSettings.defaultRenderPipeline;

            Assert.That(pipelineAsset, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pipelineAsset), Is.EqualTo(PipelinePath));
            Assert.That(
                pipelineAsset.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"));
        }

        [Test]
        public void UniversalRenderPipelineAsset_UsesProjectUniversalRenderer()
        {
            var pipelineAsset = AssetDatabase.LoadMainAssetAtPath(PipelinePath);
            Assert.That(pipelineAsset, Is.Not.Null);

            var serializedPipeline = new SerializedObject(pipelineAsset);
            var rendererDataList = serializedPipeline.FindProperty("m_RendererDataList");

            Assert.That(rendererDataList, Is.Not.Null);
            Assert.That(rendererDataList.arraySize, Is.GreaterThan(0));

            var rendererData = rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.That(rendererData, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(rendererData), Is.EqualTo(RendererPath));
            Assert.That(
                rendererData.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRendererData"));
        }
    }
}
