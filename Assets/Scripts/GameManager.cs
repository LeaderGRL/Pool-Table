using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    PlayerOneTurn,
    PlayerTwoTurn,
    PlayerOneWin,
    PlayerTwoWin,
    spectate,
    play,
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static event System.Action<GameState> OnGameStateChanged;
    public GameState state;
    
    private GameState currentPlayerTurn;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        updateGameState(GameState.PlayerOneTurn);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateGameState(GameState newState)
    {
        state = newState;

        switch (state)
        {
            case GameState.PlayerOneTurn:
                HandlePlayerOneTurn();
                break;
            case GameState.PlayerTwoTurn:
                HandlePlayerTwoTurn();
                break;
            case GameState.PlayerOneWin:
                Debug.Log("Player One Wins!");
                break;
            case GameState.PlayerTwoWin:
                Debug.Log("Player Two Wins!");
                break;
            case GameState.spectate:
                HandleSpectate();
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleSpectate()
    {
        Debug.Log("Spectating");
    }

    private void HandlePlayerOneTurn()
    {
        Debug.Log("Player One's Turn");

        currentPlayerTurn = GameState.PlayerOneTurn;
    }

    private void HandlePlayerTwoTurn()
    {
        Debug.Log("Player Two's Turn");

        currentPlayerTurn = GameState.PlayerTwoTurn;
    }

    public GameState getCurrentPlayerTurn()
    {
        return currentPlayerTurn;
    }
}
