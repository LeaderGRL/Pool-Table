using System;
using System.Collections.Generic;
using System.Reflection;
using PoolTable.Core.Balls;
using PoolTable.Gameplay.Balls;
using PoolTable.Gameplay.Pockets;
using UnityEngine;
using UnityEngine.UI;

namespace PoolTable.Presentation.UI
{
    public sealed class ModernMatchHudController : MonoBehaviour
    {
        internal const int DefaultRoundNumber = 1;
        internal const int BallSlotsPerPlayer = 7;

        private static readonly Color ActivePanelColor = Color.white;
        private static readonly Color InactivePanelColor = new Color(0.55f, 0.55f, 0.55f, 0.78f);
        private static readonly Color ActiveTextColor = new Color(0.98f, 0.93f, 0.80f, 1f);
        private static readonly Color InactiveTextColor = new Color(0.58f, 0.58f, 0.58f, 1f);
        private static readonly Color GoldTextColor = new Color(0.93f, 0.67f, 0.25f, 1f);

        private readonly BallIconSlot[] playerOneSlots = new BallIconSlot[BallSlotsPerPlayer];
        private readonly BallIconSlot[] playerTwoSlots = new BallIconSlot[BallSlotsPerPlayer];
        private readonly Dictionary<int, Sprite> ballSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, BallPocketCapture> capturesByBallNumber = new Dictionary<int, BallPocketCapture>();

        private Transform ballsRoot;
        private LegacyMatchStateReader legacyStateReader;
        private GameObject hudCanvasObject;
        private Image playerOnePanel;
        private Image playerTwoPanel;
        private Text playerOneName;
        private Text playerTwoName;
        private Text playerOneTurn;
        private Text playerTwoTurn;
        private Text roundNumberText;
        private BallGroup playerOneGroup = BallGroup.None;
        private BallGroup playerTwoGroup = BallGroup.None;
        private int activePlayer = 1;
        private int roundNumber = DefaultRoundNumber;
        private bool initialized;

        public int RoundNumber => roundNumber;

        internal GameObject HudCanvasObject => hudCanvasObject;
        internal Image PlayerOnePanel => playerOnePanel;
        internal Image PlayerTwoPanel => playerTwoPanel;
        internal Text PlayerOneTurnText => playerOneTurn;
        internal Text PlayerTwoTurnText => playerTwoTurn;
        internal BallGroup PlayerOneGroup => playerOneGroup;
        internal BallGroup PlayerTwoGroup => playerTwoGroup;
        internal int ActivePlayer => activePlayer;

        internal int[] GetDisplayedBallNumbers(int playerNumber)
        {
            var slots = playerNumber == 2 ? playerTwoSlots : playerOneSlots;
            var numbers = new int[slots.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                numbers[index] = slots[index].BallNumber;
            }

            return numbers;
        }

        internal bool IsBallIconVisible(int playerNumber, int ballNumber)
        {
            var slots = playerNumber == 2 ? playerTwoSlots : playerOneSlots;
            foreach (var slot in slots)
            {
                if (slot.BallNumber == ballNumber)
                {
                    return slot.IsVisible;
                }
            }

            return false;
        }

        public void Initialize(Transform sourceBallsRoot)
        {
            if (initialized)
            {
                return;
            }

            if (sourceBallsRoot == null)
            {
                throw new ArgumentNullException(nameof(sourceBallsRoot));
            }

            ballsRoot = sourceBallsRoot;
            legacyStateReader = new LegacyMatchStateReader();
            CacheBallState();
            LoadSprites();
            BuildHud();
            DisableLegacyHud();
            SetRoundNumber(DefaultRoundNumber);
            SetGroups(BallGroup.Solids, BallGroup.Stripes);
            SetActivePlayer(1);
            initialized = true;
        }

