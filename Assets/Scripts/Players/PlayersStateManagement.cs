using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersStateManagement : MonoBehaviour
{
    PlayersBaseState currentPlayerState;

    public static PlayersStateManagement Instance;
    public PlayersPlayState playState = new PlayersPlayState();
    public PlayersSpectateState spectateState = new PlayersSpectateState();
    public PlayersShootState shootState = new PlayersShootState();

    public GameObject WhiteBall;
    public GameObject Cam;
    public float distance;
    public float force;
    public float spin;
    public Vector3 CameraOffset;
    public float CameraDistance;

    public float rotationSpeed = 5.0f;
    public float angleLimit = 45.0f;

    private float angle = 0.0f;

    private Vector3 velocity;
    private float mouseRotationX = 0;
    private float mouseRotationY = 0;

    private void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        mouseRotationX = transform.eulerAngles.x;
        mouseRotationY = transform.eulerAngles.y;

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
        transform.position = Vector3.MoveTowards(transform.position, (transform.position - WhiteBall.transform.position).normalized * distance + WhiteBall.transform.position, 1f);
    }

    public void setRotation()
    {
        var targetRotation = Quaternion.LookRotation(WhiteBall.transform.position - transform.position);

        // Smoothly rotate towards the target point.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        
        turnArround();
    }

    public void turnArround()
    {
        mouseRotationX = Input.GetAxis("Mouse X");
        mouseRotationY = Input.GetAxis("Mouse Y");

        //Debug.Log(transform.eulerAngles.x + " , " + transform.eulerAngles.y + " , " + transform.eulerAngles.z);

 
        if (transform.eulerAngles.x < 1f)
        {
            if (mouseRotationY < 0)
            {
                transform.RotateAround(WhiteBall.transform.position, Vector3.forward, Input.GetAxis("Mouse Y"));
            }
        }
        else
        {
            transform.RotateAround(WhiteBall.transform.position, Vector3.forward, Input.GetAxis("Mouse Y"));
        }

        transform.RotateAround(WhiteBall.transform.position, Vector3.up, Input.GetAxis("Mouse X"));

        transform.rotation = Quaternion.Euler(
            Mathf.Clamp(transform.rotation.eulerAngles.x, -90f, 90f),
            transform.rotation.eulerAngles.y,
            Mathf.Clamp(transform.rotation.eulerAngles.z, -90f, 90f)
        );

    }

    public void shoot()
    {
        float mouseAxisY = Input.GetAxis("Mouse Y");

        lockCamera(true);

        transform.position -= transform.forward * mouseAxisY * 0.1f;
    }

    public void lockCamera(bool lockCamera)
    {
        //Cam.GetComponent<Cam>().enabled = !lockCamera;
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentPlayerState.OnCollisionEnter(this, collision);
    }
}
