/*
 * TutorialAudioController.cs (DEBUG VERSION WITH EXTENSIVE LOGGING)
 * 
 * This version has comprehensive debug logging to troubleshoot issues.
 * Every action logs to the console with clear markers.
 */

using UnityEngine;

public class TutorialAudioController : MonoBehaviour
{
    public static TutorialAudioController Instance { get; private set; }

    private bool isTutorialPaused = false;

    // State Group and State names (must match Wwise exactly)
    private const string STATE_GROUP = "Tutorial_State";
    private const string STATE_PLAYING = "Playing";
    private const string STATE_PAUSED = "Paused";

    void Awake()
    {
        Debug.Log("========================================");
        Debug.Log("TutorialAudioController: Awake() called");
        Debug.Log($"GameObject name: {gameObject.name}");
        Debug.Log($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("? TutorialAudioController: Singleton created and marked DontDestroyOnLoad");
            Debug.Log($"? Instance reference set: {Instance != null}");
        }
        else
        {
            Debug.Log("? TutorialAudioController: Duplicate instance found, destroying this one");
            Destroy(gameObject);
            return;
        }

        Debug.Log("========================================");
    }

    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("TutorialAudioController: Start() called");
        Debug.Log($"Current pause state: {isTutorialPaused}");
        Debug.Log($"State Group Name: '{STATE_GROUP}'");
        Debug.Log($"State Names: '{STATE_PLAYING}', '{STATE_PAUSED}'");

        // Test if Wwise is initialized
        if (AkSoundEngine.IsInitialized())
        {
            Debug.Log("? Wwise is initialized");
        }
        else
        {
            Debug.LogError("? WWISE IS NOT INITIALIZED! This is a critical error.");
        }

        // Set initial state to Playing
        Debug.Log("Attempting to set initial state to Playing...");
        AKRESULT result = AkSoundEngine.SetState(STATE_GROUP, STATE_PLAYING);
        Debug.Log($"Initial state set result: {result}");

        if (result == AKRESULT.AK_Success)
        {
            Debug.Log("? Successfully set initial Tutorial_State to Playing");
        }
        else
        {
            Debug.LogError($"? FAILED to set initial state. Error: {result}");
            Debug.LogError("Check that 'Tutorial_State' State Group exists in Wwise!");
        }

        Debug.Log("========================================");
    }

    void Update()
    {
        // Check for Numpad 0 input
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            Debug.Log("========================================");
            Debug.Log($"? NUMPAD 0 PRESSED at Time: {Time.time}");
            Debug.Log($"? Unscaled Time: {Time.unscaledTime}");
            Debug.Log($"? Time.timeScale: {Time.timeScale}");
            Debug.Log($"Current pause state BEFORE toggle: {isTutorialPaused}");

            ToggleTutorialPause();

            Debug.Log($"Current pause state AFTER toggle: {isTutorialPaused}");
            Debug.Log("========================================");
        }
    }

    public void ToggleTutorialPause()
    {
        Debug.Log(">>> ToggleTutorialPause() method called");
        Debug.Log($">>> State before toggle: {(isTutorialPaused ? "PAUSED" : "PLAYING")}");

        // Toggle the state
        isTutorialPaused = !isTutorialPaused;

        string targetState = isTutorialPaused ? STATE_PAUSED : STATE_PLAYING;
        Debug.Log($">>> Attempting to set Wwise state to: '{targetState}'");

        // Check if Wwise is still initialized
        if (!AkSoundEngine.IsInitialized())
        {
            Debug.LogError(">>> ? Wwise is NOT initialized! Cannot set state.");
            return;
        }

        // Set the state
        AKRESULT result = AkSoundEngine.SetState(STATE_GROUP, targetState);

        Debug.Log($">>> AkSoundEngine.SetState result: {result}");

        if (result == AKRESULT.AK_Success)
        {
            Debug.Log($">>> ??? SUCCESS! Tutorial State set to: {targetState}");

            if (isTutorialPaused)
            {
                Debug.Log(">>> ?? TUTORIAL AUDIO IS NOW PAUSED");
            }
            else
            {
                Debug.Log(">>> ?? TUTORIAL AUDIO IS NOW PLAYING");
            }
        }
        else
        {
            Debug.LogError($">>> ??? FAILED to set state! Error code: {result}");
            Debug.LogError($">>> Possible reasons:");
            Debug.LogError($">>>   1. State Group '{STATE_GROUP}' doesn't exist in Wwise");
            Debug.LogError($">>>   2. State '{targetState}' doesn't exist in the State Group");
            Debug.LogError($">>>   3. SoundBank not loaded");
            Debug.LogError($">>>   4. State names don't match exactly (case-sensitive!)");
        }
    }

    public void ForcePause()
    {
        Debug.Log("========================================");
        Debug.Log("ForcePause() called");
        Debug.Log($"Current state: {(isTutorialPaused ? "Already Paused" : "Playing")}");

        if (isTutorialPaused)
        {
            Debug.Log("Already paused, no action taken");
            Debug.Log("========================================");
            return;
        }

        isTutorialPaused = true;
        AKRESULT result = AkSoundEngine.SetState(STATE_GROUP, STATE_PAUSED);

        Debug.Log($"ForcePause SetState result: {result}");

        if (result == AKRESULT.AK_Success)
        {
            Debug.Log("? Tutorial State FORCE PAUSED");
        }
        else
        {
            Debug.LogError($"? ForcePause FAILED: {result}");
        }

        Debug.Log("========================================");
    }

    public void ForceResume()
    {
        Debug.Log("========================================");
        Debug.Log("ForceResume() called");
        Debug.Log($"Current state: {(isTutorialPaused ? "Paused" : "Already Playing")}");

        if (!isTutorialPaused)
        {
            Debug.Log("Already playing, no action taken");
            Debug.Log("========================================");
            return;
        }

        isTutorialPaused = false;
        AKRESULT result = AkSoundEngine.SetState(STATE_GROUP, STATE_PLAYING);

        Debug.Log($"ForceResume SetState result: {result}");

        if (result == AKRESULT.AK_Success)
        {
            Debug.Log("? Tutorial State FORCE RESUMED");
        }
        else
        {
            Debug.LogError($"? ForceResume FAILED: {result}");
        }

        Debug.Log("========================================");
    }

    public bool IsPaused()
    {
        Debug.Log($"IsPaused() called, returning: {isTutorialPaused}");
        return isTutorialPaused;
    }

    // Add this to check the state from Inspector or other scripts
    void OnGUI()
    {
        // Display current state in top-left corner of screen
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = isTutorialPaused ? Color.red : Color.green;

        GUI.Label(new Rect(10, 10, 400, 30),
            $"Tutorial Audio: {(isTutorialPaused ? "PAUSED ??" : "PLAYING ??")}",
            style);

        GUI.Label(new Rect(10, 40, 400, 30),
            $"Time.timeScale: {Time.timeScale}",
            style);
    }
}