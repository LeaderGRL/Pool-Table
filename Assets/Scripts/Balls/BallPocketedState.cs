using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPocketedState : BallBaseState
{
    public override void EnterState(BallStateManager ball)
    {
        BallStateManager.instance.addPocketedBall(ball.gameObject, GameManager.instance.getTurnNumber());
    }

    public override void OnCollisionEnter(BallStateManager ball, Collision collision)
    {

    }

    public override void UpdateState(BallStateManager ball)
    {

    }

    public override void FixedUpdateState(BallStateManager ball)
    {

    }

}
