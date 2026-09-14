using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersShootState : PlayersBaseState
{

    public Vector3 delta = Vector3.zero;
    private Vector3 lastPos = Vector3.zero;
    public override void EnterState(PlayersStateManagement player)
    {
        player.SetAimingEnabled(false);
        player.WhiteBall.GetComponent<BallStateManager>().hasCollide = false; //reset the collide state
        player.WhiteBall.GetComponent<BallStateManager>().hitTheGoodBall = false; //reset the hitTheGoodBall state
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
        float normalizedMousePosition = Mathf.Clamp(-LegacyMouseInput.Delta.y, -1, 1);


        ApplyShotVelocityChange(
            collision.rigidbody,
            player.transform.forward,
            normalizedMousePosition,
            player.maxShotSpeedMetersPerSecond);

        //if (collision.gameObject.tag == "white")
        //{
        //    //Debug.Log("HIT THE WHITE BALL !");
        //    player.SwitchState(player.spectateState);
        //}
    }

    public override void OnMouseDown(PlayersStateManagement player)
    {

    }

    private static void ApplyShotVelocityChange(
        Rigidbody rigidbody,
        Vector3 direction,
        float normalizedShotInput,
        float maxShotSpeedMetersPerSecond)
    {
        var clampedShotInput = Mathf.Clamp(normalizedShotInput, -1f, 1f);
        var velocityChange = direction.normalized * (clampedShotInput * maxShotSpeedMetersPerSecond);
        rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
    }
}
