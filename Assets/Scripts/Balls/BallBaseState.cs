using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BallBaseState
{
    public abstract void EnterState(BallStateManager ball);

    public abstract void UpdateState(BallStateManager ball);

    public abstract void FixedUpdateState(BallStateManager ball);

    public abstract void OnCollisionEnter(BallStateManager ball, Collision collision);
    
}