        public void SetRoundNumber(int value)
        {
            roundNumber = Mathf.Max(1, value);
            if (roundNumberText != null)
            {
                roundNumberText.text = roundNumber.ToString();
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (legacyStateReader.TryRead(out var currentPlayer, out var playerOneBallGroup, out var playerTwoBallGroup))
            {
                if (playerOneBallGroup != playerOneGroup || playerTwoBallGroup != playerTwoGroup)
                {
                    SetGroups(playerOneBallGroup, playerTwoBallGroup);
                }

                if (currentPlayer != activePlayer)
                {
                    SetActivePlayer(currentPlayer);
                }
            }

            RefreshBallAvailability(playerOneSlots);
            RefreshBallAvailability(playerTwoSlots);
        }

        private void OnDestroy()
        {
            if (hudCanvasObject != null)
            {
                Destroy(hudCanvasObject);
            }
        }

        internal void SetGroups(BallGroup firstPlayerGroup, BallGroup secondPlayerGroup)
        {
            if (firstPlayerGroup == BallGroup.None || secondPlayerGroup == BallGroup.None)
            {
                return;
            }

            playerOneGroup = firstPlayerGroup;
            playerTwoGroup = secondPlayerGroup;
            BindGroup(playerOneSlots, firstPlayerGroup);
            BindGroup(playerTwoSlots, secondPlayerGroup);
        }

        internal void SetActivePlayer(int playerNumber)
        {
            activePlayer = playerNumber == 2 ? 2 : 1;
            var playerOneIsActive = activePlayer == 1;

            playerOnePanel.color = playerOneIsActive ? ActivePanelColor : InactivePanelColor;
            playerTwoPanel.color = playerOneIsActive ? InactivePanelColor : ActivePanelColor;
            playerOneName.color = playerOneIsActive ? ActiveTextColor : InactiveTextColor;
            playerTwoName.color = playerOneIsActive ? InactiveTextColor : ActiveTextColor;
            playerOneTurn.text = playerOneIsActive ? "À JOUER" : string.Empty;
            playerTwoTurn.text = playerOneIsActive ? string.Empty : "À JOUER";
        }

        private void CacheBallState()
        {
            capturesByBallNumber.Clear();
            var identities = ballsRoot.GetComponentsInChildren<BallIdentity>(true);
            foreach (var identity in identities)
            {
                if (identity.IsCueBall || identity.IsEightBall)
                {
                    continue;
                }

                var capture = identity.GetComponent<BallPocketCapture>();
                if (capture != null)
                {
                    capturesByBallNumber[identity.Id.Number] = capture;
                }
            }
        }

        private void LoadSprites()
        {
            var leftPanelSprite = Resources.Load<Sprite>("MatchHud/player_panel_left");
            var rightPanelSprite = Resources.Load<Sprite>("MatchHud/player_panel_right");
            var roundPanelSprite = Resources.Load<Sprite>("MatchHud/round_panel");

            if (leftPanelSprite == null || rightPanelSprite == null || roundPanelSprite == null)
            {
                throw new InvalidOperationException("Modern match HUD panel sprites are missing from Resources/MatchHud.");
            }

            PanelSprites = new HudPanelSprites(leftPanelSprite, rightPanelSprite, roundPanelSprite);
            ballSprites.Clear();
            for (var number = 1; number <= 15; number++)
            {
                if (number == BallId.EightBallNumber)
                {
                    continue;
                }

                var sprite = Resources.Load<Sprite>($"MatchHud/Balls/{number}");
                if (sprite == null)
                {
                    throw new InvalidOperationException($"Missing HUD ball sprite for ball {number}.");
                }

                ballSprites[number] = sprite;
            }
        }

        private HudPanelSprites PanelSprites { get; set; }

        private void BuildHud()
        {
            hudCanvasObject = new GameObject("ModernMatchHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hudCanvasObject.transform.SetParent(transform, false);
            hudCanvasObject.layer = 5;

            var canvas = hudCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = hudCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = hudCanvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            playerOnePanel = CreateImage("PlayerOnePanel", root, PanelSprites.LeftPlayerPanel);
            SetAnchoredRect(playerOnePanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(760f, 180f));

            playerTwoPanel = CreateImage("PlayerTwoPanel", root, PanelSprites.RightPlayerPanel);
            SetAnchoredRect(playerTwoPanel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -18f), new Vector2(760f, 180f));

            var roundPanel = CreateImage("RoundPanel", root, PanelSprites.RoundPanel);
            SetAnchoredRect(roundPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(420f, 145f));

            BuildPlayerPanel(playerOnePanel.rectTransform, true, out playerOneName, out playerOneTurn, playerOneSlots);
            BuildPlayerPanel(playerTwoPanel.rectTransform, false, out playerTwoName, out playerTwoTurn, playerTwoSlots);
            BuildRoundPanel(roundPanel.rectTransform);
        }

        private void BuildPlayerPanel(RectTransform panel, bool isLeft, out Text nameText, out Text turnText, BallIconSlot[] slots)
        {
            nameText = CreateText(isLeft ? "PlayerOneName" : "PlayerTwoName", panel, isLeft ? "JOUEUR 1" : "JOUEUR 2", 28, ActiveTextColor, TextAnchor.MiddleCenter);
            SetAnchoredRect(
                nameText.rectTransform,
                isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
                isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
                isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
                isLeft ? new Vector2(195f, -28f) : new Vector2(-195f, -28f),
                new Vector2(430f, 45f));

            var ballRow = new GameObject(isLeft ? "PlayerOneBalls" : "PlayerTwoBalls", typeof(RectTransform));
            ballRow.transform.SetParent(panel, false);
            var rowRect = ballRow.GetComponent<RectTransform>();
            SetAnchoredRect(
                rowRect,
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(205f, 45f) : new Vector2(-205f, 45f),
                new Vector2(460f, 52f));

            for (var index = 0; index < BallSlotsPerPlayer; index++)
            {
                var icon = CreateImage($"BallSlot{index + 1}", rowRect, null);
                icon.preserveAspect = true;
                var x = 29f + index * 63f;
                icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(x, 0f);
                icon.rectTransform.sizeDelta = new Vector2(50f, 50f);
                slots[index] = new BallIconSlot(icon);
            }

            turnText = CreateText(isLeft ? "PlayerOneTurn" : "PlayerTwoTurn", panel, string.Empty, 20, GoldTextColor, TextAnchor.MiddleCenter);
            SetAnchoredRect(
                turnText.rectTransform,
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                isLeft ? new Vector2(205f, 10f) : new Vector2(-205f, 10f),
                new Vector2(460f, 30f));
        }

        private void BuildRoundPanel(RectTransform panel)
        {
            var label = CreateText("RoundLabel", panel, "MANCHE", 20, ActiveTextColor, TextAnchor.MiddleCenter);
            SetAnchoredRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(220f, 32f));

            roundNumberText = CreateText("RoundNumber", panel, DefaultRoundNumber.ToString(), 44, Color.white, TextAnchor.MiddleCenter);
            SetAnchoredRect(roundNumberText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 65f));
        }

