using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersShootState : PlayersBaseState
{
    public override void EnterState(PlayersStateManagement player)
    {
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        player.shoot();
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
        
    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        collision.rigidbody.AddForce(player.transform.up * -Input.GetAxis("Mouse Y") * 10 * player.force, ForceMode.Impulse);
        if (collision.gameObject.tag == "white")
        {
            //Debug.Log("HIT THE WHITE BALL !");
            player.SwitchState(player.spectateState);
        }
    }
}
