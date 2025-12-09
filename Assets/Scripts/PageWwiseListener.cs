using UnityEngine;
using AK.Wwise;

/// <summary>
/// Wwise Listener for PageControlItem (Data-Driven Settings)
/// 
/// This listener bridges the gap between PageControlItem's OnValueChanged event
/// and Wwise's RTPC (Real-Time Parameter Control) system.
/// 
/// DESIGN PATTERN:
/// This is a "Listener" in the MVC pattern - it responds to Model changes (PageControlItem)
/// and applies them to external systems (Wwise audio engine).
/// 
/// USAGE:
/// 1. Attach this script to any GameObject in your settings page hierarchy
/// 2. In the Inspector, wire PageControlItem.OnValueChanged to this script's OnValueChanged() method
/// 3. Assign the Wwise RTPC you want to control
/// 4. Optionally assign a feedback sound that plays when the value changes
/// 
/// EXAMPLE USE CASE:
/// A "Click Volume" slider controlled by GenericPageController.
/// As the user adjusts it with arrow keys, this listener:
/// - Updates the Wwise RTPC for click volume
/// - Plays a click sound at the new volume (feedback)
/// This allows blind users to hear the effect of their adjustment in real-time.
/// 
/// COMPOSITION PATTERN:
/// This script can coexist with UIBinder on the same GameObject:
/// - UIBinder updates the visual slider (View)
/// - PageWwiseListener updates Wwise (Audio System)
/// Both respond to the same PageControlItem.OnValueChanged event.
/// </summary>
public class PageWwiseListener : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    // INSPECTOR FIELDS
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Wwise RTPC Configuration")]
    [Tooltip("The Wwise RTPC to control (e.g., 'RTPC_ClickVolume', 'RTPC_MasterVolume')")]
    public AK.Wwise.RTPC rtpcToControl;

    [Header("Audio Feedback")]
    [Tooltip("Optional: Sound to play when value changes (lets user hear the effect of their adjustment)")]
    public AK.Wwise.Event feedbackSound;

    [Tooltip("GameObject to emit sounds from. If null, uses this GameObject.")]
    public GameObject soundEmitter;

    [Header("Value Mapping (Optional)")]
    [Tooltip("Multiplier to apply before sending to Wwise (e.g., 100 to convert 0-1 to 0-100)")]
    public float valueMultiplier = 1f;

    [Tooltip("Offset to add after multiplying (e.g., -80 to convert 0-100 to -80dB to +20dB)")]
    public float valueOffset = 0f;

    [Header("Debug")]
    [Tooltip("Enable detailed console logging for troubleshooting")]
    [SerializeField] private bool enableDebugLogs = false;

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════════════════

    private bool isInitialized = false;

    // ═══════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Validate critical references
        if (rtpcToControl == null || !rtpcToControl.IsValid())
        {
            Debug.LogError($"[PageWwiseListener - {gameObject.name}] 'rtpcToControl' is not assigned or invalid! This component will not function.");
            this.enabled = false;
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[PageWwiseListener - {gameObject.name}] Initialized with RTPC: {rtpcToControl.Name}");
    }

    private void Start()
    {
        // Set default sound emitter if not assigned
        if (soundEmitter == null)
        {
            soundEmitter = this.gameObject;
            
            if (enableDebugLogs)
                Debug.Log($"[PageWwiseListener - {gameObject.name}] soundEmitter not assigned, using self: {soundEmitter.name}");
        }

        isInitialized = true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC API - Wire these methods to PageControlItem.OnValueChanged
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Main listener method: Updates Wwise RTPC and plays feedback sound.
    /// Wire this to PageControlItem.OnValueChanged in the Inspector.
    /// </summary>
    /// <param name="rawValue">The value from PageControlItem (typically 0-1 for sliders)</param>
    public void OnValueChanged(float rawValue)
    {
        // Don't process changes before initialization is complete
        if (!isInitialized)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[PageWwiseListener - {gameObject.name}] OnValueChanged called before initialization, ignoring");
            return;
        }

        // Apply value mapping (multiplier + offset)
        float mappedValue = (rawValue * valueMultiplier) + valueOffset;

        // Update the Wwise RTPC to the new value
        rtpcToControl.SetValue(soundEmitter, mappedValue);

        if (enableDebugLogs)
            Debug.Log($"[PageWwiseListener - {gameObject.name}] RTPC '{rtpcToControl.Name}' updated: {rawValue:F2} → {mappedValue:F2} on '{soundEmitter.name}'");

        // Play the feedback sound if assigned
        // This allows users to hear the effect of their adjustment in real-time
        PlayFeedbackSound();
    }

    /// <summary>
    /// Alternative method name for clarity when wiring in Inspector.
    /// Functionally identical to OnValueChanged().
    /// </summary>
    public void UpdateRTPC(float rawValue)
    {
        OnValueChanged(rawValue);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Plays the feedback sound if assigned and valid.
    /// </summary>
    private void PlayFeedbackSound()
    {
        if (feedbackSound != null && feedbackSound.IsValid())
        {
            feedbackSound.Post(soundEmitter);

            if (enableDebugLogs)
                Debug.Log($"[PageWwiseListener - {gameObject.name}] Feedback sound '{feedbackSound.Name}' posted on '{soundEmitter.name}'");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC API - Manual Control (Optional)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manually set the RTPC to a specific value without firing feedback sound.
    /// Useful for initialization or loading saved settings silently.
    /// </summary>
    /// <param name="rawValue">The raw value to set (will be mapped)</param>
    /// <param name="playFeedback">Should the feedback sound play?</param>
    public void SetValueSilent(float rawValue, bool playFeedback = false)
    {
        if (!isInitialized) return;

        float mappedValue = (rawValue * valueMultiplier) + valueOffset;
        rtpcToControl.SetValue(soundEmitter, mappedValue);

        if (enableDebugLogs)
            Debug.Log($"[PageWwiseListener - {gameObject.name}] RTPC '{rtpcToControl.Name}' set silently to {mappedValue:F2}");

        if (playFeedback)
            PlayFeedbackSound();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DEBUG HELPERS (Editor Only)
    // ═══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Provide helpful warnings in the Inspector
        if (rtpcToControl == null || !rtpcToControl.IsValid())
        {
            Debug.LogWarning($"[PageWwiseListener - {gameObject.name}] 'rtpcToControl' is not assigned! This component requires a valid Wwise RTPC.");
        }

        // Warn about unusual value mappings
        if (valueMultiplier == 0f)
        {
            Debug.LogWarning($"[PageWwiseListener - {gameObject.name}] valueMultiplier is 0! This will always send 0 to Wwise.");
        }
    }
#endif
}