        private void BindGroup(BallIconSlot[] slots, BallGroup group)
        {
            var firstNumber = group == BallGroup.Solids ? 1 : 9;
            for (var index = 0; index < slots.Length; index++)
            {
                var number = firstNumber + index;
                var capture = capturesByBallNumber.TryGetValue(number, out var value) ? value : null;
                slots[index].Bind(number, ballSprites[number], capture);
            }

            RefreshBallAvailability(slots);
        }

        private static void RefreshBallAvailability(BallIconSlot[] slots)
        {
            foreach (var slot in slots)
            {
                slot.RefreshAvailability();
            }
        }

        private void DisableLegacyHud()
        {
            var legacyCanvas = GameObject.Find("/UI/Canvas");
            if (legacyCanvas == null)
            {
                return;
            }

            var legacyPanelTransform = legacyCanvas.transform.Find("Panel");
            if (legacyPanelTransform == null)
            {
                return;
            }

            var legacyPanel = legacyPanelTransform.gameObject;
            legacyPanel.SetActive(true);

            var canvasGroup = legacyPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = legacyPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);

            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;

            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private readonly struct HudPanelSprites
        {
            public HudPanelSprites(Sprite leftPlayerPanel, Sprite rightPlayerPanel, Sprite roundPanel)
            {
                LeftPlayerPanel = leftPlayerPanel;
                RightPlayerPanel = rightPlayerPanel;
                RoundPanel = roundPanel;
            }

            public Sprite LeftPlayerPanel { get; }
            public Sprite RightPlayerPanel { get; }
            public Sprite RoundPanel { get; }
        }

