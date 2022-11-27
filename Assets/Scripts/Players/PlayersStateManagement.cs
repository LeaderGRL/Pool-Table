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
    public Vector3 CameraOffset;
    public float CameraDistance;

    private Vector3 velocity;

    private void Awake()
    {
    }
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

    void LateUpdate() 
    {
        currentPlayerState.LateUpdateState(this);
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
        //transform.position = WhiteBall.transform.position + getDirection() * distance;
        //transform.position = (transform.position - WhiteBall.transform.position).normalized * distance + WhiteBall.transform.position;
        transform.position = Vector3.SmoothDamp(transform.position, (transform.position - WhiteBall.transform.position).normalized * distance + WhiteBall.transform.position, ref velocity, 1f);
    }

    public void setRotation()
    {
        //transform.LookAt(WhiteBall.transform.position);

        var targetRotation = Quaternion.LookRotation(WhiteBall.transform.position - transform.position);

        // Smoothly rotate towards the target point.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        //transform.Rotate(0, 90, 90);
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + 90), Time.deltaTime * 2f);
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

        transform.position -= transform.forward * mouseAxisY * 0.1f;
    }

    public void lockCamera(bool lockCamera)
    {
        //Camera.GetComponent<Camera>().enabled = !lockCamera;
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentPlayerState.OnCollisionEnter(this, collision);
    }
}
