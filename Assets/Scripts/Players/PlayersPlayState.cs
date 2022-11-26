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
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {

    }

    public override void LateUpdateState(PlayersStateManagement player)
    {
        player.Camera.transform.LookAt(player.WhiteBall.transform);

        TurnArround(player);

        player.Camera.transform.position = player.transform.position + player.CameraOffset;

    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        
    }

    protected void TurnArround(PlayersStateManagement player)
    {
        float mouseRotationX = Input.GetAxis("Mouse X");
        float mouseRotationY = Input.GetAxis("Mouse Y");

        player.Camera.transform.RotateAround(player.WhiteBall.transform.position, Vector3.up, mouseRotationX);
        player.Camera.transform.RotateAround(player.WhiteBall.transform.position, Vector3.forward, mouseRotationY);
    }
}
