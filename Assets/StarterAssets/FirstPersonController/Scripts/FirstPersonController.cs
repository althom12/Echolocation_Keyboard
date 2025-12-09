using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]

#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        public float MoveSpeed = 4.0f;
        public float SprintSpeed = 6.0f;
        public float RotationSpeed = 1.0f;
        public float SpeedChangeRate = 10.0f;

        [Header("Footstep Audio")]
        [Tooltip("Wwise footstep sound event")]
        public AK.Wwise.Event FootstepSound;
        [Tooltip("GameObject to emit footstep sounds from (leave null to use player GameObject)")]
        public GameObject FootstepEmitter;
        [Tooltip("Time interval between footstep sounds when moving (in seconds)")]
        public float FootstepInterval = 0.5f;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -70.0f;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _speed;
        private float _rotationVelocity;
        private PlayerInput _playerInput;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        // Footstep timer
        private float _footstepTimer;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse { get { return _playerInput.currentControlScheme == "KeyboardMouse"; } }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            // Get references to components in Awake() to ensure they are ready for other scripts.
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            // Set default footstep emitter if not assigned
            if (FootstepEmitter == null)
            {
                FootstepEmitter = this.gameObject;
            }

            // Initialize footstep timer
            _footstepTimer = FootstepInterval;
        }

        private void Update()
        {
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _cinemachineTargetYaw += _input.look.x * RotationSpeed * deltaTimeMultiplier;
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
            }
        }

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime));

            // ═══════════════════════════════════════════════════════════════════════
            // FOOTSTEP AUDIO SYSTEM
            // ═══════════════════════════════════════════════════════════════════════
            HandleFootsteps();
        }

        /// <summary>
        /// Handles footstep audio playback based on player movement.
        /// Respects the SettingsManager.IsFootstepsEnabled setting.
        /// </summary>
        private void HandleFootsteps()
        {
            // Check if player is moving
            bool isMoving = _input.move != Vector2.zero;

            // Check if footsteps are enabled in settings
            if (SettingsManager.Instance != null && !SettingsManager.Instance.IsFootstepsEnabled)
            {
                // Reset timer to prevent immediate step if re-enabled while moving
                _footstepTimer = FootstepInterval;
                return;
            }

            if (isMoving)
            {
                _footstepTimer += Time.deltaTime;

                if (_footstepTimer >= FootstepInterval)
                {
                    // Play footstep sound if event is assigned
                    if (FootstepSound != null && FootstepSound.IsValid())
                    {
                        FootstepSound.Post(FootstepEmitter);
                    }

                    _footstepTimer = 0f;
                }
            }
            else
            {
                // Reset timer when not moving
                _footstepTimer = FootstepInterval;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _cinemachineTargetYaw = rotation.eulerAngles.y;
            _controller.enabled = true;
        }
    }
}