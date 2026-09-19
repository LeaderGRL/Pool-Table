using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PoolTable.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MatchHudView : MonoBehaviour
    {
        private const string PlayerOneFallback = "PLAYER 1";
        private const string PlayerTwoFallback = "PLAYER 2";

        [SerializeField] private UIDocument uiDocument;

        private VisualElement setupLayer;
        private VisualElement hudLayer;
        private VisualElement playerOnePanel;
        private VisualElement playerTwoPanel;
        private TextField playerOneInput;
        private TextField playerTwoInput;
        private Button startButton;
        private Label playerOneName;
        private Label playerTwoName;
        private Label turnValue;
        private bool callbacksRegistered;
        private bool sanitizingInput;

        public event Action<string, string> StartRequested;

        public MatchPresentationModel CurrentModel { get; private set; }

        public VisualElement RootVisualElement => uiDocument != null ? uiDocument.rootVisualElement : null;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null || uiDocument.visualTreeAsset == null)
            {
                throw new InvalidOperationException("Match HUD requires a UIDocument with a VisualTreeAsset.");
            }

            BindDocument();
            RegisterCallbacks();
        }

        private void Start()
        {
            InitializeForSetup();
            playerOneInput.schedule.Execute(() => playerOneInput.Focus());
        }

        private void OnEnable()
        {
            if (uiDocument != null && uiDocument.visualTreeAsset != null && !callbacksRegistered)
            {
                BindDocument();
                RegisterCallbacks();
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        public void InitializeForSetup()
        {
            playerOneInput.SetValueWithoutNotify(string.Empty);
            playerTwoInput.SetValueWithoutNotify(string.Empty);
            Render(MatchPresentationModel.CreateSetup());
        }

        public void Render(MatchPresentationModel model)
        {
            CurrentModel = model ?? throw new ArgumentNullException(nameof(model));

            var isSetup = model.Screen == MatchPresentationScreen.Setup;
            setupLayer.style.display = isSetup ? DisplayStyle.Flex : DisplayStyle.None;
            hudLayer.style.display = isSetup ? DisplayStyle.None : DisplayStyle.Flex;

            if (isSetup)
            {
                return;
            }

            playerOneName.text = model.PlayerOne.DisplayName;
            playerTwoName.text = model.PlayerTwo.DisplayName;
            turnValue.text = model.TourNumber.ToString();

            playerOnePanel.EnableInClassList("player-panel--active", model.PlayerOne.IsActive);
            playerTwoPanel.EnableInClassList("player-panel--active", model.PlayerTwo.IsActive);
            playerOnePanel.EnableInClassList("player-panel--inactive", !model.PlayerOne.IsActive);
            playerTwoPanel.EnableInClassList("player-panel--inactive", !model.PlayerTwo.IsActive);
        }

        public void SubmitSetup()
        {
            if (CurrentModel == null || CurrentModel.Screen != MatchPresentationScreen.Setup)
            {
                return;
            }

            var normalizedPlayerOne = MatchNameRules.NormalizeForMatch(playerOneInput.value, PlayerOneFallback);
            var normalizedPlayerTwo = MatchNameRules.NormalizeForMatch(playerTwoInput.value, PlayerTwoFallback);
            StartRequested?.Invoke(normalizedPlayerOne, normalizedPlayerTwo);
        }

        private void BindDocument()
        {
            var root = uiDocument.rootVisualElement;
            setupLayer = RequireElement<VisualElement>(root, "setup-layer");
            hudLayer = RequireElement<VisualElement>(root, "hud-layer");
            playerOnePanel = RequireElement<VisualElement>(root, "player-one-panel");
            playerTwoPanel = RequireElement<VisualElement>(root, "player-two-panel");
            playerOneInput = RequireElement<TextField>(root, "player-one-input");
            playerTwoInput = RequireElement<TextField>(root, "player-two-input");
            startButton = RequireElement<Button>(root, "start-button");
            playerOneName = RequireElement<Label>(root, "player-one-name");
            playerTwoName = RequireElement<Label>(root, "player-two-name");
            turnValue = RequireElement<Label>(root, "turn-value");

            playerOneInput.maxLength = MatchNameRules.MaxLength;
            playerTwoInput.maxLength = MatchNameRules.MaxLength;
            playerOneInput.tabIndex = 0;
            playerTwoInput.tabIndex = 1;
            startButton.tabIndex = 2;
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            playerOneInput.RegisterValueChangedCallback(OnPlayerOneValueChanged);
            playerTwoInput.RegisterValueChangedCallback(OnPlayerTwoValueChanged);
            playerOneInput.RegisterCallback<NavigationSubmitEvent>(OnPlayerOneSubmit, TrickleDown.TrickleDown);
            playerTwoInput.RegisterCallback<NavigationSubmitEvent>(OnPlayerTwoSubmit, TrickleDown.TrickleDown);
            startButton.clicked += SubmitSetup;
            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
            {
                return;
            }

            playerOneInput.UnregisterValueChangedCallback(OnPlayerOneValueChanged);
            playerTwoInput.UnregisterValueChangedCallback(OnPlayerTwoValueChanged);
            playerOneInput.UnregisterCallback<NavigationSubmitEvent>(OnPlayerOneSubmit, TrickleDown.TrickleDown);
            playerTwoInput.UnregisterCallback<NavigationSubmitEvent>(OnPlayerTwoSubmit, TrickleDown.TrickleDown);
            startButton.clicked -= SubmitSetup;
            callbacksRegistered = false;
        }

        private void OnPlayerOneValueChanged(ChangeEvent<string> evt)
        {
            SanitizeField(playerOneInput, evt.newValue);
        }

        private void OnPlayerTwoValueChanged(ChangeEvent<string> evt)
        {
            SanitizeField(playerTwoInput, evt.newValue);
        }

        private void SanitizeField(TextField field, string value)
        {
            if (sanitizingInput)
            {
                return;
            }

            var sanitized = MatchNameRules.SanitizeInput(value);
            if (sanitized == value)
            {
                return;
            }

            sanitizingInput = true;
            field.SetValueWithoutNotify(sanitized);
            sanitizingInput = false;
        }

        private void OnPlayerOneSubmit(NavigationSubmitEvent evt)
        {
            playerOneInput.panel?.focusController?.IgnoreEvent(evt);
            playerTwoInput.Focus();
            evt.StopPropagation();
        }

        private void OnPlayerTwoSubmit(NavigationSubmitEvent evt)
        {
            playerTwoInput.panel?.focusController?.IgnoreEvent(evt);
            startButton.Focus();
            evt.StopPropagation();
        }

        private static T RequireElement<T>(VisualElement root, string name)
            where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException($"Match HUD is missing required element '{name}'.");
            }

            return element;
        }
    }
}
