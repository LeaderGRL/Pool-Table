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

public enum ballType
{
    striped,
    filled,
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static event System.Action<GameState> OnGameStateChanged;
    public GameState state;
    public GameObject UI_PlayerTurn;
    public GameObject UI_PlayerBallType;
    public GameObject UI_Message;
    
    private GameState currentPlayerTurn;

    private Player player1;
    private Player player2;

    private int turnNumber = 1;

    //private Dictionary<BallStateManager, int> pocketedBalls;

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
        player1 = new Player();
        player2 = new Player();

        player1.score = new Score();
        player2.score = new Score();

        //pocketedBalls = new Dictionary<BallStateManager, int>();

        player1.ballType = getRandomBallType();

        if (player1.ballType == ballType.striped)
        {
            player2.ballType = ballType.filled;
        }
        else
        {
            player2.ballType = ballType.striped;
        }
        
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
        //Debug.Log("Spectating");
    }

    private void HandlePlayerOneTurn()
    {
        Debug.Log("Player One's Turn" + getTurnNumber());

        currentPlayerTurn = GameState.PlayerOneTurn;

        Debug.Log("Player One's ball type is " + player1.ballType.ToString());

        turnNumber++;

        UI_PlayerTurn.GetComponent<UnityEngine.UI.Text>().text = "Player One's Turn";
        UI_PlayerBallType.GetComponent<UnityEngine.UI.Text>().text = player1.ballType.ToString();

    }

    private void HandlePlayerTwoTurn()
    {
        Debug.Log("Player Two's Turn" + getTurnNumber());

        currentPlayerTurn = GameState.PlayerTwoTurn;

        Debug.Log("Player Two's ball type is " + player2.ballType.ToString());

        turnNumber++;

        UI_PlayerTurn.GetComponent<UnityEngine.UI.Text>().text = "Player Two's Turn";
        UI_PlayerBallType.GetComponent<UnityEngine.UI.Text>().text = player2.ballType.ToString();
    }

    public GameState getCurrentPlayerTurn()
    {
        return currentPlayerTurn;
    }

    public Player getCurrentPlayer()
    {
        if (currentPlayerTurn == GameState.PlayerOneTurn)
        {
            return player1;
        }
        else
        {
            return player2;
        }
    }

    public Player getPlayerWithBallStriped()
    {
        if (player1.ballType == ballType.striped)
        {
            return player1;
        }
        else
        {
            return player2;
        }
    }

    public Player getPlayerWithBallFilled()
    {
        if (player1.ballType == ballType.filled)
        {
            return player1;
        }
        else
        {
            return player2;
        }
    }

    public ballType getRandomBallType()
    {
        System.Array value = ballType.GetValues(typeof(ballType));
        System.Random random = new System.Random();
        ballType randomBall = (ballType)value.GetValue(random.Next(value.Length));
        return randomBall;
    }

    public ballType getPlayer1BallType()
    {
        return player1.ballType;
    }

    public ballType getPlayer2BallType()
    {
        return player2.ballType;
    }

    public int getTurnNumber()
    {
        return turnNumber;
    }

    public GameState switchPlayerTurn()
    {
        if (currentPlayerTurn == GameState.PlayerOneTurn)
        {
            return GameState.PlayerTwoTurn;
        }
        else
        {
            return GameState.PlayerOneTurn;
        }
    }

    //public Dictionary<BallStateManager, int> getPocketedBalls()
    //{
    //    return pocketedBalls;
    //}

    //public void addPocketedBall(BallStateManager ball, int turn)
    //{
    //    pocketedBalls.Add(ball, turn);
    //}

    //public void clearPocketedBalls()
    //{
    //    pocketedBalls.Clear();
    //}

    //public bool isBallPocketedLastTurn()
    //{
    //    foreach (KeyValuePair<BallStateManager, int> ball in pocketedBalls)
    //    {
    //        if (ball.Value == turnNumber - 1)
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    //public bool isLastPocketedBallMatchPlayerBall()
    //{
    //    foreach (KeyValuePair<BallStateManager, int> ball in pocketedBalls)
    //    {
    //        if (ball.Value == turnNumber - 1)
    //        {
    //            if (ball.Key.GetBallType() == getCurrentPlayer().ballType)
    //            {
    //                return true;
    //            }
    //        }
    //    }

    //    return false;
    //}


}
