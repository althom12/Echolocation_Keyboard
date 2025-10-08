using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    // ... (All your existing variables are here, no changes)
    [Header("Wwise Emitters")]
    public GameObject ClickEmitter;
    public GameObject FootstepEmitter;
    [Header("Wwise Events")]
    public AK.Wwise.Event EcholocationSound;
    public AK.Wwise.Event FootstepSound;
    [Tooltip("Plays when pitch adjustment mode is activated.")]
    public AK.Wwise.Event PA_Settings_Opened;
    [Tooltip("Plays when pitch adjustments are confirmed/closed.")]
    public AK.Wwise.Event PA_Settings_Confirmed;
    public AK.Wwise.Event CollisionSound;
    [Header("Wwise RTPCs")]
    [Tooltip("The EXACT name of the Pitch RTPC in your Wwise project (e.g., 'click_pitch').")]
    public string ClickPitchRTPC_Name = "click_pitch";
    [Header("Footstep Settings")]
    public float FootstepInterval = 0.5f;
    [Header("Collision Settings")]
    [Tooltip("How long to wait (in seconds) before another collision sound can play.")]
    public float CollisionCooldown = 5.0f;
    [Header("Echolocation Pitch Settings")]
    [Tooltip("How quickly the scroll wheel adjusts the pitch.")]
    [SerializeField] private float scrollSensitivity = 500f;

    // --- The 'pitchAdjustmentEnabled' variable has been REMOVED ---

    private enum ControlState { Normal, AdjustingPitch }
    private ControlState _currentState = ControlState.Normal;
    private float _currentPitchValue = 0f;
    private float _footstepTimer;
    private float _collisionCooldownTimer;

    // ... (Start, Update, and OnControllerColliderHit are unchanged) ...
    private void Start()
    {
        if (ClickEmitter == null) ClickEmitter = this.gameObject;
        if (FootstepEmitter == null) FootstepEmitter = this.gameObject;
        if (!string.IsNullOrEmpty(ClickPitchRTPC_Name))
        {
            AkSoundEngine.SetRTPCValue(ClickPitchRTPC_Name, _currentPitchValue);
        }
        _footstepTimer = FootstepInterval;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_currentState == ControlState.Normal)
            {
                EcholocationSound?.Post(ClickEmitter);
            }
        }
        HandlePitchAdjustmentInput();
        if (_collisionCooldownTimer > 0)
        {
            _collisionCooldownTimer -= Time.deltaTime;
        }
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the cooldown is over AND if the object has either the "Wall" OR "Obstacle" tag.
        if (_collisionCooldownTimer <= 0 && (hit.gameObject.CompareTag("Wall") || hit.gameObject.CompareTag("Obstacle")))
        {
            CollisionSound?.Post(gameObject);
            _collisionCooldownTimer = CollisionCooldown;
        }
    }

    private void HandlePitchAdjustmentInput()
    {
        // --- The 'if (pitchAdjustmentEnabled == false) return;' line has been REMOVED ---
        // This means the code below will always run.

        if (Input.GetMouseButtonDown(2))
        {
            if (_currentState == ControlState.Normal)
            {
                _currentState = ControlState.AdjustingPitch;
                Debug.Log("Entered Pitch Adjust Mode.");
                PA_Settings_Opened?.Post(ClickEmitter);
            }
            else
            {
                _currentState = ControlState.Normal;
                Debug.Log("Exited Pitch Adjust Mode.");
                PA_Settings_Confirmed?.Post(ClickEmitter);
            }
        }
        if (_currentState == ControlState.AdjustingPitch)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                _currentPitchValue += scrollInput * scrollSensitivity;
                _currentPitchValue = Mathf.Clamp(_currentPitchValue, -1200f, 1200f);
                if (!string.IsNullOrEmpty(ClickPitchRTPC_Name))
                {
                    AkSoundEngine.SetRTPCValue(ClickPitchRTPC_Name, _currentPitchValue);
                }
                EcholocationSound?.Post(ClickEmitter);
                Debug.Log($"Current Pitch Value: {_currentPitchValue}");
            }
        }
    }

    // ... (HandleFootsteps is unchanged) ...
    public void HandleFootsteps(bool isMoving)
    {
        if (isMoving)
        {
            _footstepTimer += Time.deltaTime;
            if (_footstepTimer >= FootstepInterval)
            {
                FootstepSound?.Post(FootstepEmitter);
                _footstepTimer = 0f;
            }
        }
        else
        {
            _footstepTimer = FootstepInterval;
        }
    }

    // This function is still needed to reset the pitch.
    public void ResetPitchRTPC()
    {
        _currentPitchValue = 0f;
        if (!string.IsNullOrEmpty(ClickPitchRTPC_Name))
        {
            AkSoundEngine.SetRTPCValue(ClickPitchRTPC_Name, _currentPitchValue);
            Debug.Log("Pitch RTPC has been reset to default.");
        }
    }
}