using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Links a LandmarkDefinition (asset) to scene objects and UI elements.
/// This solves the ScriptableObject limitation where assets can't reference scene objects.
/// </summary>
[System.Serializable]
public class LandmarkUIBinding
{
    [Header("Landmark Data")]
    [Tooltip("The landmark definition asset")]
    public LandmarkDefinition landmarkDefinition;

    [Header("Scene References")]
    [Tooltip("The GameObject in the scene that emits the landmark sound (spatial position)")]
    public GameObject spatialEmitter;

    [Header("UI References")]
    [Tooltip("The UI Toggle that enables/disables this landmark")]
    public UnityEngine.UI.Toggle toggle;

    [Tooltip("The UI Slider that controls this landmark's volume")]
    public UnityEngine.UI.Slider volumeSlider;

    [Tooltip("Optional visual indicator GameObject")]
    public GameObject visualIndicator;
}

/// <summary>
/// Tracks runtime state for a single landmark.
/// This data changes during gameplay and should NOT be in the ScriptableObject.
/// </summary>
[System.Serializable]
public class LandmarkRuntimeState
{
    public bool isPlaying = false;
    public uint playingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
}

/// <summary>
/// Landmarks Manager - REFACTORED for Scalability
/// 
/// Uses LandmarkDefinition ScriptableObjects + LandmarkUIBinding for data-driven landmark management.
/// LandmarkDefinition contains landmark data (name, event, RTPC, default volume).
/// LandmarkUIBinding links LandmarkDefinition to spatial emitters and UI elements.
/// 
/// ADDING NEW LANDMARKS:
/// 1. Create LandmarkDefinition ScriptableObject
/// 2. Configure loop event, RTPC, default volume
/// 3. Add LandmarkUIBinding entry in Inspector
/// 4. Assign definition + spatial emitter + toggle + slider
/// 
/// NO CODE CHANGES REQUIRED after initial setup!
/// 
/// SCALABILITY:
/// - 2 landmarks: Works
/// - 50 landmarks: Works exactly the same way
/// - No per-landmark methods needed
/// </summary>
public class LandmarksManager : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????
    // SINGLETON (SCENE-BASED - NO DontDestroyOnLoad)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Scene-based singleton instance. Does NOT persist across scene loads.
    /// This manager holds references to scene objects (spatialEmitters) that would
    /// be lost if the manager persisted.
    /// </summary>
    public static LandmarksManager Instance { get; private set; }

    // ???????????????????????????????????????????????????????????
    // INSPECTOR FIELDS
    // ???????????????????????????????????????????????????????????

    [Header("Landmark Bindings")]
    [Tooltip("All available landmarks. Add new landmarks by adding to this array!")]
    public LandmarkUIBinding[] landmarkBindings;

    // ???????????????????????????????????????????????????????????
    // PRIVATE FIELDS - Runtime State
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Tracks runtime state for each landmark (playing status, IDs).
    /// Parallel array to landmarkBindings.
    /// </summary>
    private LandmarkRuntimeState[] runtimeStates;

    // ???????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????

    private void Awake()
    {
        // Singleton setup - Scene-based (no DontDestroyOnLoad)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[LandmarksManager] Duplicate instance found on '{gameObject.name}'. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Validate landmarkBindings array
        if (landmarkBindings == null || landmarkBindings.Length == 0)
        {
            Debug.LogError("[LandmarksManager] No landmark bindings assigned! Please add LandmarkUIBinding entries.");
            this.enabled = false;
            return;
        }

        // Initialize runtime state array
        runtimeStates = new LandmarkRuntimeState[landmarkBindings.Length];
        for (int i = 0; i < runtimeStates.Length; i++)
        {
            runtimeStates[i] = new LandmarkRuntimeState();
        }

        // Initialize all landmarks with default volumes
        for (int i = 0; i < landmarkBindings.Length; i++)
        {
            LandmarkUIBinding binding = landmarkBindings[i];

            if (!ValidateBinding(binding, i))
            {
                continue;
            }

            // Set initial volume on spatial emitter
            if (binding.landmarkDefinition.volumeRTPC != null && binding.spatialEmitter != null)
            {
                binding.landmarkDefinition.volumeRTPC.SetValue(
                    binding.spatialEmitter,
                    binding.landmarkDefinition.defaultVolume
                );

                Debug.Log($"[LandmarksManager] Initialized '{binding.landmarkDefinition.landmarkName}' volume to {binding.landmarkDefinition.defaultVolume}");
            }
        }
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Generic Methods (Index-Based)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Sets a landmark enabled/disabled by index.
    /// </summary>
    /// <param name="index">Index into landmarkBindings array</param>
    /// <param name="enabled">True to start playing, false to stop</param>
    public void SetLandmarkEnabled(int index, bool enabled)
    {
        if (!ValidateLandmarkIndex(index)) return;

        LandmarkUIBinding binding = landmarkBindings[index];
        LandmarkRuntimeState state = runtimeStates[index];

        if (enabled)
        {
            StartLandmarkSound(binding, state);
        }
        else
        {
            StopLandmarkSound(binding, state);
        }

        Debug.Log($"[LandmarksManager] '{binding.landmarkDefinition.landmarkName}' {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Sets a landmark's volume by index.
    /// </summary>
    /// <param name="index">Index into landmarkBindings array</param>
    /// <param name="volume">Volume value (typically 0-100)</param>
    public void SetLandmarkVolume(int index, float volume)
    {
        if (!ValidateLandmarkIndex(index)) return;

        LandmarkUIBinding binding = landmarkBindings[index];

        if (binding.landmarkDefinition.volumeRTPC != null && binding.spatialEmitter != null)
        {
            binding.landmarkDefinition.volumeRTPC.SetValue(binding.spatialEmitter, volume);
            Debug.Log($"[LandmarksManager] '{binding.landmarkDefinition.landmarkName}' volume set to {volume}");
        }
    }

    /// <summary>
    /// Gets a landmark binding by name.
    /// </summary>
    public LandmarkUIBinding GetLandmarkByName(string name)
    {
        for (int i = 0; i < landmarkBindings.Length; i++)
        {
            if (landmarkBindings[i] != null &&
                landmarkBindings[i].landmarkDefinition != null &&
                landmarkBindings[i].landmarkDefinition.landmarkName == name)
            {
                return landmarkBindings[i];
            }
        }

        Debug.LogWarning($"[LandmarksManager] Landmark '{name}' not found!");
        return null;
    }

    /// <summary>
    /// Gets a landmark binding by index.
    /// </summary>
    public LandmarkUIBinding GetLandmark(int index)
    {
        if (ValidateLandmarkIndex(index))
        {
            return landmarkBindings[index];
        }
        return null;
    }

    /// <summary>
    /// Checks if a landmark is currently enabled/playing.
    /// </summary>
    public bool IsLandmarkEnabled(int index)
    {
        if (ValidateLandmarkIndex(index))
        {
            return runtimeStates[index].isPlaying;
        }
        return false;
    }

    /// <summary>
    /// Gets the total number of landmarks.
    /// </summary>
    public int GetLandmarkCount()
    {
        return landmarkBindings != null ? landmarkBindings.Length : 0;
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Backward Compatibility (Keep for existing controllers)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes Clock is at index 0
    /// </summary>
    public void SetClockEnabled(bool enabled) => SetLandmarkEnabled(0, enabled);

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes Clock is at index 0
    /// </summary>
    public void SetClockVolume(float volume) => SetLandmarkVolume(0, volume);

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes Clock is at index 0
    /// </summary>
    public bool IsClockEnabled() => IsLandmarkEnabled(0);

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes HVAC is at index 1
    /// </summary>
    public void SetHVACEnabled(bool enabled) => SetLandmarkEnabled(1, enabled);

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes HVAC is at index 1
    /// </summary>
    public void SetHVACVolume(float volume) => SetLandmarkVolume(1, volume);

    /// <summary>
    /// BACKWARD COMPATIBILITY: Assumes HVAC is at index 1
    /// </summary>
    public bool IsHVACEnabled() => IsLandmarkEnabled(1);

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Audio Control
    // ???????????????????????????????????????????????????????????

    private void StartLandmarkSound(LandmarkUIBinding binding, LandmarkRuntimeState state)
    {
        if (binding.landmarkDefinition.loopEvent == null || binding.spatialEmitter == null)
        {
            Debug.LogWarning($"[LandmarksManager] Cannot start '{binding.landmarkDefinition.landmarkName}': Missing event or emitter");
            return;
        }

        // Stop existing instance if playing
        if (state.playingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(state.playingID);
        }

        // Post new event
        state.playingID = binding.landmarkDefinition.loopEvent.Post(binding.spatialEmitter);
        state.isPlaying = true;

        Debug.Log($"[LandmarksManager] Started '{binding.landmarkDefinition.landmarkName}', PlayingID: {state.playingID}");
    }

    private void StopLandmarkSound(LandmarkUIBinding binding, LandmarkRuntimeState state)
    {
        if (state.playingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(state.playingID);
            state.playingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            state.isPlaying = false;
            Debug.Log($"[LandmarksManager] Stopped '{binding.landmarkDefinition.landmarkName}'");
        }
    }

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Validation
    // ???????????????????????????????????????????????????????????

    private bool ValidateLandmarkIndex(int index)
    {
        if (landmarkBindings == null || landmarkBindings.Length == 0)
        {
            Debug.LogError("[LandmarksManager] No landmarks configured!");
            return false;
        }

        if (index < 0 || index >= landmarkBindings.Length)
        {
            Debug.LogError($"[LandmarksManager] Invalid landmark index: {index} (valid range: 0-{landmarkBindings.Length - 1})");
            return false;
        }

        if (!ValidateBinding(landmarkBindings[index], index))
        {
            return false;
        }

        return true;
    }

    private bool ValidateBinding(LandmarkUIBinding binding, int index)
    {
        if (binding == null || binding.landmarkDefinition == null)
        {
            Debug.LogError($"[LandmarksManager] Landmark binding at index {index} is null or has no definition!");
            return false;
        }

        if (binding.spatialEmitter == null)
        {
            Debug.LogWarning($"[LandmarksManager] Landmark '{binding.landmarkDefinition.landmarkName}' has no spatial emitter assigned!");
            return false;
        }

        return true;
    }

    // ???????????????????????????????????????????????????????????
    // DEBUG HELPERS
    // ???????????????????????????????????????????????????????????

#if UNITY_EDITOR
    [ContextMenu("Log All Landmarks")]
    private void LogAllLandmarks()
    {
        Debug.Log("=== LANDMARKS MANAGER STATE ===");
        Debug.Log($"Total Landmarks: {(landmarkBindings != null ? landmarkBindings.Length : 0)}");

        if (landmarkBindings == null) return;

        for (int i = 0; i < landmarkBindings.Length; i++)
        {
            LandmarkUIBinding binding = landmarkBindings[i];

            if (binding != null && binding.landmarkDefinition != null)
            {
                string status = "N/A";
                if (runtimeStates != null && i < runtimeStates.Length)
                {
                    status = $"Playing: {runtimeStates[i].isPlaying}, ID: {runtimeStates[i].playingID}";
                }

                Debug.Log($"[{i}] {binding.landmarkDefinition.landmarkName} - {status}");
            }
            else
            {
                Debug.Log($"[{i}] NULL or Invalid Binding");
            }
        }

        Debug.Log("===============================");
    }
#endif
}