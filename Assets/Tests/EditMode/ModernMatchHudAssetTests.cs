using System.IO;
using NUnit.Framework;
using PoolTable.Presentation.UI;
using UnityEditor;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class ModernMatchHudAssetTests
    {
        private const string HudRoot = "Assets/UI/Resources/MatchHud";

        [Test]
        public void PlayerPanels_AreExactHorizontalMirrors()
        {
            var left = LoadPng($"{HudRoot}/player_panel_left.png");
            var right = LoadPng($"{HudRoot}/player_panel_right.png");

            try
            {
                Assert.That(right.width, Is.EqualTo(left.width));
                Assert.That(right.height, Is.EqualTo(left.height));

                var leftPixels = left.GetPixels32();
                var rightPixels = right.GetPixels32();

                for (var y = 0; y < left.height; y++)
                {
                    var rowOffset = y * left.width;
                    for (var x = 0; x < left.width; x++)
                    {
                        Assert.That(
                            rightPixels[rowOffset + (left.width - 1 - x)],
                            Is.EqualTo(leftPixels[rowOffset + x]),
                            $"Player panels differ at ({x}, {y}).");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
            }
        }

        [Test]
        public void HudSprites_AreImportedForUnityUi()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"{HudRoot}/player_panel_left.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"{HudRoot}/player_panel_right.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"{HudRoot}/round_panel.png"), Is.Not.Null);

            for (var number = 1; number <= 15; number++)
            {
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{HudRoot}/Balls/{number}.png"),
                    Is.Not.Null,
                    $"Ball {number} HUD sprite is not imported as a Sprite.");
            }
        }

        [Test]
        public void RoundPresentation_DefaultsToOneAndClampsInvalidValues()
        {
            var gameObject = new GameObject("ModernMatchHudRoundTest");

            try
            {
                var hud = gameObject.AddComponent<ModernMatchHudController>();
                Assert.That(hud.RoundNumber, Is.EqualTo(ModernMatchHudController.DefaultRoundNumber));

                hud.SetRoundNumber(4);
                Assert.That(hud.RoundNumber, Is.EqualTo(4));

                hud.SetRoundNumber(0);
                Assert.That(hud.RoundNumber, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static Texture2D LoadPng(string assetPath)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(absolutePath)), Is.True, $"Failed to load {assetPath}.");
            return texture;
        }
    }
}
