using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersStateManagement : MonoBehaviour
{
    PlayersBaseState currentPlayerState;

    public PlayersPlayState playState = new PlayersPlayState();
    public PlayersSpectateState spectateState = new PlayersSpectateState();
    public PlayersShootState shootState = new PlayersShootState();

    public GameObject WhiteBall;
    public GameObject Camera;
    public float distance;
    public float force;

    // Start is called before the first frame update
    void Start()
    {
        currentPlayerState = playState;

        playState.EnterState(this);
    }

    // Update is called once per frame
    void Update()
    {
        currentPlayerState.UpdateState(this);
    }

    public void SwitchState(PlayersBaseState newState)
    {
        currentPlayerState = newState;
        newState.EnterState(this);
    }

    public Vector3 getPosition()
    {
        return transform.position;
    }

    public Vector3 getVelocity()
    {
        return transform.GetComponent<Rigidbody>().velocity;
    }

    public Vector3 getDirection()
    {
        return (transform.position - WhiteBall.transform.position).normalized;
    }

    public void setPosition()
    {
        transform.position = (transform.position - WhiteBall.transform.position).normalized * distance + WhiteBall.transform.position;
    }

    public void setRotation()
    {
        transform.LookAt(WhiteBall.transform.position);
        transform.Rotate(0, 90, 90);

        turnArround();
    }

    public void turnArround()
    {
        float mouseRotationX = Input.GetAxis("Mouse X");
        float mouseRotationY = Input.GetAxis("Mouse Y");

        transform.RotateAround(WhiteBall.transform.position, Vector3.up, mouseRotationX);
        transform.RotateAround(WhiteBall.transform.position, Vector3.forward, mouseRotationY);
    }

    public void shoot()
    {
        float mouseAxisY = Input.GetAxis("Mouse Y");

        lockCamera(true);

        transform.position -= transform.up * mouseAxisY * 0.1f;
    }

    public void lockCamera(bool lockCamera)
    {
        Camera.GetComponent<Camera>().enabled = !lockCamera;
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentPlayerState.OnCollisionEnter(this, collision);
    }
}