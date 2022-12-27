using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallStateManager : MonoBehaviour
{
    public static BallStateManager instance;

    BallBaseState currentState;
    public BallRollState rollState = new BallRollState();
    public BallIdleState idleState = new BallIdleState();
    public BallPocketedState pocketedState = new BallPocketedState();

    public bool hasCollide = false;
    public bool hitTheGoodBall = false;
    public GameObject ballOut;

    public Rigidbody rb;

    private ballType ballType;
    private Dictionary<GameObject, int> pocketedBalls;



    //[SerializeField] private GameObject whiteBall;



    // Start is called before the first frame update
    void Start()
    {

        instance = this;
        //starting state for the state machine
        currentState = idleState;
        // "this" is a reference to the context
        currentState.EnterState(this);

        pocketedBalls = new Dictionary<GameObject, int>();

        rb = GetComponent<Rigidbody>();
        
        SetBallType();

    }

    // Update is called once per frame
    void Update()
    {
        currentState.UpdateState(this);
    }

    private void FixedUpdate()
    {
        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody.velocity.y > 0)
        {
            var velocity = rigidbody.velocity;
            velocity.y *= 0.3f;
            rigidbody.velocity = velocity;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if((collision.gameObject.tag == "ball" || collision.gameObject.tag == "striped" || collision.gameObject.tag == "filled" || collision.gameObject.tag == "white") && collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude > 0.1f)
        {
            currentState.OnCollisionEnter(this, collision);
        }
    }

    public void SwitchState(BallBaseState state)
    {
        currentState = state;
        state.EnterState(this);
    }
    
    public float GetVelocity()
    {
        return GetComponent<Rigidbody>().velocity.magnitude;
    }

    public bool isBallMoving(){
        return GetComponent<Rigidbody>().velocity.magnitude > 0.001f;
    }

    public GameObject getParent()
    {
        return transform.parent.gameObject;
    }
    
    public float getBallRadius()
    {
        return GetComponent<SphereCollider>().radius;
    }

    public float getBallMass()
    {
        return GetComponent<Rigidbody>().mass;
    }

    private void SetBallType()
    {
        if (gameObject.tag == "ball")
        {
            //ballType = ballType.ball;
        }
        else if (gameObject.tag == "striped")
        {
            ballType = ballType.striped;
        }
        else if (gameObject.tag == "filled")
        {
            ballType = ballType.filled;
        }
        else if (gameObject.tag == "white")
        {
            //ballType = ballType.white;
        }
    }

    public ballType GetBallType()
    {
        return ballType;
    }

    public Dictionary<GameObject, int> getPocketedBalls()
    {
        return pocketedBalls;
    }

    public void addPocketedBall(GameObject ball, int turn)
    {
        pocketedBalls.Add(ball, turn);
    }

    public void clearPocketedBalls()
    {
        pocketedBalls.Clear();
    }

    public bool isBallPocketedLastTurn()
    {
        foreach (KeyValuePair<GameObject, int> ball in pocketedBalls)
        {
            if (ball.Value == GameManager.instance.getTurnNumber() - 1)
            {
                return true;
            }
        }

        return false;
    }
    
    public bool isLastPocketedBallMatchPlayerBall()
    {
        foreach (KeyValuePair<GameObject, int> ball in pocketedBalls)
        {
            if (ball.Value != GameManager.instance.getTurnNumber() - 1)
            {
                return false;
            }

            if (ball.Key.GetComponent<BallStateManager>().GetBallType() == GameManager.instance.getCurrentPlayer().ballType)
            {
                Debug.Log("Last pocketed ball is the same as the player's ball");
                return true;
            }
        }

        return false;
    }

    public bool isPocketedBallContainWhiteBall()
    {
        foreach (KeyValuePair<GameObject, int> ball in pocketedBalls)
        {
            if (ball.Key.gameObject.tag == "white")
            {
                return true;
            }
        }
        return false;
    }

    public void resetWhiteBallFromPocket()
    {
        GameObject temp = new GameObject();
        foreach (KeyValuePair<GameObject, int> ball in pocketedBalls)
        {
            if (ball.Key.gameObject.tag == "white")
            {
                ball.Key.gameObject.transform.position = new Vector3(-0.635f, 1, 0);
                ball.Key.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                ball.Key.gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                temp = ball.Key;
            }
        }
        if (temp != null)
        {
            pocketedBalls.Remove(temp);
        }
    }

    public void resetBall(GameObject ball)
    {
        ball.transform.position = new Vector3(-0.635f, 1, 0);
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    public void AddBallForce()
    {
        //GetComponent<Rigidbody>().AddForce(transform.forward * -Input.GetAxis("Mouse Y") * 10 * force);
    }

    public Vector3 CalculateCollisionVelocity(Vector3 velocity1, Vector3 velocity2, float mass1, float mass2, Collision collision)
    {
        // Calculate the relative velocity of the two objects
        Vector3 relativeVelocity = velocity1 - velocity2;

        // Calculate the impulse of the collision
        float impulse = -(1 + 1) * Vector3.Dot(relativeVelocity, collision.contacts[0].normal) / Vector3.Dot(collision.contacts[0].normal, collision.contacts[0].normal * (1 / mass1 + 1 / mass2));

        // Calculate the new velocity of the first object after the collision
        return velocity1 + impulse / mass1 * collision.contacts[0].normal;
    }

    public Vector3 CalculateCollisionVelocityImpulseMomentum(Vector3 velocity1, Vector3 velocity2, float mass1, float mass2, Vector3 normal)
    {
        // Calculate the relative velocity of the two objects
        Vector3 relativeVelocity = velocity1 - velocity2;

        // Calculate the impulse of the collision
        float impulse = -(1 + 1) * Vector3.Dot(relativeVelocity, normal) / (1 / mass1 + 1 / mass2);

        // Calculate the new velocity of the first object after the collision
        return velocity1 + impulse / mass1 * normal;
    }

    // Applies spin to a ball based on the direction and strength of the shot
    public void ApplySpin(Rigidbody ball, Vector3 direction, float strength)
    {
        // Clamp the strength to the maximum allowed spin
        strength = Mathf.Clamp(strength, 0, 100);

        // Calculate the spin to apply based on the direction and strength of the shot
        Vector3 spin = Vector3.Cross(direction, Vector3.up) * strength * ball.mass * getBallRadius();

        // Apply the spin to the ball
        ball.AddTorque(spin, ForceMode.Impulse);
    }


    // Calculates the impulse applied to two colliders based on their mass, velocity, and collision normal
    public float CalculateImpulse(Rigidbody collider1, Rigidbody collider2, Vector3 relativeVelocity, Vector3 collisionNormal)
    {
        // Calculate the mass of each collider
        float mass1 = collider1.mass;
        float mass2 = collider2.mass;

        // Calculate the coefficient of restitution (bounciness) of each collider
        float cor1 = collider1.gameObject.GetComponent<Collider>().material.bounciness;
        float cor2 = collider2.gameObject.GetComponent<Collider>().material.bounciness;

        // Calculate the combined coefficient of restitution
        float cor = cor1 * cor2;

        // Calculate the impulse applied to each collider
        float impulse = (-(1 + cor) * Vector3.Dot(relativeVelocity, collisionNormal)) / (1 / mass1 + 1 / mass2);

        return impulse;
    }

}
