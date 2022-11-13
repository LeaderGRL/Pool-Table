using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stick_Raycast : Raycast
{
    public GameObject pt;

    protected Ray ray;
    protected RaycastHit hit;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        ray = new Ray(transform.position, transform.up);

        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            //Debug.Log(hit.transform.name + " traverse le rayon");
            //Debug.Log(hit.transform.name + " est à " + hit.distance + " unités de la caméra");
            pt.transform.position = hit.point;

            //Make the raycast hit the center of the object
            
        }
    }
}
