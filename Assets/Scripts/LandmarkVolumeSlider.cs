using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Specialized slider for controlling landmark volume through LandmarksManager.
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
    [Tooltip("Optional sound to play when adjusting the slider")]
    public AK.Wwise.Event feedbackSound;

    private Slider _slider;
    private bool _isInitialized = false;

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
    }

    private void OnSliderValueChanged(float value)
    {
        if (!_isInitialized) return;

        Debug.Log($"LandmarkVolumeSlider: {landmarkType} slider changed to {value}");

        UpdateLandmarkVolume(value);

        // Play feedback sound on the slider GameObject
        if (feedbackSound != null && feedbackSound.IsValid())
        {
            feedbackSound.Post(gameObject);
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