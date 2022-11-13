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

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        collision.rigidbody.AddForce(player.transform.up * -Input.GetAxis("Mouse Y") * 10 * player.force, ForceMode.Impulse);
        if (collision.gameObject.tag == "ball")
        {
            player.SwitchState(player.spectateState);
        }
    }
}
