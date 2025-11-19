using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Specialized slider for controlling landmark volume through LandmarksManager.
/// 
/// DUAL-EMITTER PATTERN (CRITICAL for blind user UX):
/// - Spatial Emitter: The landmark GameObject in the world (e.g., Clock at position X,Y,Z)
/// - 2D Feedback Emitter: This slider GameObject (always audible, centered, non-spatial)
/// 
/// When user adjusts slider:
/// 1. RTPC is set on BOTH the spatial emitter (affects landmark in world) AND slider (affects feedback)
/// 2. Feedback sound plays from slider GameObject (user hears it clearly regardless of position)
/// 3. User can adjust landmarks from anywhere in the world and hear the result
/// 
/// REFACTORED: Now uses landmarkIndex instead of enum for scalability.
/// </summary>
public class LandmarkVolumeSlider : MonoBehaviour, IDeselectHandler
{
    [Header("Landmark Configuration")]
    [Tooltip("Index of the landmark this slider controls (0 = first in LandmarksManager array)")]
    public int landmarkIndex = 0;

    [Header("Slider Settings")]
    [Tooltip("Default volume value (0-100)")]
    public float defaultValue = 50f;

    [Header("Audio Feedback")]
    [Tooltip("Optional sound to play when adjusting the slider (will match the volume level)")]
    public AK.Wwise.Event feedbackSound;

    private Slider _slider;
    private bool _isInitialized = false;

    // Track the playing ID of the feedback sound
    private uint _feedbackPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        if (_slider == null)
        {
            Debug.LogError($"LandmarkVolumeSlider on '{gameObject.name}': No Slider component found! Disabling.");
            this.enabled = false;
        }
    }

    private void Start()
    {
        // Set slider to default value
        _slider.value = defaultValue;

        // Initialize landmark volume in manager
        UpdateLandmarkVolume(defaultValue);

        _isInitialized = true;

        // Get landmark name for better logging
        string landmarkName = GetLandmarkName();
        Debug.Log($"LandmarkVolumeSlider: Initialized '{landmarkName}' (index {landmarkIndex}) slider at volume {defaultValue}");
    }

    private void OnEnable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnDisable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        // Stop any playing feedback sound when disabled
        StopFeedbackSound();
    }

    /// <summary>
    /// Called by Unity's EventSystem when this slider is deselected (user navigates away).
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        // Stop the feedback sound when user navigates to another UI element
        StopFeedbackSound();

        string landmarkName = GetLandmarkName();
        Debug.Log($"LandmarkVolumeSlider: '{landmarkName}' (index {landmarkIndex}) slider deselected, stopping feedback sound.");
    }

    private void OnSliderValueChanged(float value)
    {
        if (!_isInitialized) return;

        string landmarkName = GetLandmarkName();
        Debug.Log($"LandmarkVolumeSlider: '{landmarkName}' (index {landmarkIndex}) slider changed to {value}");

        // Update the landmark volume in the world
        UpdateLandmarkVolume(value);

        // Play feedback sound at the current volume level (DUAL-EMITTER PATTERN)
        PlayFeedbackSound(value);
    }

    /// <summary>
    /// DUAL-EMITTER PATTERN: Plays feedback sound from THIS slider GameObject (2D, always audible).
    /// Sets RTPC on slider so feedback plays at same volume as the landmark would.
    /// This lets blind users hear the volume adjustment regardless of world position.
    /// </summary>
    private void PlayFeedbackSound(float volume)
    {
        if (feedbackSound == null || !feedbackSound.IsValid())
        {
            return;
        }

        LandmarksManager manager = LandmarksManager.Instance;
        if (manager == null) return;

        // Stop any existing feedback sound first
        if (_feedbackPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(_feedbackPlayingID);
        }

        // Get the landmark binding to access the RTPC
        LandmarkUIBinding binding = manager.GetLandmark(landmarkIndex);
        if (binding == null || binding.landmarkDefinition == null)
        {
            Debug.LogWarning($"LandmarkVolumeSlider: Could not get landmark binding for index {landmarkIndex}");
            return;
        }

        // CRITICAL: Set RTPC on THIS slider GameObject (not the spatial emitter!)
        // This makes the feedback sound play at the volume the user is setting
        if (binding.landmarkDefinition.volumeRTPC != null)
        {
            binding.landmarkDefinition.volumeRTPC.SetValue(gameObject, volume);
        }

        // Post the feedback sound from THIS slider GameObject (2D, non-spatial)
        _feedbackPlayingID = feedbackSound.Post(gameObject);
    }

    private void StopFeedbackSound()
    {
        if (_feedbackPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(_feedbackPlayingID);
            _feedbackPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    /// <summary>
    /// Updates the landmark's volume in the world (SPATIAL emitter).
    /// This is separate from the feedback sound.
    /// </summary>
    private void UpdateLandmarkVolume(float volume)
    {
        LandmarksManager manager = LandmarksManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LandmarkVolumeSlider: LandmarksManager.Instance is null!");
            return;
        }

        // This sets the RTPC on the SPATIAL emitter (the Clock/HVAC GameObject in the world)
        manager.SetLandmarkVolume(landmarkIndex, volume);
    }

    /// <summary>
    /// Helper method to get landmark name for logging.
    /// </summary>
    private string GetLandmarkName()
    {
        if (LandmarksManager.Instance == null) return "Unknown";

        LandmarkUIBinding binding = LandmarksManager.Instance.GetLandmark(landmarkIndex);
        if (binding != null && binding.landmarkDefinition != null)
        {
            return binding.landmarkDefinition.landmarkName;
        }

        return $"Index_{landmarkIndex}";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (landmarkIndex < 0)
        {
            Debug.LogWarning($"[{gameObject.name}] landmarkIndex is negative! Setting to 0.");
            landmarkIndex = 0;
        }

        if (defaultValue < 0 || defaultValue > 100)
        {
            Debug.LogWarning($"[{gameObject.name}] defaultValue should be between 0-100. Current: {defaultValue}");
        }
    }
#endif
}