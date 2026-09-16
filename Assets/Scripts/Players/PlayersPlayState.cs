using UnityEngine;

public class PlayersPlayState : PlayersBaseState
{
    private bool waitingForBallInHandPlacement;
    private bool pointerPrimaryWasPressed;
    private bool pointerYawAdjustmentStarted;

    //s[SerializeField] private GameObject whiteBall;
    public override void EnterState(PlayersStateManagement player)
    {
        player.SetShotCameraEnabled(false);
        player.SetSpectateCameraEnabled(false);
        player.SetShotPowerEnabled(false);
        player.SetAimingEnabled(true);
        player.SetSpinControlEnabled(true);

        waitingForBallInHandPlacement = false;
        pointerPrimaryWasPressed = LegacyMouseInput.PointerPrimaryButtonIsPressed;
        pointerYawAdjustmentStarted = false;
        player.ResetPointerAimingSequence();

        if (GameManager.instance.getTurnNumber() == 0)
        {
            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
            GameManager.instance.turnNumber++;
            return;
        }

        GameManager.instance.turnNumber++;

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
                pointerPrimaryWasPressed = LegacyMouseInput.PointerPrimaryButtonIsPressed;
                return;
            }

            waitingForBallInHandPlacement = false;
            pointerPrimaryWasPressed = false;
            pointerYawAdjustmentStarted = false;
            player.ResetPointerAimingSequence();
            return;
        }

        var pointerPrimaryIsPressed = LegacyMouseInput.PointerPrimaryButtonIsPressed;
        var pointerPrimaryPressedThisFrame = pointerPrimaryIsPressed && !pointerPrimaryWasPressed;
        pointerPrimaryWasPressed = pointerPrimaryIsPressed;

        if (pointerPrimaryPressedThisFrame)
        {
            if (!pointerYawAdjustmentStarted)
            {
                pointerYawAdjustmentStarted = true;
                player.BeginPointerYawAdjustment();
                return;
            }

            player.SwitchState(player.shootState);
            return;
        }

        if (LegacyMouseInput.GamepadPrimaryButtonIsPressed)
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
