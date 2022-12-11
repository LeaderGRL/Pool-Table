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

    private ballType ballType;
    private Dictionary<GameObject, int> pocketedBalls;



    // Start is called before the first frame update
    void Start()
    {

        instance = this;
        //starting state for the state machine
        currentState = idleState;
        // "this" is a reference to the context
        currentState.EnterState(this);

        pocketedBalls = new Dictionary<GameObject, int>();
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
}
