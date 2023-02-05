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

        //if (!ball.gameObject.CompareTag("white") && !collision.gameObject.CompareTag("white"))
        //{
        //    return;
        //}

        //Debug.Log("Idle Collision between " + ball.gameObject.name + " and " + collision.gameObject.name + " detected !!!!!!!!!!!!!!!!!!!!");

        //if ((!collision.gameObject.CompareTag("striped") && !collision.gameObject.CompareTag("filled")) && (!ball.gameObject.CompareTag("filled") && !ball.gameObject.CompareTag("striped")))
        //{
        //    return;
        //}

        //if (ball.gameObject.GetComponent<BallStateManager>().hasCollide)
        //{
        //    return;
        //}

        //Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        //ball.gameObject.GetComponent<BallStateManager>().hasCollide = true;

        //if (!collision.gameObject.CompareTag(GameManager.instance.getCurrentPlayer().ballType.ToString()))
        //{
        //    Debug.Log(GameManager.instance.getCurrentPlayer().ballType.ToString() + " collide with " + collision.gameObject.tag + " that is not the same type");
        //    return;
        //}

        //PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = true;

        //Debug.Log("White ball hit the good ball : " + PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall);


        //Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);

        //if (collision.gameObject.tag != "white")
        //{
        //    return;
        //}

        //if (ball.gameObject.tag != "striped" || ball.gameObject.tag != "filled")
        //{
        //    return;
        //}

        //Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        //collision.gameObject.GetComponent<BallStateManager>().hasCollide = true;

        //if (GameManager.instance.getCurrentPlayer().ballType.ToString() != collision.gameObject.tag)
        //{
        //    Debug.Log(GameManager.instance.getCurrentPlayer().ballType.ToString() + " collide with " + collision.gameObject.tag + " that is not the same type");
        //    return;
        //}

        //PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = true;




        //if (ball.gameObject.tag == "white")
        //{
        //    if (collision.gameObject.tag == "striped" || collision.gameObject.tag == "filled")
        //    {
        //        Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        //        ball.gameObject.GetComponent<BallStateManager>().hasCollide = true;
        //    }
        //}

        //if (PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hasCollide)
        //{
        //    if (GameManager.instance.getCurrentPlayer().ballType.ToString() == ball.gameObject.tag.ToString() || GameManager.instance.getCurrentPlayer().ballType.ToString() == collision.gameObject.tag.ToString())
        //    {
        //        PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = true;
        //        return;
        //    }
        //}











        //Get the Rigidbody of the first collider
        //Rigidbody collider1 = ball.gameObject.GetComponent<SphereCollider>().attachedRigidbody;

        //// Get the Rigidbody of the second collider
        //Rigidbody collider2 = collision.collider.attachedRigidbody;

        //// Calculate the collision point and normal
        //Vector3 collisionPoint = collision.contacts[0].point;
        //Vector3 collisionNormal = collision.contacts[0].normal;

        //// Calculate the relative velocity of the two colliders at the collision point
        //Vector3 relativeVelocity = collider1.GetPointVelocity(collisionPoint) - collider2.GetPointVelocity(collisionPoint);

        //// Calculate the impulse applied to each collider
        //float impulse = ball.CalculateImpulse(collider1, collider2, relativeVelocity, collisionNormal);

        //// Apply the impulse to each collider
        //collider1.AddForceAtPosition(collisionNormal * impulse, collisionPoint, ForceMode.Impulse);
        //collider2.AddForceAtPosition(-collisionNormal * impulse, collisionPoint, ForceMode.Impulse);

        //if (collision.gameObject.CompareTag("Rail"))
        //{
        //    // Calculate the velocity of the ball after the collision
        //    Vector3 velocity = ball.GetComponent<Rigidbody>().velocity;

        //    // Calculate the new velocity of the ball after the collision with the rail using the impulse-momentum method
        //    Vector3 newVelocity = ball.CalculateCollisionVelocityImpulseMomentum(velocity, Vector3.zero, ball.GetComponent<Rigidbody>().mass, 0, collision.contacts[0].normal);

        //    // Apply the new velocity to the ball
        //    ball.GetComponent<Rigidbody>().velocity = newVelocity;
        //}
        //else
        //{
        //    Vector3 velocity1 = ball.GetComponent<Rigidbody>().velocity;
        //    Vector3 velocity2 = collision.gameObject.GetComponent<Rigidbody>().velocity;

        //    // Calculate the new velocities of the two balls after the collision using the impulse-momentum method
        //    Vector3 newVelocity1 = ball.CalculateCollisionVelocityImpulseMomentum(velocity1, velocity2, 1, collision.gameObject.GetComponent<Rigidbody>().mass, collision.contacts[0].normal);
        //    Vector3 newVelocity2 = ball.CalculateCollisionVelocityImpulseMomentum(velocity2, velocity1, collision.gameObject.GetComponent<Rigidbody>().mass, 1, collision.contacts[0].normal);

        //    // Apply the new velocities to the balls
        //    ball.GetComponent<Rigidbody>().velocity = newVelocity1;
        //    collision.gameObject.GetComponent<Rigidbody>().velocity = newVelocity2;
        //}



        // Calculate the velocity of the ball after the collision
        //Vector3 velocity1 = ball.GetComponent<Rigidbody>().velocity;
        //Vector3 velocity2 = collision.gameObject.GetComponent<Rigidbody>().velocity;

        //// Calculate the new velocities of the two balls after the collision
        //Vector3 newVelocity1 = CalculateCollisionVelocity(velocity1, velocity2, ball.GetComponent<Rigidbody>().mass, collision.gameObject.GetComponent<Rigidbody>().mass, collision);
        //Vector3 newVelocity2 = CalculateCollisionVelocity(velocity2, velocity1, collision.gameObject.GetComponent<Rigidbody>().mass, ball.GetComponent<Rigidbody>().mass, collision);

        //// Apply the new velocities to the balls
        //ball.GetComponent<Rigidbody>().velocity = newVelocity1;
        //collision.gameObject.GetComponent<Rigidbody>().velocity = newVelocity2;

        //if (hasCollide == false)
        //{
        //    if (collision.gameObject.tag == "white")
        //    {
        //        Debug.Log("JE SUIS COLIDE");


        //        hasCollide = true;


        //        //ball.SwitchState(ball.rollState);


        //        if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerOneTurn) // if its player 1's turn
        //        {
        //            if (GameManager.instance.getPlayer1BallType() == ballType.striped) // if the player 1 has to hit filled balls
        //            {
        //                if (ball.GetBallType() == ballType.striped)
        //                {
        //                    ball.hitTheGoodBall = false;
        //                    //GameManager.instance.updateGameState(GameState.PlayerTwoTurn); // if the player 1 hit the wrong ball, it is now player 2's turn
        //                    Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn " + tempCount++);
        //                    return;
        //                }
        //                else
        //                {
        //                    Debug.Log(ballType.striped.ToString() + " " + ball.GetBallType() + " " + ball.name + "!!!!!!!!!!!!!!!!");
        //                    ball.hitTheGoodBall = true;
        //                    //GameManager.instance.updateGameState(GameState.PlayerOneTurn); // if the player 1 hit the right ball, it is still player 1's turn
        //                    Debug.Log("Player 1 hit the right ball, it is still player 1's turn " + tempCount++);
        //                    return;
        //                }
        //            }
        //            else if (GameManager.instance.getPlayer1BallType() == ballType.filled)
        //            {
        //                if (ball.GetBallType() == ballType.filled)
        //                {
        //                    ball.hitTheGoodBall = false;
        //                    //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //                    Debug.Log("Player 1 hit the wrong ball, it is now player 2's turn " + tempCount++);
        //                    return;
        //                }
        //                else
        //                {
        //                    Debug.Log(ballType.filled.ToString() + " " + ball.GetBallType() + " " + ball.name + "!!!!!!!!!!!!!!!!");

        //                    ball.hitTheGoodBall = true;
        //                    //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //                    Debug.Log("Player 1 hit the right ball, it is still player 1's turn " + tempCount++);
        //                    return;
        //                }
        //            }
        //        }
        //        else if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerTwoTurn)
        //        {
        //            if (GameManager.instance.getPlayer2BallType() == ballType.striped)
        //            {
        //                if (ball.GetBallType() == ballType.striped)
        //                {
        //                    ball.hitTheGoodBall = false;
        //                    //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //                    Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn " + tempCount++);
        //                    return;
        //                }
        //                else
        //                {
        //                    ball.hitTheGoodBall = true;
        //                    //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //                    Debug.Log("Player 2 hit the right ball, it is still player 2's turn " + tempCount++);
        //                    return;
        //                }
        //            }
        //            else if (GameManager.instance.getPlayer2BallType() == ballType.filled)
        //            {
        //                if (ball.GetBallType() == ballType.filled)
        //                {
        //                    ball.hitTheGoodBall = false;
        //                    //GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //                    Debug.Log("Player 2 hit the wrong ball, it is now player 1's turn " + tempCount++);
        //                    return;
        //                }
        //                else
        //                {
        //                    ball.hitTheGoodBall = true;
        //                    //GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //                    Debug.Log("Player 2 hit the right ball, it is still player 2's turn " + tempCount++);
        //                    return;
        //                }
        //            }
        //        }
        //    }
        //}
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
