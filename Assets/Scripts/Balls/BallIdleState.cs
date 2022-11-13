using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallIdleState : BallBaseState
{
    public override void EnterState(BallStateManager ball)
    {
        Debug.Log("Hello from the Idle State");
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {
        ball.SwitchState(ball.rollState);
    }

    public override void UpdateState(BallStateManager ball)
    {
        
    }
    public override void FixedUpdateState(BallStateManager ball)
    {

    }
}
