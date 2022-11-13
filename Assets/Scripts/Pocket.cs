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
        if (other.gameObject.tag == "filled")
        {
            //Destroy
            Destroy(other.gameObject);
            GameManager.instance.updateGameState(GameState.PlayerOneTurn);
            Debug.Log(other.gameObject.name + " has been destroyed");
        }
        else if (other.gameObject.tag == "striped")
        {
            //Destroy
            Destroy(other.gameObject);
            GameManager.instance.updateGameState(GameState.PlayerTwoTurn);
            Debug.Log(other.gameObject.name + " has been destroyed");
        }
    }
}
