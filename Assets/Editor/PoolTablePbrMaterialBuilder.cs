using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PoolTable.Editor
{
    public static class PoolTablePbrMaterialBuilder
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

        [MenuItem("Pool Table/Rendering/Rebuild Pool Table PBR Material")]
        public static void Rebuild()
        {
            GenerateMetallicSmoothnessMap();
            ConfigureTextureImporters();
            ConfigureMaterial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateMetallicSmoothnessMap()
        {
            var metallicTexture = LoadLinearPng(MetallicPath);
            var roughnessTexture = LoadLinearPng(RoughnessPath);

            try
            {
                if (metallicTexture.width != roughnessTexture.width || metallicTexture.height != roughnessTexture.height)
                {
                    throw new InvalidOperationException("Metallic and roughness maps must have matching dimensions.");
                }

                var metallicPixels = metallicTexture.GetPixels32();
                var roughnessPixels = roughnessTexture.GetPixels32();
                var packedPixels = new Color32[metallicPixels.Length];

                for (var index = 0; index < packedPixels.Length; index++)
                {
                    var metallic = metallicPixels[index];
                    var roughness = roughnessPixels[index].r;
                    packedPixels[index] = new Color32(
                        metallic.r,
                        metallic.g,
                        metallic.b,
                        (byte)(byte.MaxValue - roughness));
                }

                var packedTexture = new Texture2D(
                    metallicTexture.width,
                    metallicTexture.height,
                    TextureFormat.RGBA32,
                    false,
                    true);

                try
                {
                    packedTexture.SetPixels32(packedPixels);
                    packedTexture.Apply(false, false);
                    File.WriteAllBytes(ToAbsolutePath(PackedMetallicSmoothnessPath), packedTexture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(packedTexture);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallicTexture);
                UnityEngine.Object.DestroyImmediate(roughnessTexture);
            }

            AssetDatabase.ImportAsset(PackedMetallicSmoothnessPath, ImportAssetOptions.ForceUpdate);
        }

        private static Texture2D LoadLinearPng(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var bytes = File.ReadAllBytes(ToAbsolutePath(assetPath));

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException($"Unable to decode texture '{assetPath}'.");
            }

            return texture;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve the Unity project root.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureDataTexture(MetallicPath);
            ConfigureDataTexture(RoughnessPath);
            ConfigureDataTexture(HeightPath);

            var normalImporter = GetTextureImporter(NormalPath);
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.sRGBTexture = false;
            normalImporter.mipmapEnabled = true;
            normalImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            normalImporter.SaveAndReimport();

            var packedImporter = GetTextureImporter(PackedMetallicSmoothnessPath);
            packedImporter.textureType = TextureImporterType.Default;
            packedImporter.sRGBTexture = false;
            packedImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            packedImporter.alphaIsTransparency = false;
            packedImporter.mipmapEnabled = true;
            packedImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            packedImporter.SaveAndReimport();
        }

        private static void ConfigureDataTexture(string assetPath)
        {
            var importer = GetTextureImporter(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static TextureImporter GetTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to load texture importer for '{assetPath}'.");
            }

            return importer;
        }

        private static void ConfigureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Unable to load pool-table material '{MaterialPath}'.");
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader is unavailable.");
            }

            var baseColor = LoadTexture(BaseColorPath);
            var normal = LoadTexture(NormalPath);
            var metallicSmoothness = LoadTexture(PackedMetallicSmoothnessPath);

            material.shader = shader;
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetTexture("_ParallaxMap", null);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_WorkflowMode", 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.DisableKeyword("_SPECULAR_SETUP");
            material.DisableKeyword("_PARALLAXMAP");

            EditorUtility.SetDirty(material);
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Unable to load texture '{assetPath}'.");
            }

            return texture;
        }
    }
}
