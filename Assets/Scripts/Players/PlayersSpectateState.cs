using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersSpectateState : PlayersBaseState
{
    private GameObject balls;    
    public override void EnterState(PlayersStateManagement player)
    {
        //Debug.Log("Spectate State");
        balls = GameObject.Find("Balls");
        
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        // Use a coroutine to execute the code after a delay.
        // Without that, the code is somotime executed before the ball are moving !
        player.StartCoroutine(CheckIfBallIsMoving(player)); //Genius system 
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
        
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
}
