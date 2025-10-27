using UnityEngine;

/// <summary>
/// Manages landmark audio (Clock and HVAC) - toggling them on/off and controlling their volume.
/// </summary>
public class LandmarksManager : MonoBehaviour
{
    [Header("Landmark GameObjects")]
    [Tooltip("The GameObject in the scene that emits the clock sound")]
    public GameObject clockGameObject;

    [Tooltip("The GameObject in the scene that emits the HVAC sound")]
    public GameObject hvacGameObject;

    [Header("Wwise Events")]
    public AK.Wwise.Event clockEvent; // BetterClock event
    public AK.Wwise.Event hvacEvent;  // HVAC event

    [Header("Wwise RTPCs")]
    public AK.Wwise.RTPC clockVolumeRTPC;
    public AK.Wwise.RTPC hvacVolumeRTPC;

    [Header("Default Settings")]
    public float defaultClockVolume = 50f;
    public float defaultHVACVolume = 50f;

    private bool isClockEnabled = false;  // Was: true
    private bool isHVACEnabled = false;   // Was: true
    private uint clockPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
    private uint hvacPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

    public static LandmarksManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set initial RTPC values
        if (clockVolumeRTPC != null && clockGameObject != null)
        {
            clockVolumeRTPC.SetValue(clockGameObject, defaultClockVolume);
        }

        if (hvacVolumeRTPC != null && hvacGameObject != null)
        {
            hvacVolumeRTPC.SetValue(hvacGameObject, defaultHVACVolume);
        }

        // Start both sounds playing
        //StartClockSound();
        //StartHVACSound();
    }

    // ==================== CLOCK CONTROLS ====================

    public void SetClockEnabled(bool enabled)
    {
        isClockEnabled = enabled;

        if (enabled)
        {
            StartClockSound();
        }
        else
        {
            StopClockSound();
        }

        Debug.Log($"Clock sound {(enabled ? "enabled" : "disabled")}");
    }

    public void SetClockVolume(float volume)
    {
        if (clockVolumeRTPC != null && clockGameObject != null)
        {
            clockVolumeRTPC.SetValue(clockGameObject, volume);
            Debug.Log($"Clock volume set to: {volume}");
        }
    }

    private void StartClockSound()
    {
        if (clockEvent != null && clockGameObject != null)
        {
            // Stop any existing instance first
            if (clockPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                AkSoundEngine.StopPlayingID(clockPlayingID);
            }

            clockPlayingID = clockEvent.Post(clockGameObject);
            Debug.Log($"Started Clock sound, PlayingID: {clockPlayingID}");
        }
    }

    private void StopClockSound()
    {
        if (clockPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(clockPlayingID);
            clockPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            Debug.Log("Stopped Clock sound");
        }
    }

    // ==================== HVAC CONTROLS ====================

    public void SetHVACEnabled(bool enabled)
    {
        isHVACEnabled = enabled;

        if (enabled)
        {
            StartHVACSound();
        }
        else
        {
            StopHVACSound();
        }

        Debug.Log($"HVAC sound {(enabled ? "enabled" : "disabled")}");
    }

    public void SetHVACVolume(float volume)
    {
        if (hvacVolumeRTPC != null && hvacGameObject != null)
        {
            hvacVolumeRTPC.SetValue(hvacGameObject, volume);
            Debug.Log($"HVAC volume set to: {volume}");
        }
    }

    private void StartHVACSound()
    {
        if (hvacEvent != null && hvacGameObject != null)
        {
            // Stop any existing instance first
            if (hvacPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                AkSoundEngine.StopPlayingID(hvacPlayingID);
            }

            hvacPlayingID = hvacEvent.Post(hvacGameObject);
            Debug.Log($"Started HVAC sound, PlayingID: {hvacPlayingID}");
        }
    }

    private void StopHVACSound()
    {
        if (hvacPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(hvacPlayingID);
            hvacPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            Debug.Log("Stopped HVAC sound");
        }
    }

    // ==================== GETTERS ====================

    public bool IsClockEnabled()
    {
        return isClockEnabled;
    }

    public bool IsHVACEnabled()
    {
        return isHVACEnabled;
    }
}