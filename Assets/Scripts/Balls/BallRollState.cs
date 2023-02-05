using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class BallRollState : BallBaseState
{
    public override void EnterState(BallStateManager ball)
    {
        //Debug.Log(ball.gameObject.name + " : Hello from the BallRollState");
        PlayersStateManagement.Instance.SwitchState(PlayersStateManagement.Instance.spectateState);
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {

        if (ball.gameObject.tag != "white")
        {
            return;
        }

        if (collision.gameObject.tag != "striped" && collision.gameObject.tag != "filled")
        {
            return;
        }

        if (ball.gameObject.GetComponent<BallStateManager>().hasCollide)
        {
            return;
        }

        Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        ball.gameObject.GetComponent<BallStateManager>().hasCollide = true;

        if (GameManager.instance.getCurrentPlayer().ballType.ToString() != collision.gameObject.tag)
        {
            Debug.Log(GameManager.instance.getCurrentPlayer().ballType.ToString() + " collide with " + collision.gameObject.tag + " that is not the same type");
            return;
        }

        PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = true;





        //if (collision.gameObject.tag == "white")
        //{
        //    if (ball.gameObject.tag == "striped" || ball.gameObject.tag == "filled")
        //    {
        //        Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        //        collision.gameObject.GetComponent<BallStateManager>().hasCollide = true;
        //    }
        //}

        //if (ball.gameObject.tag == "white")
        //{
        //    if (collision.gameObject.tag == "striped" || collision.gameObject.tag == "filled")
        //    {
        //        Debug.Log(ball.gameObject.name + " collide with " + collision.gameObject.tag);
        //        ball.gameObject.GetComponent<BallStateManager>().hasCollide = true;
        //    }
        //}

        //Debug.Log("Hello from the roll State");

        // Get the Rigidbody of the first collider
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

        //// Calculate the rolling friction torque applied to each collider
        //float torque1 = CalculateRollingFrictionTorque(collider1, collisionPoint, collisionNormal);
        //float torque2 = CalculateRollingFrictionTorque(collider2, collisionPoint, collisionNormal);

        //// Apply the rolling friction torque to each collider
        //collider1.AddTorque(collisionNormal * torque1, ForceMode.Impulse);
        //collider2.AddTorque(-collisionNormal * torque2, ForceMode.Impulse);
        //Vector3 collisionNormal = collision.contacts[0].normal;

        //// Calculate the reflection vector (the direction in which the ball should bounce off the surface)
        //Vector3 reflectionVector = Vector3.Reflect(ball.rb.velocity, collisionNormal);

        //// Apply the reflection vector to the ball's velocity
        //ball.rb.velocity = reflectionVector;

        //// Apply spin to the ball based on the collision normal and the ball's angular velocity
        //ball.rb.angularVelocity = collisionNormal * ball.rb.angularVelocity.magnitude;

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


    }

    public override void UpdateState(BallStateManager ball)
    {
        if (!ball.isBallMoving())
        {
            ball.SwitchState(ball.idleState);
        }

        //if (ball.gameObject.GetComponent<Rigidbody>().velocity.magnitude > 0)
        //{
        //    ball.gameObject.GetComponent<Rigidbody>().AddForce(-ball.gameObject.GetComponent<Rigidbody>().velocity.normalized * ball.gameObject.GetComponent<Rigidbody>().velocity.magnitude * ball.gameObject.GetComponent<Rigidbody>().drag, ForceMode.Acceleration);
        //}

        //// If the ball is spinning, apply torque to slow it down
        //if (ball.gameObject.GetComponent<Rigidbody>().angularVelocity.magnitude > 0)
        //{
        //    ball.gameObject.GetComponent<Rigidbody>().AddTorque(-ball.gameObject.GetComponent<Rigidbody>().angularVelocity.normalized * ball.gameObject.GetComponent<Rigidbody>().angularVelocity.magnitude * ball.gameObject.GetComponent<Rigidbody>().angularDrag, ForceMode.Acceleration);
        //}

    }

    float CalculateRollingFrictionTorque(Rigidbody collider, Vector3 collisionPoint, Vector3 collisionNormal)
    {
        // Calculate the mass of the collider
        float mass = collider.mass;

        // Calculate the radius of the collider (assuming it is a sphere)
        float radius = collider.GetComponent<SphereCollider>().radius;

        // Calculate the velocity of the collider at the collision point
        Vector3 velocity = collider.GetPointVelocity(collisionPoint);

        // Calculate the rolling friction torque applied to the collider
        float torque = mass * radius * Vector3.Dot(velocity, collisionNormal) / collider.angularDrag;

        return torque;
    }

    public override void FixedUpdateState(BallStateManager ball)
    {
        
    }
}
