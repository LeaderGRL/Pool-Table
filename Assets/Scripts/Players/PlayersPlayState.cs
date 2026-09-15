using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    private bool waitingForBallInHandPlacement;

    //s[SerializeField] private GameObject whiteBall;
    public override void EnterState(PlayersStateManagement player)
    {
        player.SetShotPowerEnabled(false);
        player.SetAimingEnabled(true);
        player.SetSpinControlEnabled(true);
        GameManager.instance.turnNumber++;

        //player.setPosition();
        player.lockCamera(false);

        if (GameManager.instance.getTurnNumber() == 0)
        {
            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
            return;
        }

        //if (BallStateManager.instance.GetPlayAgain())
        //{
        //    Debug.Log("Play Again");
        //    BallStateManager.instance.SetPlayAgain(false);
        //    return;
        //}

        if (BallStateManager.instance.isPocketedBallContainWhiteBall())
        {
            Debug.Log("White Ball Pocketed");
            waitingForBallInHandPlacement = player.BeginBallInHandPlacement();

            if (!waitingForBallInHandPlacement)
            {
                BallStateManager.instance.resetWhiteBallFromPocket();
            }

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

        //Debug.Log("has collide : " + player.WhiteBall.GetComponent<BallStateManager>().hasCollide);

        if (!player.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall)
        {
            Debug.Log("Hit The Wrong Ball");
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        if (!BallStateManager.instance.IsBallPocketedLastTurn())
        {
            Debug.Log("No Ball Pocketed");
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            //BallStateManager.instance.SetPlayAgain(false);
            return;
        }

        if (!BallStateManager.instance.isLastPocketedBallMatchPlayerBall())
        {
            Debug.Log("Last Pocketed Ball Not Match");
            //Debug.Log("OUIIII : " + BallStateManager.instance.getPocketedBalls());
            GameManager.instance.updateGameState(GameManager.instance.switchPlayerTurn());
            BallStateManager.instance.SetPlayAgain(true);
            return;
        }

        if (BallStateManager.instance.GetPlayAgain())
        {
            Debug.Log("Play Again");
            BallStateManager.instance.SetPlayAgain(false);
            return;
        }
    }

    public override void UpdateState(PlayersStateManagement player)
    {
        if (waitingForBallInHandPlacement)
        {
            if (player.IsBallInHandPlacementEnabled())
            {
                return;
            }

            player.SetAimingEnabled(true);
            player.SetSpinControlEnabled(true);

            if (LegacyMouseInput.PrimaryButtonIsPressed)
            {
                return;
            }

            waitingForBallInHandPlacement = false;
            return;
        }

        if (LegacyMouseInput.PrimaryButtonIsPressed)
        {
            player.SwitchState(player.shootState);
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
