using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ballls : MonoBehaviour
{
    //private Vector3 ballPosition;
    //private Vector3 ballVelocity;

    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.15f;

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(gameObject.name + " - Velocity : " + gameObject.GetComponent<Rigidbody>().velocity);
    }

    void FixedUpdate()
    {
        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody.linearVelocity.y > 0)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.y *= 0.3f;
            rigidbody.linearVelocity = velocity;
        }
    }

    protected Vector3 getPosition()
    {
        return transform.position;
    }

    public Vector3 getWorldPosition()
    {
        return transform.InverseTransformPoint(getPosition());
    }

    protected Vector3 getVelocity()
    {
        return rb.linearVelocity;
    }

    protected bool isHit()
    {
        return rb.linearVelocity.magnitude > 0.1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }


}
