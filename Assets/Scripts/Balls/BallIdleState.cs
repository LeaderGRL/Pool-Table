using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallIdleState : BallBaseState
{
    private int tempCount = 0;
    private bool hasCollide = false;
    public override void EnterState(BallStateManager ball)
    {
        if (ball.gameObject.tag != "white")
        {
            return;
        }
        
        if (ball.ballOut != null)
        {
            ball.resetBall(ball.ballOut);
        }
        //Debug.Log("Hello from the Idle State");
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {
        //if(ball.gameObject.tag != "white")
        //{
        //    return;
        //}

        //ball.getParent().GetComponentInChildren<BallStateManager>().hasCollide = true;
        //BallStateManager.instance.getParent().GetComponentInChildren<BallStateManager>().hasCollide == true;
        if (hasCollide == false)
        {
            if (collision.gameObject.tag == "white")
            {
                Debug.Log("JE SUIS COLIDE");


                hasCollide = true;


                //ball.SwitchState(ball.rollState);


                if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerOneTurn) // if its player 1's turn
                {
                    if (GameManager.instance.getPlayer1BallType() == ballType.striped) // if the player 1 has to hit filled balls
                    {
                        if (ball.GetBallType() == ballType.striped)
                        {
                            ball.hitTheGoodBall = false;
                            //GameManager.instance.updateGameState(GameState.PlayerTwoTurn); // if the player 1 hit the wrong ball, it is now player 2's turn
                            Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn " + tempCount++);
                            return;
                        }
                        else
                        {
                            Debug.Log(ballType.striped.ToString() + " " + ball.GetBallType() + " " + ball.name + "!!!!!!!!!!!!!!!!");
                            ball.hitTheGoodBall = true;
                            //GameManager.instance.updateGameState(GameState.PlayerOneTurn); // if the player 1 hit the right ball, it is still player 1's turn
                            Debug.Log("Player 1 hit the right ball, it is still player 1's turn " + tempCount++);
                            return;
                        }
                    }
                    else if (GameManager.instance.getPlayer1BallType() == ballType.filled)
                    {
                        if (ball.GetBallType() == ballType.filled)
                        {
                            ball.hitTheGoodBall = false;
                            //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                            Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn " + tempCount++);
                            return;
                        }
                        else
                        {
                            Debug.Log(ballType.filled.ToString() + " " + ball.GetBallType() + " " + ball.name + "!!!!!!!!!!!!!!!!");

                            ball.hitTheGoodBall = true;
                            //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                            Debug.Log("Player 1 hit the right ball, it is still player 1's turn " + tempCount++);
                            return;
                        }
                    }
                }
                else if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerTwoTurn)
                {
                    if (GameManager.instance.getPlayer2BallType() == ballType.striped)
                    {
                        if (ball.GetBallType() == ballType.striped)
                        {
                            ball.hitTheGoodBall = false;
                            //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                            Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn " + tempCount++);
                            return;
                        }
                        else
                        {
                            ball.hitTheGoodBall = true;
                            //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                            Debug.Log("Player 2 hit the right ball, it is still player 2's turn " + tempCount++);
                            return;
                        }
                    }
                    else if (GameManager.instance.getPlayer2BallType() == ballType.filled)
                    {
                        if (ball.GetBallType() == ballType.filled)
                        {
                            ball.hitTheGoodBall = false;
                            //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
                            Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn " + tempCount++);
                            return;
                        }
                        else
                        {
                            ball.hitTheGoodBall = true;
                            //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
                            Debug.Log("Player 2 hit the right ball, it is still player 2's turn " + tempCount++);
                            return;
                        }
                    }
                }
            }
        }
    }

    public override void UpdateState(BallStateManager ball)
    {
        if (ball.isBallMoving())
        {
            ball.SwitchState(ball.rollState);
        }
    }
    public override void FixedUpdateState(BallStateManager ball)
    {

    }
}
