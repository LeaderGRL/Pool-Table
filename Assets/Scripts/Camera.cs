using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public GameObject target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(target.transform);

        TurnArround();
    }

    protected void TurnArround()
    {
        float mouseRotationX = Input.GetAxis("Mouse X");
        float mouseRotationY = Input.GetAxis("Mouse Y");
        
        transform.RotateAround(target.transform.position, Vector3.up, mouseRotationX);
        transform.RotateAround(target.transform.position, Vector3.forward, mouseRotationY);

        //Debug.Log("x : " + mouseRotationX + " y : " + mouseRotationY);
    }
}
