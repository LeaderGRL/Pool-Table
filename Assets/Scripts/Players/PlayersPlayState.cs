using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    public override void EnterState(PlayersStateManagement player)
    {
        Debug.Log("Entered Play State");
        player.setPosition();
        player.lockCamera(false);
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        if (Input.GetMouseButton(0))
        {
            player.SwitchState(player.shootState);
        }

        player.setRotation();

        //else
        //{
        //    lockCamera(false); // Maybe We need to optimise this cause we call an unnecessary function every frame
        //    setRotation();
        //}
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {

    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        
    }
}
