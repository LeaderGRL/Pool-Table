using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pocket : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "filled" || other.gameObject.tag == "striped" || other.gameObject.tag == "white")
        {
            other.GetComponent<BallStateManager>().SwitchState(other.GetComponent<BallStateManager>().pocketedState);
        }


        //if (other.gameObject.tag == "filled")
        //{
        //    other.gameObject.GetComponent<BallStateManager>().SwitchState(BallStateManager.instance.pocketedState);
        //    GameManager.instance.getPlayerWithBallFilled().score.addScore(1);
        //    GameManager.instance.UI_Message.GetComponent<UnityEngine.UI.Text>().text = "filled pocketed";

        //    if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerOneTurn)
        //    {
        //        if (GameManager.instance.getPlayer1BallType() == ballType.filled)
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //        }
        //        else
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //        }
        //    }
        //    else
        //    {
        //        if (GameManager.instance.getPlayer2BallType() == ballType.filled)
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //        }
        //        else
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //        }
        //    }

        //}
        //else if (other.gameObject.tag == "striped")
        //{
        //    GameManager.instance.getPlayerWithBallStriped().score.addScore(1);
        //    GameManager.instance.UI_Message.GetComponent<UnityEngine.UI.Text>().text = "striped pocketed";


        //    if (GameManager.instance.getCurrentPlayerTurn() == GameState.PlayerOneTurn)
        //    {
        //        if (GameManager.instance.getPlayer1BallType() == ballType.striped)
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //        }
        //        else
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //        }
        //    }
        //    else
        //    {
        //        if (GameManager.instance.getPlayer2BallType() == ballType.striped)
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
        //        }
        //        else
        //        {
        //            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
        //        }
        //    }
        //}

        //Destroy(other.gameObject);
        //Debug.Log(other.gameObject.name + " has been destroyed");
    }
}
