using UnityEngine;
using UnityEngine.UI;
using AK.Wwise;

/// <summary>
/// A reusable, decoupled component that links a Unity UI Slider 
/// to a Wwise RTPC on a SPECIFIC GameObject.
///
/// (Version 2: Now with a public default value)
///
/// This component MUST be placed on the same GameObject as the Slider.
/// </summary>
public class WwiseRtpcSlider : MonoBehaviour
{


    public AK.Wwise.RTPC rtpcToControl;


    public AK.Wwise.Event feedbackSound;


    public GameObject soundEmitter;



    public float defaultValue = 50f;

    // --- Cached References ---
    private Slider _slider;
    private bool _isInitialized = false;

    private void Awake()
    {
        // Get the Slider component on this same GameObject
        _slider = GetComponent<Slider>();

        if (rtpcToControl == null || !rtpcToControl.IsValid())
        {
            Debug.LogError($"WwiseRtpcSlider on '{gameObject.name}': 'Rtpc To Control' is not assigned. Disabling component.");
            this.enabled = false;
            return;
        }

        if (_slider == null)
        {
            Debug.LogError($"WwiseRtpcSlider on '{gameObject.name}': Could not find a 'Slider' component on this GameObject. Disabling component.");
            this.enabled = false;
        }
    }

    private void Start()
    {
        // If no sound emitter is assigned, use this GameObject (the slider)
        if (soundEmitter == null)
        {
            soundEmitter = this.gameObject;
            Debug.Log($"WwiseRtpcSlider on '{gameObject.name}': No 'soundEmitter' assigned. Defaulting to self.");
        }

        // --- Initial Configuration ---
        // We now use the public 'defaultValue' variable from the Inspector.

        // 1. Set the slider's visual value to the default
        //    (Make sure the Slider's Min/Max in the Inspector are set correctly!)
        _slider.value = defaultValue;

        // 2. Set the initial RTPC value in Wwise to match
        // We use SetValue on the specific soundEmitter object 
        rtpcToControl.SetValue(soundEmitter, defaultValue);

        Debug.Log($"WwiseRtpcSlider: Initial RTPC value for '{rtpcToControl.Name}' set to {defaultValue} on '{soundEmitter.name}'.");

        // 3. Mark as initialized
        _isInitialized = true;
    }

    private void OnEnable()
    {
        // Subscribe to the slider's OnValueChanged event [2]
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and errors
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// This method is called by the Slider's event whenever the
    /// user drags the slider.
    /// </summary>
    /// <param name="value">The new value from the slider.</param>
    private void OnSliderValueChanged(float value)
    {
        if (!_isInitialized) return;

        Debug.Log($"Slider value changed to: {value}. Attempting to set RTPC and play sound.");

        // 1. Set the Wwise RTPC to the new value on the soundEmitter 
        rtpcToControl.SetValue(soundEmitter, value);

        // 2. Play the feedback sound (if one is provided) on the soundEmitter
        if (feedbackSound != null && feedbackSound.IsValid())
        {
            feedbackSound.Post(soundEmitter);
        }
        else if (feedbackSound == null)
        {
            Debug.LogWarning("WwiseRtpcSlider: 'Feedback Sound' is not assigned in the Inspector. No feedback will play.");
        }
    }
}