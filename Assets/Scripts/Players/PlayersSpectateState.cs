using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersSpectateState : PlayersBaseState
{
    private GameObject balls;    
    public override void EnterState(PlayersStateManagement player)
    {
        balls = GameObject.Find("Balls");

    }

    public override void UpdateState(PlayersStateManagement player)
    {
        if (balls.GetComponentInChildren<BallStateManager>().isBallMoving() == false)
        {
            player.SwitchState(player.playState);
        }
    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {

    }
}
