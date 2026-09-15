using UnityEngine;

public class PlayersSpectateState : PlayersBaseState
{
    private GameObject balls;
    private Vector3 targetPosition;
    private Vector3 velocity;

    public override void EnterState(PlayersStateManagement player)
    {
        player.SetShotCameraEnabled(false);
        player.SetAimingEnabled(false);
        player.SetSpinControlEnabled(false);
        player.SetShotPowerEnabled(false);
        player.SetSpectateCameraEnabled(true);
        balls = GameObject.Find("Balls");
        targetPosition = player.transform.position + Vector3.left;
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

    public override void OnMouseDown(PlayersStateManagement player)
    {
        
    }
}
