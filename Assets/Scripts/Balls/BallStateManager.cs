using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallStateManager : MonoBehaviour
{
    public static BallStateManager instance;

    BallBaseState currentState;
    public BallRollState rollState = new BallRollState();
    public BallIdleState idleState = new BallIdleState();

    // Start is called before the first frame update
    void Start()
    {

        instance = this;
        //starting state for the state machine
        currentState = idleState;
        // "this" is a reference to the context
        currentState.EnterState(this);

        
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
        if((collision.gameObject.tag == "ball" || collision.gameObject.tag == "striped" || collision.gameObject.tag == "filled") && collision.gameObject.GetComponent<Rigidbody>().velocity.magnitude > 0.1f)
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
}
