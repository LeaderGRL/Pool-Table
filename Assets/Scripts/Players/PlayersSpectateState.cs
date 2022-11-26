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
        // Without that, the code is somotime executed before the ball are moving !
        player.StartCoroutine(CheckIfBallIsMoving(player)); //Genius system 
        player.transform.position = Vector3.SmoothDamp(player.transform.position, targetPosition , ref velocity, 1f);
        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.Euler(player.transform.eulerAngles.x, player.transform.eulerAngles.y, 0), Time.deltaTime * 2f);
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
        
    }

    public override void LateUpdateState(PlayersStateManagement player)
    {
        player.Camera.transform.LookAt(player.WhiteBall.transform);
        if (player.WhiteBall.GetComponent<Rigidbody>().velocity.magnitude < 0.1f)
        {
            moveCamera(player);
        }
        //player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 2, ref velocity, 1f);

        //player.Camera.transform.position = player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 2;

    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {

    }

    private IEnumerator CheckIfBallIsMoving(PlayersStateManagement player)
    {
        yield return new WaitForSeconds(1);

        if (balls.GetComponentInChildren<BallStateManager>().isBallMoving() == false)
        {
            player.SwitchState(player.playState);
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
        player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.up * 2, ref velocity, 1f);

        switch (checkWhiteBallPosition(player))
        {
            case corner.bottom_left:
                player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.forward * 5 + Vector3.up, ref velocity, 1f);
                break;
            case corner.bottom_right:
                player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up, ref velocity, 1f);
                break;
            case corner.top_left:
                player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.forward * 5 + Vector3.up * 3, ref velocity, 1f);
                break;
            case corner.top_right:
                player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 3, ref velocity, 1f);
                break;
            default:
                player.Camera.transform.position = Vector3.SmoothDamp(player.Camera.transform.position, player.WhiteBall.transform.position + Vector3.back * 5 + Vector3.up * 2, ref velocity, 1f);
                break;
        }
    }
}
