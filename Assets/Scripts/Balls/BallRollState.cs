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
