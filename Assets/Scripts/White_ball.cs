using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class White_ball : Balls
{
    void Start()
    {
        //rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.anyKey)
        //{
        //    pulse();
        //}

        //Debug.Log(getPosition());
    }

    //Compared to Update, which is called every frame, Fixed Update is called independently of framerate,
    //at measured intervals, in sync with the game’s physics system.
    void FixedUpdate()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }

    //void pulse()
    //{
    //    rb.AddForce(0.1f, 0, 0, ForceMode.Impulse);
    //}

}
