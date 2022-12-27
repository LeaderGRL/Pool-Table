using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum corner
{
    top_left,
    top_right,
    bottom_left,
    bottom_right
}

public class PlayersSpectateState : PlayersBaseState
{
    private GameObject balls;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private bool cameraHasMoved;
    public override void EnterState(PlayersStateManagement player)
    {
        cameraHasMoved = false;
        balls = GameObject.Find("Balls");
        targetPosition = player.transform.position + Vector3.left;
        //player.lockCamera(false);

    }

    public override void UpdateState(PlayersStateManagement player)
    {
        // Use a coroutine to execute the code after a delay.
        // Without that, the code is sometime executed before the ball are moving !
        //player.StartCoroutine(CheckIfBallIsMoving(player)); //Genius system 
        CheckIfBallIsMoving(player);

        //Animating the cue to get it off the table
        player.transform.position = Vector3.SmoothDamp(player.transform.position, targetPosition , ref velocity, 1f);
        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.Euler(-90, player.transform.eulerAngles.y, player.transform.eulerAngles.z), Time.deltaTime * 2f);
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
        
    }

    public override void LateUpdateState(PlayersStateManagement player)
    {
        player.Cam.transform.LookAt(player.WhiteBall.transform);
        if (player.WhiteBall.GetComponent<Rigidbody>().velocity.magnitude < 0.1f)
        {
            player.Cam.transform.LookAt(player.WhiteBall.transform);
            moveCamera(player);
        }


        //player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 2, ref velocity, 1f);

        //player.Cam.transform.position = player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 2;

    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        player.WhiteBall.GetComponent<BallStateManager>().hasCollide = true;
    }

    private void CheckIfBallIsMoving(PlayersStateManagement player)
    {
        //yield return new WaitForSeconds(1);

        if (balls.GetComponentInChildren<BallStateManager>().isBallMoving() == false)
        {
            player.SwitchState(player.playState);
            //yield break;
        }
    }

    private corner checkWhiteBallPosition(PlayersStateManagement player)
    {
        if (player.WhiteBall.transform.position.x < 0 && player.WhiteBall.transform.position.z > 0)
        {
            return corner.bottom_left;
        }
        else if (player.WhiteBall.transform.position.x < 0 && player.WhiteBall.transform.position.z < 0)
        {
            return corner.bottom_right;
        }
        else if (player.WhiteBall.transform.position.x > 0 && player.WhiteBall.transform.position.z > 0)
        {
            return corner.top_left;
        }
        else if (player.WhiteBall.transform.position.x > 0 && player.WhiteBall.transform.position.z < 0)
        {
            return corner.top_right;
        }
        else
        {
            return corner.bottom_left;
        }
    }

    private void moveCamera(PlayersStateManagement player)
    {
        player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.up * 2, ref velocity, 1f);

        switch (checkWhiteBallPosition(player))
        {
            case corner.bottom_left:
                player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.forward * 100 + Vector3.up, ref velocity, 1f);
                break;
            case corner.bottom_right:
                player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.back * 100 + Vector3.up, ref velocity, 1f);
                break;
            case corner.top_left:
                player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.forward * 100 + Vector3.up * 3, ref velocity, 1f);
                break;
            case corner.top_right:
                player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.back * 100 + Vector3.up * 3, ref velocity, 1f);
                break;
            default:
                player.Cam.transform.position = Vector3.SmoothDamp(player.Cam.transform.position, player.WhiteBall.transform.position + Vector3.back * 100 + Vector3.up * 2, ref velocity, 1f);
                break;
        }
    }

    public override void OnMouseDown(PlayersStateManagement player)
    {
        
    }
}
