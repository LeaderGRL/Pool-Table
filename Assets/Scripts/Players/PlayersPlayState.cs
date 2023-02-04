using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    //s[SerializeField] private GameObject whiteBall;
    public override void EnterState(PlayersStateManagement player)
    {
        //player.setPosition();
        player.lockCamera(false);

        if (GameManager.instance.getTurnNumber() == 0)
        {
            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
            return;
        }

        if (BallStateManager.instance.GetPlayAgain())
        {
            Debug.Log("Play Again");
            BallStateManager.instance.SetPlayAgain(false);
            return;
        }

        if (BallStateManager.instance.isPocketedBallContainWhiteBall())
        {
            Debug.Log("White Ball Pocketed");
            BallStateManager.instance.resetWhiteBallFromPocket();
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        if (!player.WhiteBall.GetComponent<BallStateManager>().hasCollide)
        {
            Debug.Log("White Ball Not Hit");
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        if (!BallStateManager.instance.hitTheGoodBall)
        {
            Debug.Log("Hit The Wrong Ball");
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        if (BallStateManager.instance.isLastPocketedBallMatchPlayerBall() == false)
        {
            Debug.Log("OUIIII : " + BallStateManager.instance.getPocketedBalls());
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        if (Input.GetMouseButton(0))
        {
            player.SwitchState(player.shootState);
        }
        player.setPosition();
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

    public override void OnMouseDown(PlayersStateManagement player)
    {
        //Vector3 power = Vector3.right * 100;
        //player.WhiteBall.GetComponent<Rigidbody>().AddForce(power, ForceMode.Impulse);
    }
}
