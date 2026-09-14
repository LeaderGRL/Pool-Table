using UnityEngine;

public class PlayersShootState : PlayersBaseState
{
    public override void EnterState(PlayersStateManagement player)
    {
        player.SetAimingEnabled(false);
        player.SetShotPowerEnabled(true);
        player.lockCamera(true);
        player.WhiteBall.GetComponent<BallStateManager>().hasCollide = false;
        player.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = false;
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        if (!player.IsShotPowerEnabled())
        {
            player.SwitchState(player.spectateState);
        }
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
    }

    public override void LateUpdateState(PlayersStateManagement player)
    {
    }

    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
    }

    public override void OnMouseDown(PlayersStateManagement player)
    {
    }
}
