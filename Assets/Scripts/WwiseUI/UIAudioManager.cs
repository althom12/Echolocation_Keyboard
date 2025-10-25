using UnityEngine;
using AK.Wwise; // Required for all Wwise integration types

public class UIAudioManager : MonoBehaviour
{
    // --- 1. SINGLETON INSTANCE (Makes it easy to call from anywhere) ---
    public static UIAudioManager Instance { get; private set; }

    // --- 2. GENERIC WWISE EVENT REFERENCES ---
    [Header("1. Main Menu Events")]

    public AK.Wwise.Event OnMenuOpenFromGame;


    public AK.Wwise.Event OnMenuCloseToGame;

    // --- You can add more categories here later (Select, Submit, Error, etc.) ---
    // [Header("2. Global Navigation")]
    // public AK.Wwise.Event OnNavigationMove;


    // --- Static variable to track the globally playing UI event ---
    private static uint _globalPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;


    private void Awake()
    {
        // Enforce the Singleton pattern (only one instance allowed)
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Utility function to stop the currently playing global sound.
    /// </summary>
    public static void StopCurrentGlobalSound()
    {
        if (_globalPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            // Stop immediately - no curve parameter needed
            AkSoundEngine.StopPlayingID(_globalPlayingID, 0);

            _globalPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    /// <summary>
    /// Posts a sound after stopping the current one and captures the new Playing ID.
    /// </summary>
    public void PostStoppableEvent(AK.Wwise.Event wwiseEvent, GameObject emitter)
    {
        StopCurrentGlobalSound();
        if (wwiseEvent != null)
        {
            // Post the event and capture the unique ID [1]
            _globalPlayingID = wwiseEvent.Post(emitter);
        }
    }

    // --- 3. PUBLIC DELEGATE FUNCTIONS (Called by other scripts) ---

    /// <summary>
    /// Posts the sound for opening the main menu from the game state.
    /// </summary>
    public void PlayMenuOpenFromGameSound(GameObject emitter)
    {
        PostStoppableEvent(OnMenuOpenFromGame, emitter);
    }

    /// <summary>
    /// Posts the sound for closing the main menu back to the game state.
    /// </summary>
    public void PlayMenuCloseToGameSound(GameObject emitter)
    {
        PostStoppableEvent(OnMenuCloseToGame, emitter);
    }
}