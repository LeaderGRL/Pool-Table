using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    //s[SerializeField] private GameObject whiteBall;
    public override void EnterState(PlayersStateManagement player)
    {        
        player.setPosition();
        player.lockCamera(false);

        if (BallStateManager.instance.isPocketedBallContainWhiteBall())
        {
            BallStateManager.instance.resetWhiteBallFromPocket();
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            return;
        }

        if (!player.WhiteBall.GetComponent<BallStateManager>().hasCollide)
        {
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            return;
        }

        if (!BallStateManager.instance.hitTheGoodBall)
        {
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            return;
        }

        if (BallStateManager.instance.isLastPocketedBallMatchPlayerBall() == false)
        {
            Debug.Log("OUIIII : " + BallStateManager.instance.getPocketedBalls());
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            return;
        }

        
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
        player.Cam.transform.LookAt(player.WhiteBall.transform);
        

        TurnArround(player);

        player.Cam.transform.position = (player.transform.position + player.WhiteBall.transform.position) / 2 + player.CameraOffset;

        //player.Cam.transform.position = player.transform.position + player.Cam.transform.forward * player.CameraDistance;

    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        
    }

    protected void TurnArround(PlayersStateManagement player)
    {
        float mouseRotationX = Input.GetAxis("Mouse X");
        float mouseRotationY = Input.GetAxis("Mouse Y");

        player.Cam.transform.RotateAround(player.WhiteBall.transform.position, Vector3.up, mouseRotationX);
        player.Cam.transform.RotateAround(player.WhiteBall.transform.position, Vector3.forward, mouseRotationY);

        //limit the rotation
        //Vector3 rotation = player.Cam.transform.eulerAngles;
        //rotation.x = Mathf.Clamp(rotation.x, 0, 90);
        //rotation.y = Mathf.Clamp(rotation.y, 0, 90);
        //rotation.z = Mathf.Clamp(rotation.z, 0, 90);
        //player.Cam.transform.eulerAngles = rotation;
       
    }
}
