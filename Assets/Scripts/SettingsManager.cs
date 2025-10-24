using UnityEngine;

/// <summary>
/// A persistent MonoBehaviour Singleton that manages all game settings.
/// Provides a central, global "source of truth" for all other systems.
/// Handles loading and saving settings to PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // --- PlayerPrefs Keys ---
    private static readonly string FootstepsEnabledKey = "Settings_FootstepsEnabled";

    // --- Public Static Instance (Singleton Pattern) ---
    private static SettingsManager _instance;

    /// <summary>
    /// The global, persistent instance of the SettingsManager.
    /// </summary>
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Initialize();
            }
            return _instance;
        }
    }

    // --- Public Settings Properties ---

    /// <summary>
    /// Is the footstep audio currently enabled? Read by PlayerAudio, Written by UI.
    /// </summary>
    public bool IsFootstepsEnabled { get; set; }

    // --- Initialization ---

    /// <summary>
    /// Ensures the SettingsManager is created and loaded before any Awake() calls.
    /// </summary>

    public static void Initialize()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<SettingsManager>();
            if (_instance == null)
            {
                GameObject managerObject = new GameObject("@SettingsManager");
                _instance = managerObject.AddComponent<SettingsManager>();
            }
            // Awake() will handle DontDestroyOnLoad
            _instance.LoadSettings();
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings(); // Load settings if Initialize didn't run (e.g. direct scene start in editor)
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Duplicate SettingsManager found. Destroying extra instance.");
            Destroy(gameObject);
            return;
        }
    }

    // --- Persistence (Saving and Loading) ---

    /// <summary>
    /// Saves all current settings to PlayerPrefs. Called by UI elements.
    /// </summary>
    public void SaveSettings()
    {
        // Convert bool to int for PlayerPrefs [1, 2]
        int footstepsValue = IsFootstepsEnabled ? 1 : 0;
        PlayerPrefs.SetInt(FootstepsEnabledKey, footstepsValue); 

        PlayerPrefs.Save();
        Debug.Log("Settings saved. Footsteps enabled: " + IsFootstepsEnabled);
    }

    /// <summary>
    /// Loads all settings from PlayerPrefs. Called only at game launch via Initialize/Awake.
    /// </summary>
    private void LoadSettings()
    {
        // --- THIS IS THE CORRECTED SECTION ---

        // 1. Get the raw value before converting, using 1 (true) as default [1, 2]
        int loadedValue = PlayerPrefs.GetInt(FootstepsEnabledKey, 1);

        // 2. Set the property based on the loaded value
        IsFootstepsEnabled = loadedValue == 1;

        // 3. Log the debug info (this line was causing the error)
        Debug.Log($"SettingsManager.LoadSettings: Loaded raw value for '{FootstepsEnabledKey}' = {loadedValue}. IsFootstepsEnabled set to: {IsFootstepsEnabled}");

        // --- END CORRECTED SECTION ---
    }
}