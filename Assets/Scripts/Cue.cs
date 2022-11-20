using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cue : MonoBehaviour
{
    public GameObject WhiteBall;
    public GameObject Camera;
    public float distance;
    public float force;

    // Start is called before the first frame update
    void Start()
    {
        setPosition();
        //setRotation();

    }

    // Update is called once per frame
    void Update()
    {


        //Debug.Log("Stick position : " + getPosition());
        //Debug.Log("White ball world position: " + WhiteBall.transform.InverseTransformPoint(transform.position));
        //Debug.Log("Stick world position: " + transform.InverseTransformPoint(WhiteBall.transform.position));
        //Debug.Log("Stick direction : " + getDirection());

        //Debug.Log("Y Axis : " + Input.GetAxis("Mouse Y"));

        if (Input.GetMouseButton(0))
        {
            shoot();
        }
        else
        {
            lockCamera(false); // Maybe We need to optimise this cause we call an unnecessary function every frame
            setRotation();
        }
    }

    protected Vector3 getPosition()
    {
        return transform.position;
    }

    protected Vector3 getVelocity()
    {
        return transform.GetComponent<Rigidbody>().velocity;
    }

    protected Vector3 getDirection()
    {
        return (transform.position -WhiteBall.transform.position).normalized;
    }

    protected void setPosition()
    {   
        transform.position = (transform.position - WhiteBall.transform.position).normalized * distance + WhiteBall.transform.position;
    }

    protected void setRotation()
    {
        transform.LookAt(WhiteBall.transform.position);
        transform.Rotate(0, 90, 90);

        turnArround();
    }

    protected void turnArround()
    {
        float mouseRotationX = Input.GetAxis("Mouse X");
        float mouseRotationY = Input.GetAxis("Mouse Y");

        transform.RotateAround(WhiteBall.transform.position, Vector3.up, mouseRotationX);
        transform.RotateAround(WhiteBall.transform.position, Vector3.forward, mouseRotationY);
    }

    protected void shoot()
    {
        //Debug.Log("Shoot");
        
        float mouseAxisY = Input.GetAxis("Mouse Y");
        
        lockCamera(true);

        transform.position -= transform.up * mouseAxisY * 0.1f;
    }

    protected void lockCamera(bool lockCamera)
    {
        Camera.GetComponent<Camera>().enabled = !lockCamera;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Force : " + transform.up * -Input.GetAxis("Mouse Y") * 10 * force);

        //Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA: " + collision.contacts[0].point);
        //Vector3 forceDirection = (collision.contacts[0].point - transform.position).normalized;
        collision.rigidbody.AddForce(transform.up * -Input.GetAxis("Mouse Y") * 10 *  force, ForceMode.Impulse);
        //collision.rigidbody.AddForce(forceDirection * gameObject.GetComponent<Rigidbody>().velocity.magnitude * 100, ForceMode.Impulse);
        if (collision.gameObject.tag == "ball")
        {
            GameManager.instance.updateGameState(GameState.spectate);
        }
    }
}
