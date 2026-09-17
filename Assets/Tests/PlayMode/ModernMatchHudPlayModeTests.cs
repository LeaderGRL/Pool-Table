using System.Collections;
using System.Linq;
using NUnit.Framework;
using PoolTable.Core.Balls;
using PoolTable.Core.Shots;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Pockets;
using PoolTable.Presentation;
using PoolTable.Presentation.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PoolTable.Tests.PlayMode
{
    [Category("SceneSmoke")]
    public sealed class ModernMatchHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator PoolTableScene_ComposesModernHudAndDisablesLegacyPanel()
        {
            yield return LoadPoolTableScene();

            var compositionRoot = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>();
            Assert.That(compositionRoot, Is.Not.Null);

            var hud = compositionRoot.ModernMatchHudController;
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.HudCanvasObject, Is.Not.Null);
            Assert.That(hud.HudCanvasObject.activeInHierarchy, Is.True);
            Assert.That(hud.RoundNumber, Is.EqualTo(ModernMatchHudController.DefaultRoundNumber));

            var scaler = hud.HudCanvasObject.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            var legacyRoot = GameObject.Find("UI");
            Assert.That(legacyRoot, Is.Not.Null);
            var legacyPanel = legacyRoot.transform.Find("Canvas/Panel");
            Assert.That(legacyPanel, Is.Not.Null);
            Assert.That(legacyPanel.gameObject.activeSelf, Is.True, "Legacy UI state objects must remain active for the migration bridge.");
            var legacyCanvasGroup = legacyPanel.GetComponent<CanvasGroup>();
            Assert.That(legacyCanvasGroup, Is.Not.Null);
            Assert.That(legacyCanvasGroup.alpha, Is.Zero, "The old bottom HUD must stay visually hidden once the modern HUD is composed.");
            Assert.That(legacyCanvasGroup.blocksRaycasts, Is.False);
        }

        [UnityTest]
        public IEnumerator Hud_PresentsActivePlayerAndCorrectGroupNumbers()
        {
            yield return LoadPoolTableScene();

            var hud = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>().ModernMatchHudController;
            hud.SetGroups(BallGroup.Solids, BallGroup.Stripes);
            hud.SetActivePlayer(2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, hud.GetDisplayedBallNumbers(1));
            CollectionAssert.AreEqual(new[] { 9, 10, 11, 12, 13, 14, 15 }, hud.GetDisplayedBallNumbers(2));
            Assert.That(hud.PlayerOneGroup, Is.EqualTo(BallGroup.Solids));
            Assert.That(hud.PlayerTwoGroup, Is.EqualTo(BallGroup.Stripes));
            Assert.That(hud.ActivePlayer, Is.EqualTo(2));
            Assert.That(hud.PlayerOneTurnText.text, Is.Empty);
            Assert.That(hud.PlayerTwoTurnText.text, Is.EqualTo("À JOUER"));
            Assert.That(hud.PlayerTwoPanel.color.a, Is.GreaterThan(hud.PlayerOnePanel.color.a));
        }

        [UnityTest]
        public IEnumerator Hud_HidesAndRestoresCapturedBallFromTypedPocketState()
        {
            yield return LoadPoolTableScene();

            var compositionRoot = Object.FindFirstObjectByType<PoolTableSceneCompositionRoot>();
            var hud = compositionRoot.ModernMatchHudController;
            var solidsPlayer = hud.PlayerOneGroup == BallGroup.Solids ? 1 : 2;

            var ball = compositionRoot.BallsRoot
                .GetComponentsInChildren<BallIdentity>(true)
                .Single(identity => identity.Id.Number == 1);
            var capture = ball.GetComponent<BallPocketCapture>();
            Assert.That(capture, Is.Not.Null);
            Assert.That(hud.IsBallIconVisible(solidsPlayer, 1), Is.True);

            var originalPosition = ball.transform.position;
            Assert.That(capture.TryCapture(new PocketId(1), out _), Is.True);
            yield return null;

            Assert.That(hud.IsBallIconVisible(solidsPlayer, 1), Is.False, "A typed captured ball must disappear from the owning player's remaining-ball row.");

            capture.Restore(originalPosition);
            yield return null;
            Assert.That(hud.IsBallIconVisible(solidsPlayer, 1), Is.True);
        }

        private static IEnumerator LoadPoolTableScene()
        {
            Assert.That(SceneManager.sceneCountInBuildSettings, Is.GreaterThan(0));
            var loadOperation = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            yield return null;
        }
    }
}
