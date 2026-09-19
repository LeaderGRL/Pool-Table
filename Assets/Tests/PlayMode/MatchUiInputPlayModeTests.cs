using System.Collections;
using NUnit.Framework;
using PoolTable.Presentation.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PoolTable.Tests.PlayMode
{
    [Category("Functional")]
    [Category("Input")]
    public sealed class MatchUiInputPlayModeTests
    {
        private Keyboard keyboard;
        private Gamepad gamepad;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;

        [SetUp]
        public void SetUp()
        {
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [TearDown]
        public void TearDown()
        {
            RemoveDeviceIfAdded(keyboard);
            RemoveDeviceIfAdded(gamepad);
            keyboard = null;
            gamepad = null;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
        }

        [UnityTest]
        public IEnumerator MatchSetup_KeyboardSubmitMovesFocusAndStartsMatch()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadMatchSetup();

            var view = Object.FindAnyObjectByType<MatchHudView>();
            Assert.That(view, Is.Not.Null);

            var playerOneInput = view.RootVisualElement.Q<TextField>("player-one-input");
            var playerTwoInput = view.RootVisualElement.Q<TextField>("player-two-input");
            var startButton = view.RootVisualElement.Q<Button>("start-button");
            AssertFocusContract(playerOneInput, playerTwoInput, startButton);

            playerOneInput.Focus();
            yield return null;
            AssertFocusWithin(playerOneInput);

            QueueKeyboardEnter(isPressed: true);
            yield return null;
            Assert.That(keyboard.enabled, Is.True, "The synthetic keyboard must remain enabled in batchmode.");
            Assert.That(keyboard.enterKey.isPressed, Is.True, "The queued Enter press must reach the synthetic keyboard state.");
            QueueKeyboardEnter(isPressed: false);
            yield return null;
            AssertFocusWithin(playerTwoInput);

            QueueKeyboardEnter(isPressed: true);
            yield return null;
            QueueKeyboardEnter(isPressed: false);
            yield return null;
            AssertFocusWithin(startButton);

            QueueKeyboardEnter(isPressed: true);
            yield return null;
            QueueKeyboardEnter(isPressed: false);
            yield return null;

            Assert.That(view.CurrentModel.Screen, Is.EqualTo(MatchPresentationScreen.Match));
        }

        [UnityTest]
        public IEnumerator MatchSetup_GamepadSubmitMovesFocusAndStartsMatch()
        {
            gamepad = InputSystem.AddDevice<Gamepad>();
            yield return LoadMatchSetup();

            var view = Object.FindAnyObjectByType<MatchHudView>();
            Assert.That(view, Is.Not.Null);

            var playerOneInput = view.RootVisualElement.Q<TextField>("player-one-input");
            var playerTwoInput = view.RootVisualElement.Q<TextField>("player-two-input");
            var startButton = view.RootVisualElement.Q<Button>("start-button");
            AssertFocusContract(playerOneInput, playerTwoInput, startButton);

            playerOneInput.Focus();
            yield return null;
            AssertFocusWithin(playerOneInput);

            QueueGamepadSouth(isPressed: true);
            yield return null;
            QueueGamepadSouth(isPressed: false);
            yield return null;
            AssertFocusWithin(playerTwoInput);

            QueueGamepadSouth(isPressed: true);
            yield return null;
            QueueGamepadSouth(isPressed: false);
            yield return null;
            AssertFocusWithin(startButton);

            QueueGamepadSouth(isPressed: true);
            yield return null;
            QueueGamepadSouth(isPressed: false);
            yield return null;

            Assert.That(view.CurrentModel.Screen, Is.EqualTo(MatchPresentationScreen.Match));
        }

        private static IEnumerator LoadMatchSetup()
        {
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

        private void QueueKeyboardEnter(bool isPressed)
        {
            var state = isPressed ? new KeyboardState(Key.Enter) : new KeyboardState();
            InputSystem.QueueStateEvent(keyboard, state);
        }

        private void QueueGamepadSouth(bool isPressed)
        {
            var state = isPressed
                ? new GamepadState().WithButton(GamepadButton.South)
                : new GamepadState();
            InputSystem.QueueStateEvent(gamepad, state);
        }

        private static void RemoveDeviceIfAdded(InputDevice device)
        {
            if (device != null && device.added)
            {
                InputSystem.RemoveDevice(device);
            }
        }

        private static void AssertFocusContract(TextField playerOneInput, TextField playerTwoInput, Button startButton)
        {
            Assert.That(playerOneInput, Is.Not.Null);
            Assert.That(playerTwoInput, Is.Not.Null);
            Assert.That(startButton, Is.Not.Null);
            Assert.That(playerOneInput.focusable, Is.True);
            Assert.That(playerTwoInput.focusable, Is.True);
            Assert.That(startButton.focusable, Is.True);
            Assert.That(playerOneInput.tabIndex, Is.LessThan(playerTwoInput.tabIndex));
            Assert.That(playerTwoInput.tabIndex, Is.LessThan(startButton.tabIndex));
        }

        private static void AssertFocusWithin(VisualElement expectedRoot)
        {
            var focused = expectedRoot.panel?.focusController?.focusedElement as VisualElement;
            var focusedDescription = focused == null
                ? "<null>"
                : $"{focused.GetType().Name} name='{focused.name}' parent='{focused.parent?.name}'";
            while (focused != null)
            {
                if (focused == expectedRoot)
                {
                    return;
                }

                focused = focused.parent;
            }

            Assert.Fail($"Expected focus to remain within '{expectedRoot.name}', but focused element was {focusedDescription}.");
        }
    }
}