        private sealed class BallIconSlot
        {
            private readonly Image image;
            private BallPocketCapture capture;

            public BallIconSlot(Image image)
            {
                this.image = image;
            }

            public void Bind(int ballNumber, Sprite sprite, BallPocketCapture ballCapture)
            {
                BallNumber = ballNumber;
                capture = ballCapture;
                image.sprite = sprite;
                RefreshAvailability();
            }

            public int BallNumber { get; private set; }

            public bool IsVisible => image.enabled;

            public void RefreshAvailability()
            {
                image.enabled = capture == null || !capture.IsCaptured;
            }
        }

        private sealed class LegacyMatchStateReader
        {
            private readonly FieldInfo instanceField;
            private readonly MethodInfo getCurrentPlayerTurn;
            private readonly MethodInfo getPlayerOneBallType;
            private readonly MethodInfo getPlayerTwoBallType;

            public LegacyMatchStateReader()
            {
                var gameManagerType = FindType("GameManager");
                if (gameManagerType == null)
                {
                    return;
                }

                instanceField = gameManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                getCurrentPlayerTurn = gameManagerType.GetMethod("getCurrentPlayerTurn", BindingFlags.Public | BindingFlags.Instance);
                getPlayerOneBallType = gameManagerType.GetMethod("getPlayer1BallType", BindingFlags.Public | BindingFlags.Instance);
                getPlayerTwoBallType = gameManagerType.GetMethod("getPlayer2BallType", BindingFlags.Public | BindingFlags.Instance);
            }

            public bool TryRead(out int currentPlayer, out BallGroup playerOneBallGroup, out BallGroup playerTwoBallGroup)
            {
                currentPlayer = 1;
                playerOneBallGroup = BallGroup.None;
                playerTwoBallGroup = BallGroup.None;

                if (instanceField == null || getCurrentPlayerTurn == null || getPlayerOneBallType == null || getPlayerTwoBallType == null)
                {
                    return false;
                }

                var instance = instanceField.GetValue(null);
                if (instance == null)
                {
                    return false;
                }

                try
                {
                    var currentTurnName = getCurrentPlayerTurn.Invoke(instance, null)?.ToString();
                    var playerOneTypeName = getPlayerOneBallType.Invoke(instance, null)?.ToString();
                    var playerTwoTypeName = getPlayerTwoBallType.Invoke(instance, null)?.ToString();

                    currentPlayer = string.Equals(currentTurnName, "PlayerTwoTurn", StringComparison.Ordinal) ? 2 : 1;
                    playerOneBallGroup = ParseGroup(playerOneTypeName);
                    playerTwoBallGroup = ParseGroup(playerTwoTypeName);
                    return playerOneBallGroup != BallGroup.None && playerTwoBallGroup != BallGroup.None;
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
            }

            private static BallGroup ParseGroup(string value)
            {
                if (string.Equals(value, "filled", StringComparison.OrdinalIgnoreCase))
                {
                    return BallGroup.Solids;
                }

                if (string.Equals(value, "striped", StringComparison.OrdinalIgnoreCase))
                {
                    return BallGroup.Stripes;
                }

                return BallGroup.None;
            }

            private static Type FindType(string typeName)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(typeName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }

                return null;
            }
        }
    }
}
