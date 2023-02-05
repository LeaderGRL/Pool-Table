using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteBallCollision : MonoBehaviour
{
    public void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("striped") && !collision.gameObject.CompareTag("filled"))
        {
            return;
        }

        if (gameObject.GetComponent<BallStateManager>().hasCollide)
        {
            return;
        }

        gameObject.GetComponent<BallStateManager>().hasCollide = true;

        if (!collision.gameObject.CompareTag(GameManager.instance.getCurrentPlayer().ballType.ToString()))
        {
            return;
        }

        PlayersStateManagement.Instance.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = true;
    }
}
