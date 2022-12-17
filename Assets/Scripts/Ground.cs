using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
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
        if(other.gameObject.tag == "white")
        {
            BallStateManager.instance.resetBall(other.gameObject);
            BallStateManager.instance.ballOut = other.gameObject;
            return;
        }
    }
}
