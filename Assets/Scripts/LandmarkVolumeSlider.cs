using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Specialized slider for controlling landmark volume through LandmarksManager.
/// The feedback sound will play at the same volume level as the slider value for preview.
/// </summary>
public class LandmarkVolumeSlider : MonoBehaviour
{
    public enum LandmarkType
    {
        Clock,
        HVAC
    }

    [Header("Landmark Configuration")]
    public LandmarkType landmarkType;

    [Header("Slider Settings")]
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
        _slider.value = defaultValue;
        UpdateLandmarkVolume(defaultValue);
        _isInitialized = true;
        Debug.Log($"LandmarkVolumeSlider: Initialized {landmarkType} slider at volume {defaultValue}");
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

    private void OnSliderValueChanged(float value)
    {
        if (!_isInitialized) return;

        Debug.Log($"LandmarkVolumeSlider: {landmarkType} slider changed to {value}");

        UpdateLandmarkVolume(value);

        // Stop previous feedback sound and play new one at the current volume level
        PlayFeedbackSound(value);
    }

    private void PlayFeedbackSound(float volume)
    {
        if (feedbackSound != null && feedbackSound.IsValid())
        {
            LandmarksManager manager = LandmarksManager.Instance;
            if (manager == null) return;

            // Stop any existing feedback sound first
            if (_feedbackPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                AkSoundEngine.StopPlayingID(_feedbackPlayingID);
            }

            // Set the appropriate RTPC on THIS GameObject (the slider) to match the volume
            // This makes the feedback sound play at the same volume level as the landmark
            AK.Wwise.RTPC rtpcToUse = null;

            switch (landmarkType)
            {
                case LandmarkType.Clock:
                    rtpcToUse = manager.clockVolumeRTPC;
                    break;
                case LandmarkType.HVAC:
                    rtpcToUse = manager.hvacVolumeRTPC;
                    break;
            }

            if (rtpcToUse != null)
            {
                // Set the RTPC value on the slider GameObject
                rtpcToUse.SetValue(gameObject, volume);
            }

            // Post the feedback sound - it will use the RTPC value we just set
            _feedbackPlayingID = feedbackSound.Post(gameObject);
        }
    }

    private void StopFeedbackSound()
    {
        if (_feedbackPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(_feedbackPlayingID);
            _feedbackPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    private void UpdateLandmarkVolume(float volume)
    {
        LandmarksManager manager = LandmarksManager.Instance;
        if (manager == null)
        {
            Debug.LogError("LandmarkVolumeSlider: LandmarksManager.Instance is null!");
            return;
        }

        switch (landmarkType)
        {
            case LandmarkType.Clock:
                manager.SetClockVolume(volume);
                break;
            case LandmarkType.HVAC:
                manager.SetHVACVolume(volume);
                break;
        }
    }
}