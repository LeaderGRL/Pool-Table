using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallIdleState : BallBaseState
{
    public override void EnterState(BallStateManager ball)
    {
        //Debug.Log("Hello from the Idle State");
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {
        if(ball.gameObject.tag != "white")
        {
            return;
        }
        Debug.Log(ball.gameObject.name);
        if (GameManager.instance.getTurnNumber() != 1) // if its not the first turn of the game
        {
            //ball.getParent().GetComponentInChildren<BallStateManager>().hasCollide = true;
            //BallStateManager.instance.getParent().GetComponentInChildren<BallStateManager>().hasCollide == true;
            if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerOneTurn) // if its player 1's turn
            {
                if (GameManager.instance.getPlayer1BallType() == ballType.striped) // if the player 1 has to hit filled balls
                {
                    if (collision.gameObject.tag == ballType.filled.ToString())
                    {
                        GameManager.instance.updateGameState(GameState.PlayerTwoTurn); // if the player 1 hit the wrong ball, it is now player 2's turn
                        Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn");
                    }
                    else
                    {
                        GameManager.instance.updateGameState(GameState.PlayerOneTurn); // if the player 1 hit the right ball, it is still player 1's turn
                        Debug.Log("Player 1 hit the right ball, it is still player 1's turn");
                    }
                }
                else if (GameManager.instance.getPlayer1BallType() == ballType.filled)
                {
                    if (collision.gameObject.tag == ballType.striped.ToString())
                    {
                        GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                        Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn");
                    }
                    else
                    {
                        GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                        Debug.Log("Player 1 hit the right ball, it is still player 1's turn");
                    }
                }
            }
            else if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerTwoTurn)
            {
                if (GameManager.instance.getPlayer2BallType() == ballType.striped)
                {
                    if (collision.gameObject.tag == ballType.filled.ToString())
                    {
                        GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                        Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn");
                    }
                    else
                    {
                        GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                        Debug.Log("Player 2 hit the right ball, it is still player 2's turn");
                    }
                }
                else if (GameManager.instance.getPlayer2BallType() == ballType.filled)
                {
                    if (collision.gameObject.tag == ballType.striped.ToString())
                    {
                        GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                        Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn");
                    }
                    else
                    {
                        GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                        Debug.Log("Player 2 hit the right ball, it is still player 2's turn");
                    }
                }
            }
        }

        ball.SwitchState(ball.rollState);
    }

    public override void UpdateState(BallStateManager ball)
    {
        
    }
    public override void FixedUpdateState(BallStateManager ball)
    {

    }
}
