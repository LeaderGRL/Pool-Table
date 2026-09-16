using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class PoolTablePbrMaterialTests
    {
        private const string BaseColorPath = "Assets/3D/source/pool table low_POOL TABLE_BaseColor.png";
        private const string MetallicPath = "Assets/3D/source/pool table low_POOL TABLE_Metallic.png";
        private const string NormalPath = "Assets/3D/source/pool table low_POOL TABLE_Normal.png";
        private const string RoughnessPath = "Assets/3D/source/pool table low_POOL TABLE_Roughness.png";
        private const string HeightPath = "Assets/3D/source/pool table low_POOL TABLE_Height.png";
        private const string PackedMetallicSmoothnessPath =
            "Assets/3D/source/pool table low_POOL TABLE_MetallicSmoothness.png";
        private const string MaterialPath =
            "Assets/3D/source/Materials/pool table low_POOL TABLE_BaseColor.mat";

        [Test]
        public void PoolTableMaterial_UsesIntentionalUrpPbrMaps()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")), Is.EqualTo(BaseColorPath));
            Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")), Is.EqualTo(NormalPath));
            Assert.That(
                AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap")),
                Is.EqualTo(PackedMetallicSmoothnessPath));
            Assert.That(material.GetTexture("_ParallaxMap"), Is.Null);
            Assert.That(material.GetFloat("_Smoothness"), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(material.GetFloat("_SmoothnessTextureChannel"), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(material.IsKeywordEnabled("_NORMALMAP"), Is.True);
            Assert.That(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
            Assert.That(material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"), Is.False);
            Assert.That(material.IsKeywordEnabled("_PARALLAXMAP"), Is.False);
        }

        [Test]
        public void PoolTablePbrDataMaps_UseLinearImportSettings()
        {
            AssertDataTexture(MetallicPath);
            AssertDataTexture(RoughnessPath);
            AssertDataTexture(HeightPath);
            AssertDataTexture(PackedMetallicSmoothnessPath);

            var normalImporter = GetTextureImporter(NormalPath);
            Assert.That(normalImporter.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            Assert.That(normalImporter.sRGBTexture, Is.False);
        }

        [Test]
        public void PackedMetallicSmoothnessMap_PreservesMetallicAndInvertsRoughness()
        {
            var metallic = LoadLinearPng(MetallicPath);
            var roughness = LoadLinearPng(RoughnessPath);
            var packed = LoadLinearPng(PackedMetallicSmoothnessPath);

            try
            {
                Assert.That(packed.width, Is.EqualTo(metallic.width));
                Assert.That(packed.height, Is.EqualTo(metallic.height));
                Assert.That(roughness.width, Is.EqualTo(metallic.width));
                Assert.That(roughness.height, Is.EqualTo(metallic.height));

                var metallicPixels = metallic.GetPixels32();
                var roughnessPixels = roughness.GetPixels32();
                var packedPixels = packed.GetPixels32();
                var stride = Math.Max(1, packedPixels.Length / 1024);

                for (var index = 0; index < packedPixels.Length; index += stride)
                {
                    Assert.That(packedPixels[index].r, Is.EqualTo(metallicPixels[index].r));
                    Assert.That(packedPixels[index].g, Is.EqualTo(metallicPixels[index].g));
                    Assert.That(packedPixels[index].b, Is.EqualTo(metallicPixels[index].b));
                    Assert.That(
                        packedPixels[index].a,
                        Is.EqualTo((byte)(byte.MaxValue - roughnessPixels[index].r)));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                UnityEngine.Object.DestroyImmediate(packed);
            }
        }

        private static void AssertDataTexture(string assetPath)
        {
            var importer = GetTextureImporter(assetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(importer.sRGBTexture, Is.False, $"Data texture '{assetPath}' must be imported in linear space.");
        }

        private static TextureImporter GetTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, $"Unable to load texture importer for '{assetPath}'.");
            return importer;
        }

        private static Texture2D LoadLinearPng(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);

            var absolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);

            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                Assert.Fail($"Unable to decode texture '{assetPath}'.");
            }

            return texture;
        }
    }
}
