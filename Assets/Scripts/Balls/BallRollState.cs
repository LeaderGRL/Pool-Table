using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallRollState : BallBaseState
{
    public override void EnterState(BallStateManager ball)
    {
        //Debug.Log(ball.gameObject.name + " : Hello from the BallRollState");
        PlayersStateManagement.Instance.SwitchState(PlayersStateManagement.Instance.spectateState);
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {
        Debug.Log("Hello from the roll State");

        //Vector3 collisionNormal = collision.contacts[0].normal;

        //// Calculate the reflection vector (the direction in which the ball should bounce off the surface)
        //Vector3 reflectionVector = Vector3.Reflect(ball.rb.velocity, collisionNormal);

        //// Apply the reflection vector to the ball's velocity
        //ball.rb.velocity = reflectionVector;

        //// Apply spin to the ball based on the collision normal and the ball's angular velocity
        //ball.rb.angularVelocity = collisionNormal * ball.rb.angularVelocity.magnitude;

        if (collision.gameObject.CompareTag("Rail"))
        {
            // Calculate the velocity of the ball after the collision
            Vector3 velocity = ball.GetComponent<Rigidbody>().velocity;

            // Calculate the new velocity of the ball after the collision with the rail using the impulse-momentum method
            Vector3 newVelocity = ball.CalculateCollisionVelocityImpulseMomentum(velocity, Vector3.zero, ball.GetComponent<Rigidbody>().mass, 0, collision.contacts[0].normal);
            
            // Apply the new velocity to the ball
            ball.GetComponent<Rigidbody>().velocity = newVelocity;
        } 
        else
        {
            Vector3 velocity1 = ball.GetComponent<Rigidbody>().velocity;
            Vector3 velocity2 = collision.gameObject.GetComponent<Rigidbody>().velocity;

            // Calculate the new velocities of the two balls after the collision using the impulse-momentum method
            Vector3 newVelocity1 = ball.CalculateCollisionVelocityImpulseMomentum(velocity1, velocity2, 1, collision.gameObject.GetComponent<Rigidbody>().mass, collision.contacts[0].normal);
            Vector3 newVelocity2 = ball.CalculateCollisionVelocityImpulseMomentum(velocity2, velocity1, collision.gameObject.GetComponent<Rigidbody>().mass, 1, collision.contacts[0].normal);

            // Apply the new velocities to the balls
            ball.GetComponent<Rigidbody>().velocity = newVelocity1;
            collision.gameObject.GetComponent<Rigidbody>().velocity = newVelocity2;
        }

        
    }

    public override void UpdateState(BallStateManager ball)
    {
        if (!ball.isBallMoving())
        {
            ball.SwitchState(ball.idleState);
        }
    }

    public override void FixedUpdateState(BallStateManager ball)
    {
        
    }

}
