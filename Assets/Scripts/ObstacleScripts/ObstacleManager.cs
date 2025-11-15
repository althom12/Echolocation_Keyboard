using UnityEngine;
using AK.Wwise; // Make sure this is here for your Wwise Events
using UnityEngine.UI;
public class ObstacleManager : MonoBehaviour
{
    [Header("Game Objects")]

    public GameObject[] obstacleSets;

    [Header("Material Manager")]

    public ObstacleMaterialManager materialManager;



    public AK.Wwise.Event[] activationSounds;

    // --- We no longer need sizePrefabs or currentSizeIndex ---

    void Start()
    {
        // When the game starts, set the "no obstacles" state.
        // This will also correctly tell the MaterialManager that no set is active.
        SelectLayout(-1, 0);
    }

    // --- The Update() function is no longer needed, as the UI handles all input ---
    // void Update() {... }


    /// <summary>
    /// Activates a layout, plays a sound, AND tells the MaterialManager
    /// what to look at.
    /// </summary>
    public void SelectLayout(int obstacleIndex, int soundIndex)
    {
        AkSoundEngine.StopAll(gameObject); 

        // First, play the sound. Check if the index is valid for the array.
        if (soundIndex >= 0 && soundIndex < activationSounds.Length)
        {
            // Use the AK.Wwise.Event type to post the event [5]
            activationSounds[soundIndex]?.Post(gameObject);
        }

        // Then, activate the correct obstacle set.
        ActivateObstacleSet(obstacleIndex);

        // --- THIS IS THE CRITICAL NEW LOGIC ---
        // After activating the set, tell the MaterialManager to find its reflectors.
        if (materialManager != null)
        {
            GameObject newlyActiveSet = null;

            // Check if the index is valid for our obstacleSets array
            if (obstacleIndex >= 0 && obstacleIndex < obstacleSets.Length)
            {
                newlyActiveSet = obstacleSets[obstacleIndex];
            }

            // Pass the active set (or null if "None") to the material manager
            materialManager.FindReflectorsInSet(newlyActiveSet);
        }
    }

    /// <summary>
    /// This function is now just for activating/deactivating the GameObjects.
    /// </summary>
    public void ActivateObstacleSet(int indexToActivate)
    {
        for (int i = 0; i < obstacleSets.Length; i++)
        {
            bool shouldBeActive = (i == indexToActivate);
            if (obstacleSets[i] != null)
            {
                obstacleSets[i].SetActive(shouldBeActive);
            }
        }
    }

    // --- We no longer need the 'CycleObstacleSize()' function ---


    // --- THESE FUNCTIONS ARE NOW DECOUPLED ---
    // They ONLY change the material on the *currently active* set.

    /// <summary>
    /// This is the function called by the UI Toggle for CARPET.
    /// It must take a boolean, which represents the new state of the toggle.
    /// </summary>
    public void HandleCarpetToggle(bool isOn)
    {
        // CRITICAL GUARD: We only care when the toggle is turned ON.
        if (!isOn)
        {
            // When a Preset calls ClearCustomizations, this function runs with isOn=false.
            // We immediately RETURN to prevent the toggle from re-asserting its selection,
            // which is what was causing the checkmark to stay.
            return;
        }

        if (materialManager != null)
        {
            // 1. Set the material
            materialManager.SetAllMaterialsToCarpet();

            // 2. Set indicators/sync toggles.
            // We call this function on the material manager (which handles indicator logic)
            materialManager.SetIndicatorActive(true, false);
        }
    }

    /// <summary>
    /// This is the function called by the UI Toggle for CONCRETE.
    /// </summary>
    public void HandleConcreteToggle(bool isOn)
    {
        // CRITICAL GUARD: We only care when the toggle is turned ON.
        if (!isOn)
        {
            return;
        }

        if (materialManager != null)
        {
            // 1. Set the material
            materialManager.SetAllMaterialsToConcrete();

            // 2. Set indicators/sync toggles.
            materialManager.SetIndicatorActive(false, true);
        }
    }

    /// <summary>
    /// Clears all visual material indicators and resets the material toggles.
    /// This is called when a non-custom preset (Index 0-4) is selected.
    /// </summary>
    public void ClearCustomizations()
    {
        if (materialManager != null)
        {
            // 1. Clear Material Visuals
            materialManager.SetIndicatorActive(false, false);

            // 2. Clear Material Toggles (We need a way to reference them)
            // Since Toggles don't clear themselves easily from a separate script,
            // we need to access them via the ObstacleMaterialManager.
            // We will implement a function for this next.
        }

        // You may also want to set the actual AkAcousticTexture to 'None' or 'Default' 
        // here if that's an option in your Wwise project, but usually, the presets 
        // handle their own materials.
    }
}