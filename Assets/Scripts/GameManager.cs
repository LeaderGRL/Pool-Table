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
    public GameObject UI_Player1Turn;
    public GameObject UI_Player2Turn;
    public GameObject UI_Player1BallType;
    public GameObject UI_Player2BallType;
    public GameObject UI_Message;

    public GameObject ballPrefab;
    private float ballRadius;
    public float ballSpacing;

    public int rows;


    private GameState currentPlayerTurn;

    private Player player1;
    private Player player2;

    private int turnNumber = 0;


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
        //setTrianglePositionBall();

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

        //updateGameState(GameState.PlayerOneTurn);
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

        //Debug.Log("Player One's ball type is " + player1.ballType.ToString());

        turnNumber++;

        UI_Player1Turn.SetActive(true);
        UI_Player2Turn.SetActive(false);
        
        UI_Player1BallType.GetComponent<UnityEngine.UI.Text>().text = "Playing as " + player1.ballType.ToString();
        UI_Player2BallType.GetComponent<UnityEngine.UI.Text>().text = "Playing as " + player2.ballType.ToString();
        //UI_PlayerTurn.GetComponent<UnityEngine.UI.Text>().text = "Player One's Turn";
        //UI_Player1BallType.GetComponent<UnityEngine.UI.Text>().text = player1.ballType.ToString();

    }

    private void HandlePlayerTwoTurn()
    {
        Debug.Log("Player Two's Turn" + getTurnNumber());

        currentPlayerTurn = GameState.PlayerTwoTurn;

        turnNumber++;

        UI_Player1Turn.SetActive(false);
        UI_Player2Turn.SetActive(true);

        UI_Player1BallType.GetComponent<UnityEngine.UI.Text>().text = "Playing as " + player1.ballType.ToString();
        UI_Player2BallType.GetComponent<UnityEngine.UI.Text>().text = "Playing as " + player2.ballType.ToString();
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

    private void setTrianglePositionBall()
    {
        int count = ballPrefab.transform.childCount;
        float posBall = 0;

        float rowOffset = 0;

        ballRadius = ballPrefab.GetComponentInChildren<SphereCollider>().radius;

        // Calculate the position of the first ball in the triangle
        Vector3 startPosition = ballPrefab.transform.position;

        // Calculate the center position of the current row

        for (int row = 0; row < rows; row++)
            {


            rowOffset = row * ballRadius * 2 + ballSpacing * row;

            // Calculate the number of balls in the current row
            int ballCount = rows - row;

            Vector3 rowCenter = startPosition + new Vector3(-rowOffset / 2, 0, 0);


            // Place the balls in the current row
            for (int j = 0; j < ballCount; j++)
                {

                posBall += ballSpacing;

                // Calculate the index of the current ball
                int index = j + (row * (row + 1)) / 2;


                // Print the index to the console
                Debug.Log("Index: " + index);
                // Calculate the position of the current ball
                Vector3 ballPosition = rowCenter + new Vector3(ballSpacing * j, 0, 0);

                posBall += ballSpacing;

                // Update the position of the ball game object
                if(count > 1)
                {
                    ballPrefab.transform.GetChild(count-1).gameObject.transform.position = ballPosition;

                }
                count--;
                }
        }
    }
}

