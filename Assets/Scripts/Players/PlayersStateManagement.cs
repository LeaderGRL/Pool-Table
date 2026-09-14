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
    public GameObject Cue_Camera;
    public float distance;
    public float spin;
    public Vector3 CameraOffset;
    public float CameraDistance;
    public Behaviour aimingController;
    public Behaviour shotPowerController;

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

    void OnMouseDown()
    {
        currentPlayerState.OnMouseDown(this);
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
        return transform.GetComponent<Rigidbody>().linearVelocity;
    }

    public void SetAimingEnabled(bool enabled)
    {
        if (aimingController != null)
        {
            aimingController.enabled = enabled;
        }
    }

    public void SetShotPowerEnabled(bool enabled)
    {
        if (shotPowerController != null)
        {
            shotPowerController.enabled = enabled;
        }
    }

    public bool IsShotPowerEnabled()
    {
        return shotPowerController != null && shotPowerController.enabled;
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
