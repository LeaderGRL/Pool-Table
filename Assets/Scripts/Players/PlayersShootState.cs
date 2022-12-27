using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersShootState : PlayersBaseState
{

    public Vector3 delta = Vector3.zero;
    private Vector3 lastPos = Vector3.zero;
    public override void EnterState(PlayersStateManagement player)
    {
        Debug.Log(GameManager.instance.getCurrentPlayer().score.getScore());

    }

    public override void UpdateState(PlayersStateManagement player)
    {
        player.shoot();
    }

    public override void FixedUpdateState(PlayersStateManagement player)
    {
        
    }

    public override void LateUpdateState(PlayersStateManagement player)
    {

    }
    public override void OnCollisionEnter(PlayersStateManagement player, Collision collision)
    {
        // Get the current mouse position
        //Vector3 mousePosition = Input.mousePosition;

        // Normalize the mouse position to a value between 0 and 1
        float normalizedMousePosition = Mathf.Clamp(-Input.GetAxis("Mouse Y"), -1, 1);


        // Convert the normalized mouse position to a shot strength between 0 and 100
        float strength = normalizedMousePosition * 100;


        // Calculate the force to apply based on the direction and strength of the shot
        Vector3 force = player.transform.forward * strength ;

        // Apply the force to the ball
        collision.rigidbody.AddForceAtPosition(force, Vector3.zero, ForceMode.Impulse);
        //collision.rigidbody.AddForce(force, ForceMode.Impulse);

        //BallStateManager.instance.ApplySpin(collision.rigidbody, player.transform.up, strength);


        //collision.rigidbody.AddForce(player.transform.forward * -Input.GetAxis("Vertical") * 10 * player.force, ForceMode.Impulse);

        //add torque
        //collision.rigidbody.AddTorque(player.transform.up * -Input.GetAxis("Mouse Y") * 10 * player.spin, ForceMode.Impulse);


        //if (collision.gameObject.tag == "white")
        //{
        //    //Debug.Log("HIT THE WHITE BALL !");
        //    player.SwitchState(player.spectateState);
        //}
    }

    public override void OnMouseDown(PlayersStateManagement player)
    {

    }
}
