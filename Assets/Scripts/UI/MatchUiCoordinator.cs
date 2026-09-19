using PoolTable.Presentation.UI;
using UnityEngine;

public sealed class MatchUiCoordinator : MonoBehaviour
{
    [SerializeField] private MatchHudView view;
    [SerializeField] private PlayersStateManagement gameplayStateMachine;

    private MatchUiSession session;

    private void Awake()
    {
        if (view == null)
        {
            view = GetComponent<MatchHudView>();
        }

        if (gameplayStateMachine == null)
        {
            gameplayStateMachine = FindAnyObjectByType<PlayersStateManagement>(FindObjectsInactive.Include);
        }

        if (view == null || gameplayStateMachine == null)
        {
            throw new System.InvalidOperationException("Match UI requires the HUD view and gameplay state machine.");
        }

        SetGameplayEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnLegacyGameStateChanged;
        GameManager.OnOpeningBreakResolved += OnOpeningBreakResolved;
    }

    private void Start()
    {
        view.StartRequested += BeginMatch;
    }

    private void OnDisable()
    {
        view.StartRequested -= BeginMatch;
        GameManager.OnGameStateChanged -= OnLegacyGameStateChanged;
        GameManager.OnOpeningBreakResolved -= OnOpeningBreakResolved;
    }

    private void BeginMatch(string requestedPlayerOneName, string requestedPlayerTwoName)
    {
        if (session != null)
        {
            return;
        }

        if (GameManager.instance == null)
        {
            throw new System.InvalidOperationException("Match UI cannot start before GameManager is initialized.");
        }

        session = MatchUiSession.Start(requestedPlayerOneName, requestedPlayerTwoName, Random.Range(0, 2));
        view.Render(session.Model);

        GameManager.instance.StartMatch(session.StartingPlayerIndex == 0
            ? GameState.PlayerOneTurn
            : GameState.PlayerTwoTurn);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetGameplayEnabled(true);
    }

    private void OnOpeningBreakResolved()
    {
        if (session == null)
        {
            return;
        }

        session.ResolveOpeningBreak(Time.frameCount);
        view.Render(session.Model);
    }

    private void OnLegacyGameStateChanged(GameState state)
    {
        if (session == null || !TryMapPlayerIndex(state, out var playerIndex))
        {
            return;
        }

        session.ApplyActivePlayer(playerIndex, Time.frameCount);
        view.Render(session.Model);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        gameplayStateMachine.SetMatchStarted(enabled);
        gameplayStateMachine.SetAimingEnabled(enabled);
        gameplayStateMachine.SetSpinControlEnabled(enabled);
        gameplayStateMachine.SetShotPowerEnabled(false);
        gameplayStateMachine.SetShotCameraEnabled(false);
        gameplayStateMachine.SetSpectateCameraEnabled(false);
    }

    private static bool TryMapPlayerIndex(GameState state, out int playerIndex)
    {
        switch (state)
        {
            case GameState.PlayerOneTurn:
                playerIndex = 0;
                return true;
            case GameState.PlayerTwoTurn:
                playerIndex = 1;
                return true;
            default:
                playerIndex = -1;
                return false;
        }
    }
}
