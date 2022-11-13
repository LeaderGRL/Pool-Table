
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerManager : MonoBehaviour
{
    public static PlayerManager playerInstance;

    [SerializeField] private GameObject cue;
    [SerializeField] private GameObject ball;
    private Player player1;
    private Player player2;
    private void Awake()
    {
        GameManager.OnGameStateChanged += GameManager_OnGameStateChanged;
    }

    private void GameManager_OnGameStateChanged(GameState state)
    {
        cue.SetActive(state == GameState.PlayerOneTurn || state == GameState.PlayerTwoTurn);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= GameManager_OnGameStateChanged;
    }

    // Start is called before the first frame update
    void Start()
    {
        player1 = new Player();
        player2 = new Player();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void nextPlayer()
    {
        if (GameManager.instance.state == GameState.spectate && ball.gameObject.GetComponent<Rigidbody>().velocity == Vector3.zero)
        {
            //GameManager.instance.updateGameState(GameState)
            //GameManager.Instance.CurrentState = GameState.PlayerTwoTurn;
        }
    }
}
