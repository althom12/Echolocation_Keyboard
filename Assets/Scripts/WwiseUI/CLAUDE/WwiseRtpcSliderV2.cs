using UnityEngine;
using UnityEngine.UI;
using AK.Wwise;

/// <summary>
/// Wwise RTPC Slider Controller (V2)
/// 
/// A reusable component that links a Unity UI Slider to a Wwise RTPC (Real-Time Parameter Control).
/// This allows users to control audio parameters (volume, pitch, etc.) in real-time via the UI.
/// 
/// IMPORTANT: This component MUST be placed on the same GameObject as the Slider component.
/// 
/// COMPOSITION PATTERN:
/// For complete slider audio feedback, use BOTH components on the same GameObject:
///   - WwiseRtpcSliderV2: Handles RTPC control + value change feedback sound
///   - WwiseUIElementV2: Handles selection audio when navigating TO the slider
/// 
/// FUNCTIONALITY:
/// - Initialization: Sets both the slider's visual position and the Wwise RTPC to a default value
/// - On Value Change: Updates the RTPC and plays an optional feedback sound in real-time
/// 
/// USE CASE EXAMPLE:
/// A "Click Volume" slider that lets blind users adjust click sound volume.
/// As they drag the slider, they hear the click sound at the current volume level,
/// allowing them to find their preferred setting through audio feedback alone.
/// </summary>
public class WwiseRtpcSliderV2 : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ═══════════════════════════════════════════════════════════════════

    [Header("RTPC Configuration")]
    [Tooltip("The Wwise RTPC to control (e.g., 'RTPC_ClickVolume')")]
    public AK.Wwise.RTPC rtpcToControl;

    [Tooltip("The default value for the RTPC and slider at startup")]
    public float defaultValue = 50f;

    [Header("Audio Feedback")]
    [Tooltip("Optional: Sound to play when slider value changes (lets user hear the effect of their adjustment)")]
    public AK.Wwise.Event feedbackSound;

    [Tooltip("GameObject to emit the feedback sound from. If null, uses this GameObject.")]
    public GameObject soundEmitter;

    // ═══════════════════════════════════════════════════════════════════
    // PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════════════

    private Slider slider;
    private bool isInitialized = false;

    // ═══════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Cache the Slider component on this GameObject
        slider = GetComponent<Slider>();

        // Validate critical references
        if (slider == null)
        {
            Debug.LogError($"[WwiseRtpcSliderV2] '{gameObject.name}': No Slider component found! This component must be on the same GameObject as a Slider. Disabling.");
            this.enabled = false;
            return;
        }

        if (rtpcToControl == null || !rtpcToControl.IsValid())
        {
            Debug.LogError($"[WwiseRtpcSliderV2] '{gameObject.name}': 'rtpcToControl' is not assigned or invalid! Disabling component.");
            this.enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Set default sound emitter if not assigned
        if (soundEmitter == null)
        {
            soundEmitter = this.gameObject;
        }

        // Initialize slider and RTPC to default value
        InitializeSliderAndRTPC();
    }

    private void OnEnable()
    {
        // Subscribe to slider value changes
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes both the slider visual and the Wwise RTPC to the default value.
    /// This ensures the UI and audio parameter are in sync from the start.
    /// </summary>
    private void InitializeSliderAndRTPC()
    {
        // Set the slider's visual position to the default value
        // NOTE: Ensure the slider's Min/Max values in the Inspector are configured correctly!
        slider.value = defaultValue;

        // Set the RTPC in Wwise to match
        // This sets the parameter on the specific soundEmitter GameObject
        rtpcToControl.SetValue(soundEmitter, defaultValue);

        // Mark as initialized
        isInitialized = true;

        Debug.Log($"[WwiseRtpcSliderV2] '{gameObject.name}': Initialized RTPC '{rtpcToControl.Name}' to {defaultValue} on emitter '{soundEmitter.name}'");
    }

    // ═══════════════════════════════════════════════════════════════════
    // SLIDER EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called automatically by the Slider whenever its value changes.
    /// Updates the Wwise RTPC and plays optional feedback sound.
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        // Don't process changes before initialization is complete
        if (!isInitialized) return;

        // Update the Wwise RTPC to the new value
        rtpcToControl.SetValue(soundEmitter, value);

        // Play the feedback sound if assigned
        // This allows users to hear the effect of their adjustment in real-time
        if (feedbackSound != null && feedbackSound.IsValid())
        {
            feedbackSound.Post(soundEmitter);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC API (Optional - for external control)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manually set the slider and RTPC to a specific value.
    /// Useful for loading saved settings or resetting to defaults.
    /// </summary>
    public void SetValue(float newValue)
    {
        if (slider != null)
        {
            slider.value = newValue; // This will trigger OnSliderValueChanged
        }
    }

    /// <summary>
    /// Get the current slider/RTPC value.
    /// </summary>
    public float GetValue()
    {
        return slider != null ? slider.value : defaultValue;
    }

    // ═══════════════════════════════════════════════════════════════════
    // DEBUG HELPERS (Optional - can be removed in production)
    // ═══════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Provide helpful warnings in the Inspector
        if (rtpcToControl == null || !rtpcToControl.IsValid())
        {
            Debug.LogWarning($"[WwiseRtpcSliderV2] '{gameObject.name}': 'rtpcToControl' is not assigned!");
        }

        // Warn if there's no Slider component on this GameObject
        if (GetComponent<Slider>() == null)
        {
            Debug.LogWarning($"[WwiseRtpcSliderV2] '{gameObject.name}': No Slider component found on this GameObject! This component requires a Slider.");
        }

        // Validate default value is within typical RTPC ranges
        if (defaultValue < 0)
        {
            Debug.LogWarning($"[WwiseRtpcSliderV2] '{gameObject.name}': defaultValue is negative ({defaultValue}). Ensure this is intentional.");
        }
    }
#endif
}
