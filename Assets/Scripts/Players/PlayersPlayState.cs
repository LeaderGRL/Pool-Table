using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    //s[SerializeField] private GameObject whiteBall;
    public override void EnterState(PlayersStateManagement player)
    {
        Debug.Log("Entered Play State");
        
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
        player.Camera.transform.LookAt(player.WhiteBall.transform);
        

        TurnArround(player);

        player.Camera.transform.position = (player.transform.position + player.WhiteBall.transform.position) / 2 + player.CameraOffset;

        //player.Camera.transform.position = player.transform.position + player.Camera.transform.forward * player.CameraDistance;

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
