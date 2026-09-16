using UnityEngine;

public class PlayersStateManagement : MonoBehaviour
{
    PlayersBaseState currentPlayerState;

    public static PlayersStateManagement Instance;
    public PlayersPlayState playState = new PlayersPlayState();
    public PlayersSpectateState spectateState = new PlayersSpectateState();
    public PlayersShootState shootState = new PlayersShootState();

    public GameObject WhiteBall;
    public float distance;
    public Behaviour aimingController;
    public Behaviour spinController;
    public Behaviour shotPowerController;
    public Behaviour ballInHandPlacementController;
    public Behaviour shotCameraController;
    public Behaviour spectateCameraController;

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

    public void SetSpinControlEnabled(bool enabled)
    {
        if (spinController != null)
        {
            spinController.enabled = enabled;
        }
    }

    public void SetShotCameraEnabled(bool enabled)
    {
        if (shotCameraController != null)
        {
            shotCameraController.enabled = enabled;
        }
    }

    public void SetSpectateCameraEnabled(bool enabled)
    {
        if (spectateCameraController != null)
        {
            spectateCameraController.enabled = enabled;
        }
    }

    public void ResetPointerAimingSequence()
    {
        if (aimingController != null)
        {
            aimingController.gameObject.SendMessage(
                "ResetPointerAdjustmentSequence",
                SendMessageOptions.DontRequireReceiver);
        }
    }

    public void BeginPointerYawAdjustment()
    {
        if (aimingController != null)
        {
            aimingController.gameObject.SendMessage(
                "BeginPointerYawAdjustment",
                SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool IsShotPowerEnabled()
    {
        return shotPowerController != null && shotPowerController.enabled;
    }

    public bool BeginBallInHandPlacement()
    {
        if (ballInHandPlacementController == null)
        {
            return false;
        }

        SetAimingEnabled(false);
        SetSpinControlEnabled(false);
        SetShotPowerEnabled(false);
        ballInHandPlacementController.enabled = true;
        ballInHandPlacementController.gameObject.SendMessage(
            "BeginLegacyScratchPlacement",
            SendMessageOptions.RequireReceiver);
        return true;
    }

    public bool IsBallInHandPlacementEnabled()
    {
        return ballInHandPlacementController != null && ballInHandPlacementController.enabled;
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentPlayerState.OnCollisionEnter(this, collision);
    }
}
